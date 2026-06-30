using System.Security.Cryptography;
using System.Text.Json.Serialization;
using ErrorOr;
using Forge.Next.Shared;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests;

/// <summary>
/// Unit tests for <see cref="ObjectExtensions"/>: the exception-safe <c>Protect*</c> wrappers and the
/// family of SHA hashing helpers, plus the <see cref="ObjectExtensions.DefaultJsonSerializerOptions"/>
/// configuration.
///
/// The private members (<c>SerializeObjectToJson</c>, <c>GetSHAFromString</c>, <c>GetSHAFromStringAsInt</c>)
/// are not reachable from the test assembly; they are covered indirectly through the public hashing
/// helpers, whose deterministic / length / null-handling behaviour pins down the private logic.
/// </summary>
public class ObjectExtensionsTests
{
    /// <summary>
    /// A tiny serializable DTO used as hash input. Two instances with the same property values serialize
    /// to identical JSON, which lets us assert that hashing is value-based and deterministic.
    /// </summary>
    private sealed class SampleDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    #region Protect / ProtectAsync

    /// <summary>
    /// Tests <see cref="ObjectExtensions.Protect{T, TResult}(T, Func{T, ErrorOr{TResult}}, ErrorType)"/>:
    /// the function's result is passed through on success, and a thrown exception becomes an
    /// <see cref="ErrorOr{TResult}"/> error carrying the requested <see cref="ErrorType"/>.
    /// </summary>
    [Fact]
    public void ProtectTest()
    {
        SampleDto dto = new() { Id = 1, Name = "x" };

        // Success path: stub the function to return a value and verify it flows straight through.
        ErrorOr<int> expected = 42;
        Func<SampleDto, ErrorOr<int>> success = Substitute.For<Func<SampleDto, ErrorOr<int>>>();
        success(dto).Returns(expected);

        ErrorOr<int> ok = dto.Protect(success);
        ok.IsError.ShouldBeFalse();
        ok.Value.ShouldBe(42);
        success.Received(1).Invoke(dto);

        // Failure path: a throwing function is caught and converted into an error result.
        InvalidOperationException boom = new("boom");
        ErrorOr<int> failed = dto.Protect<SampleDto, int>(_ => throw boom, ErrorType.Conflict);
        failed.IsError.ShouldBeTrue();
        failed.FirstError.Type.ShouldBe(ErrorType.Conflict);     // requested error type is honoured
        failed.FirstError.Description.ShouldBe("boom");          // description is the exception message
    }

    /// <summary>
    /// Tests <see cref="ObjectExtensions.ProtectAsync{T, TResult}(T, Func{T, Task{ErrorOr{TResult}}}, ErrorType, bool)"/>.
    /// </summary>
    [Fact]
    public async Task ProtectAsyncTest()
    {
        SampleDto dto = new() { Id = 1, Name = "x" };

        // Success path.
        ErrorOr<int> expected = 42;
        Func<SampleDto, Task<ErrorOr<int>>> success = Substitute.For<Func<SampleDto, Task<ErrorOr<int>>>>();
        success(dto).Returns(Task.FromResult(expected));

        ErrorOr<int> ok = await dto.ProtectAsync(success);
        ok.IsError.ShouldBeFalse();
        ok.Value.ShouldBe(42);
        // The returned Task is discarded: this call only records the "received" assertion.
        _ = success.Received(1).Invoke(dto);

        // Failure path: the (synchronously) throwing delegate is caught around the await.
        InvalidOperationException boom = new("async-boom");
        ErrorOr<int> failed = await dto.ProtectAsync<SampleDto, int>(_ => throw boom, ErrorType.NotFound);
        failed.IsError.ShouldBeTrue();
        failed.FirstError.Type.ShouldBe(ErrorType.NotFound);
        failed.FirstError.Description.ShouldBe("async-boom");
    }

    /// <summary>
    /// Tests <see cref="ObjectExtensions.ProtectDo{T}(T, Action{T}, ErrorType)"/>: the side-effecting action
    /// runs and the ORIGINAL object is returned on success; a thrown exception becomes an error.
    /// </summary>
    [Fact]
    public void ProtectDoTest()
    {
        SampleDto dto = new() { Id = 1, Name = "x" };

        // Success: the action is invoked and the same object instance comes back wrapped as the value.
        Action<SampleDto> action = Substitute.For<Action<SampleDto>>();
        ErrorOr<SampleDto> ok = dto.ProtectDo(action);
        ok.IsError.ShouldBeFalse();
        ok.Value.ShouldBeSameAs(dto);
        action.Received(1).Invoke(dto);

        // Failure: the throwing action is caught and converted to an error.
        InvalidOperationException boom = new("do-boom");
        ErrorOr<SampleDto> failed = dto.ProtectDo(_ => throw boom, ErrorType.Validation);
        failed.IsError.ShouldBeTrue();
        failed.FirstError.Type.ShouldBe(ErrorType.Validation);
        failed.FirstError.Description.ShouldBe("do-boom");
    }

    /// <summary>
    /// Tests <see cref="ObjectExtensions.ProtectDoAsync{T}(T, Func{T, Task}, ErrorType, bool)"/>.
    /// </summary>
    [Fact]
    public async Task ProtectDoAsyncTest()
    {
        SampleDto dto = new() { Id = 1, Name = "x" };

        // Success.
        Func<SampleDto, Task> action = Substitute.For<Func<SampleDto, Task>>();
        action(dto).Returns(Task.CompletedTask);
        ErrorOr<SampleDto> ok = await dto.ProtectDoAsync(action);
        ok.IsError.ShouldBeFalse();
        ok.Value.ShouldBeSameAs(dto);
        // The returned Task is discarded: this call only records the "received" assertion.
        _ = action.Received(1).Invoke(dto);

        // Failure.
        InvalidOperationException boom = new("do-async-boom");
        ErrorOr<SampleDto> failed = await dto.ProtectDoAsync(_ => throw boom, ErrorType.Unauthorized);
        failed.IsError.ShouldBeTrue();
        failed.FirstError.Type.ShouldBe(ErrorType.Unauthorized);
        failed.FirstError.Description.ShouldBe("do-async-boom");
    }

    #endregion

    #region SHA hashing helpers

    /// <summary>
    /// Shared assertions for the byte[]-returning hash helpers. Verifies the digest length, that hashing is
    /// deterministic and value-based (equal content -> equal hash), that different content yields a different
    /// hash, and that a <see langword="null"/> input is rejected with <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="hash">The hash helper under test, captured as a delegate.</param>
    /// <param name="expectedLength">The expected digest size in bytes (e.g. 32 for SHA-256).</param>
    private static void AssertByteHashBehaviour(Func<object, byte[]> hash, int expectedLength)
    {
        SampleDto a1 = new() { Id = 7, Name = "alpha" };
        SampleDto a2 = new() { Id = 7, Name = "alpha" };    // structurally equal to a1
        SampleDto b = new() { Id = 8, Name = "beta" };       // different content

        byte[] h1 = hash(a1);
        h1.ShouldNotBeNull();
        h1.Length.ShouldBe(expectedLength);                  // correct digest size

        hash(a1).ShouldBe(h1);                               // repeating the call is deterministic
        hash(a2).ShouldBe(h1);                               // equal content -> identical hash
        hash(b).ShouldNotBe(h1);                             // different content -> different hash

        // A null instance is rejected before any hashing happens.
        Should.Throw<ArgumentNullException>(() => hash(null!));
    }

    /// <summary>
    /// Shared assertions for the int-returning (<c>...AsInt</c>) hash helpers. <see cref="HashCode.Combine"/>
    /// is deterministic for the lifetime of the process, so equal content must fold to the same integer;
    /// different content folds to a different integer (collision probability is negligible). A
    /// <see langword="null"/> input is rejected.
    /// </summary>
    /// <param name="hash">The hash helper under test, captured as a delegate.</param>
    private static void AssertIntHashBehaviour(Func<object, int> hash)
    {
        SampleDto a1 = new() { Id = 7, Name = "alpha" };
        SampleDto a2 = new() { Id = 7, Name = "alpha" };
        SampleDto b = new() { Id = 8, Name = "beta" };

        int h1 = hash(a1);
        hash(a1).ShouldBe(h1);                               // deterministic within this process
        hash(a2).ShouldBe(h1);                               // equal content -> equal folded int
        hash(b).ShouldNotBe(h1);                             // different content -> different folded int

        Should.Throw<ArgumentNullException>(() => hash(null!));
    }

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA256(object)"/>.</summary>
    [Fact]
    public void GetSHA256Test() => AssertByteHashBehaviour(o => o.GetSHA256(), 32);

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA256AsInt(object)"/>.</summary>
    [Fact]
    public void GetSHA256AsIntTest() => AssertIntHashBehaviour(o => o.GetSHA256AsInt());

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA384(object)"/>.</summary>
    [Fact]
    public void GetSHA384Test() => AssertByteHashBehaviour(o => o.GetSHA384(), 48);

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA384AsInt(object)"/>.</summary>
    [Fact]
    public void GetSHA384AsIntTest() => AssertIntHashBehaviour(o => o.GetSHA384AsInt());

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA512(object)"/>.</summary>
    [Fact]
    public void GetSHA512Test() => AssertByteHashBehaviour(o => o.GetSHA512(), 64);

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA512AsInt(object)"/>.</summary>
    [Fact]
    public void GetSHA512AsIntTest() => AssertIntHashBehaviour(o => o.GetSHA512AsInt());

    // The SHA-3 helpers are compiled into the library only for .NET (NETCOREAPP) target frameworks, so the
    // tests referencing them are guarded by the same symbol. This test project targets net10.0, which defines
    // NETCOREAPP, so they are compiled and executed here.
#if NETCOREAPP

    /// <summary>
    /// Tests <see cref="ObjectExtensions.GetSHA3_256(object)"/>. SHA-3 availability is platform dependent,
    /// so when it is unsupported we assert the helper surfaces <see cref="PlatformNotSupportedException"/>.
    /// </summary>
    [Fact]
    public void GetSHA3_256Test()
    {
        if (!SHA3_256.IsSupported)
        {
            Should.Throw<PlatformNotSupportedException>(() => new SampleDto().GetSHA3_256());
            return;
        }

        AssertByteHashBehaviour(o => o.GetSHA3_256(), 32);
    }

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA3_256_AsInt(object)"/>.</summary>
    [Fact]
    public void GetSHA3_256_AsIntTest()
    {
        if (!SHA3_256.IsSupported)
        {
            Should.Throw<PlatformNotSupportedException>(() => new SampleDto().GetSHA3_256_AsInt());
            return;
        }

        AssertIntHashBehaviour(o => o.GetSHA3_256_AsInt());
    }

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA3_384(object)"/>.</summary>
    [Fact]
    public void GetSHA3_384Test()
    {
        if (!SHA3_384.IsSupported)
        {
            Should.Throw<PlatformNotSupportedException>(() => new SampleDto().GetSHA3_384());
            return;
        }

        AssertByteHashBehaviour(o => o.GetSHA3_384(), 48);
    }

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA3_384_AsInt(object)"/>.</summary>
    [Fact]
    public void GetSHA3_384_AsIntTest()
    {
        if (!SHA3_384.IsSupported)
        {
            Should.Throw<PlatformNotSupportedException>(() => new SampleDto().GetSHA3_384_AsInt());
            return;
        }

        AssertIntHashBehaviour(o => o.GetSHA3_384_AsInt());
    }

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA3_512(object)"/>.</summary>
    [Fact]
    public void GetSHA3_512Test()
    {
        if (!SHA3_512.IsSupported)
        {
            Should.Throw<PlatformNotSupportedException>(() => new SampleDto().GetSHA3_512());
            return;
        }

        AssertByteHashBehaviour(o => o.GetSHA3_512(), 64);
    }

    /// <summary>Tests <see cref="ObjectExtensions.GetSHA3_512_AsInt(object)"/>.</summary>
    [Fact]
    public void GetSHA3_512_AsIntTest()
    {
        if (!SHA3_512.IsSupported)
        {
            Should.Throw<PlatformNotSupportedException>(() => new SampleDto().GetSHA3_512_AsInt());
            return;
        }

        AssertIntHashBehaviour(o => o.GetSHA3_512_AsInt());
    }

#endif

    #endregion

    #region DefaultJsonSerializerOptions

    /// <summary>
    /// Tests the <see cref="ObjectExtensions.DefaultJsonSerializerOptions"/> property exposes options that
    /// include fields and preserve references, as the hashing helpers rely on that configuration.
    /// </summary>
    [Fact]
    public void DefaultJsonSerializerOptionsTest()
    {
        ObjectExtensions.DefaultJsonSerializerOptions.ShouldNotBeNull();
        ObjectExtensions.DefaultJsonSerializerOptions.IncludeFields.ShouldBeTrue();
        ObjectExtensions.DefaultJsonSerializerOptions.ReferenceHandler.ShouldBe(ReferenceHandler.Preserve);
    }

    #endregion
}
