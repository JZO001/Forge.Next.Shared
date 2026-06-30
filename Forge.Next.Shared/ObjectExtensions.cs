using ErrorOr;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forge.Next.Shared;

/// <summary>
/// Extension methods for arbitrary objects: exception-safe execution wrappers that return an
/// <see cref="ErrorOr{TValue}"/> instead of throwing, and helpers that hash an object based on its
/// JSON representation.
/// </summary>
public static class ObjectExtensions
{

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used when serializing objects to JSON prior to hashing.
    /// Fields are included and reference loops are preserved.
    /// </summary>
    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; set; } = new JsonSerializerOptions
    {
        IncludeFields = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    /// <summary>
    /// Asynchronously invokes <paramref name="func"/> on <paramref name="object"/>, catching any thrown
    /// exception and converting it into an <see cref="ErrorOr{TResult}"/> error.
    /// </summary>
    /// <typeparam name="T">The type of the source object.</typeparam>
    /// <typeparam name="TResult">The type of the successful result.</typeparam>
    /// <param name="object">The object the function operates on.</param>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <param name="errorType">The <see cref="ErrorType"/> assigned to the error when an exception is caught.</param>
    /// <param name="configureAwait">The value forwarded to <c>ConfigureAwait</c> when awaiting <paramref name="func"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while awaiting <paramref name="func"/>.</param>
    /// <returns>The result produced by <paramref name="func"/>, or an error describing the caught exception.</returns>
    public static async Task<ErrorOr<TResult>> ProtectAsync<T, TResult>(
        this T @object,
        Func<T, CancellationToken, Task<ErrorOr<TResult>>> func,
        ErrorType errorType = ErrorType.Unexpected,
        bool configureAwait = false,
        CancellationToken cancellationToken = default!)
    {
        try
        {
            return await func(@object, cancellationToken).ConfigureAwait(configureAwait);
        }
        catch (Exception ex)
        {
            return ex.ToErrorOrError<TResult>(errorType);
        }
    }

    /// <summary>
    /// Invokes <paramref name="func"/> on <paramref name="object"/>, catching any thrown exception and
    /// converting it into an <see cref="ErrorOr{TResult}"/> error.
    /// </summary>
    /// <typeparam name="T">The type of the source object.</typeparam>
    /// <typeparam name="TResult">The type of the successful result.</typeparam>
    /// <param name="object">The object the function operates on.</param>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorType">The <see cref="ErrorType"/> assigned to the error when an exception is caught.</param>
    /// <returns>The result produced by <paramref name="func"/>, or an error describing the caught exception.</returns>
    public static ErrorOr<TResult> Protect<T, TResult>(
        this T @object,
        Func<T, ErrorOr<TResult>> func,
        ErrorType errorType = ErrorType.Unexpected)
    {
        try
        {
            return func(@object);
        }
        catch (Exception ex)
        {
            return ex.ToErrorOrError<TResult>(errorType);
        }
    }

    /// <summary>
    /// Asynchronously runs <paramref name="func"/> on <paramref name="object"/> for its side effects,
    /// returning the object itself on success or an <see cref="ErrorOr{T}"/> error describing any thrown exception.
    /// </summary>
    /// <typeparam name="T">The type of the source object.</typeparam>
    /// <param name="object">The object the function operates on and that is returned on success.</param>
    /// <param name="func">The asynchronous action to execute.</param>
    /// <param name="errorType">The <see cref="ErrorType"/> assigned to the error when an exception is caught.</param>
    /// <param name="configureAwait">The value forwarded to <c>ConfigureAwait</c> when awaiting <paramref name="func"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe while awaiting <paramref name="func"/>.</param>
    /// <returns><paramref name="object"/> when <paramref name="func"/> completes, or an error describing the caught exception.</returns>
    public static async Task<ErrorOr<T>> ProtectDoAsync<T>(
        this T @object,
        Func<T, CancellationToken, Task> func,
        ErrorType errorType = ErrorType.Unexpected,
        bool configureAwait = false,
        CancellationToken cancellationToken = default!)
    {
        try
        {
            await func(@object, cancellationToken).ConfigureAwait(configureAwait);
            return @object;
        }
        catch (Exception ex)
        {
            return ex.ToErrorOrError<T>(errorType);
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> on <paramref name="object"/> for its side effects, returning the object
    /// itself on success or an <see cref="ErrorOr{T}"/> error describing any thrown exception.
    /// </summary>
    /// <typeparam name="T">The type of the source object.</typeparam>
    /// <param name="object">The object the action operates on and that is returned on success.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorType">The <see cref="ErrorType"/> assigned to the error when an exception is caught.</param>
    /// <returns><paramref name="object"/> when <paramref name="action"/> completes, or an error describing the caught exception.</returns>
    public static ErrorOr<T> ProtectDo<T>(
        this T @object,
        Action<T> action,
        ErrorType errorType = ErrorType.Unexpected)
    {
        try
        {
            action(@object);
            return @object;
        }
        catch (Exception ex)
        {
            return ex.ToErrorOrError<T>(errorType);
        }
    }

    /// <summary>
    /// Computes the SHA-256 hash of the object's JSON representation and folds it into a 32-bit integer.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>An integer derived from the SHA-256 hash of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    public static int GetSHA256AsInt(this object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        using (SHA256 sha256 = SHA256.Create())
        {
            return GetSHAFromStringAsInt(SerializeObjectToJson(obj), sha256);
        }
    }

    /// <summary>
    /// Computes the SHA-256 hash of the object's JSON representation.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>The SHA-256 hash bytes of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    public static byte[] GetSHA256(this object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        using (SHA256 sha256 = SHA256.Create())
        {
            return GetSHAFromString(SerializeObjectToJson(obj), sha256);
        }
    }

    /// <summary>
    /// Computes the SHA-384 hash of the object's JSON representation and folds it into a 32-bit integer.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>An integer derived from the SHA-384 hash of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    public static int GetSHA384AsInt(this object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        using (SHA384 sha384 = SHA384.Create())
        {
            return GetSHAFromStringAsInt(SerializeObjectToJson(obj), sha384);
        }
    }

    /// <summary>
    /// Computes the SHA-384 hash of the object's JSON representation.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>The SHA-384 hash bytes of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    public static byte[] GetSHA384(this object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        using (SHA384 sha384 = SHA384.Create())
        {
            return GetSHAFromString(SerializeObjectToJson(obj), sha384);
        }
    }

    /// <summary>
    /// Computes the SHA-512 hash of the object's JSON representation and folds it into a 32-bit integer.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>An integer derived from the SHA-512 hash of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    public static int GetSHA512AsInt(this object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        using (SHA512 sha512 = SHA512.Create())
        {
            return GetSHAFromStringAsInt(SerializeObjectToJson(obj), sha512);
        }
    }

    /// <summary>
    /// Computes the SHA-512 hash of the object's JSON representation.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>The SHA-512 hash bytes of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    public static byte[] GetSHA512(this object obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        using (SHA512 sha512 = SHA512.Create())
        {
            return GetSHAFromString(SerializeObjectToJson(obj), sha512);
        }
    }

#if NETCOREAPP

    /// <summary>
    /// Computes the SHA3-256 hash of the object's JSON representation and folds it into a 32-bit integer.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>An integer derived from the SHA3-256 hash of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">SHA3-256 is not supported on the current platform.</exception>
    public static int GetSHA3_256_AsInt(this object obj)
    {
        if (obj is null) ArgumentNullException.ThrowIfNull(obj);

        using (SHA3_256 sha3_256 = SHA3_256.Create())
        {
            return GetSHAFromStringAsInt(SerializeObjectToJson(obj), sha3_256);
        }
    }

    /// <summary>
    /// Computes the SHA3-256 hash of the object's JSON representation.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>The SHA3-256 hash bytes of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">SHA3-256 is not supported on the current platform.</exception>
    public static byte[] GetSHA3_256(this object obj)
    {
        if (obj is null) ArgumentNullException.ThrowIfNull(obj);

        using (SHA3_256 sha3_256 = SHA3_256.Create())
        {
            return GetSHAFromString(SerializeObjectToJson(obj), sha3_256);
        }
    }

    /// <summary>
    /// Computes the SHA3-384 hash of the object's JSON representation and folds it into a 32-bit integer.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>An integer derived from the SHA3-384 hash of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">SHA3-384 is not supported on the current platform.</exception>
    public static int GetSHA3_384_AsInt(this object obj)
    {
        if (obj is null) ArgumentNullException.ThrowIfNull(obj);

        using (SHA3_384 sha3_384 = SHA3_384.Create())
        {
            return GetSHAFromStringAsInt(SerializeObjectToJson(obj), sha3_384);
        }
    }

    /// <summary>
    /// Computes the SHA3-384 hash of the object's JSON representation.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>The SHA3-384 hash bytes of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">SHA3-384 is not supported on the current platform.</exception>
    public static byte[] GetSHA3_384(this object obj)
    {
        if (obj is null) ArgumentNullException.ThrowIfNull(obj);

        using (SHA3_384 sha3_384 = SHA3_384.Create())
        {
            return GetSHAFromString(SerializeObjectToJson(obj), sha3_384);
        }
    }

    /// <summary>
    /// Computes the SHA3-512 hash of the object's JSON representation and folds it into a 32-bit integer.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>An integer derived from the SHA3-512 hash of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">SHA3-512 is not supported on the current platform.</exception>
    public static int GetSHA3_512_AsInt(this object obj)
    {
        if (obj is null) ArgumentNullException.ThrowIfNull(obj);

        using (SHA3_512 sha3_512 = SHA3_512.Create())
        {
            return GetSHAFromStringAsInt(SerializeObjectToJson(obj), sha3_512);
        }
    }

    /// <summary>
    /// Computes the SHA3-512 hash of the object's JSON representation.
    /// </summary>
    /// <param name="obj">The object to hash. Must not be <see langword="null"/>.</param>
    /// <returns>The SHA3-512 hash bytes of the serialized object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="PlatformNotSupportedException">SHA3-512 is not supported on the current platform.</exception>
    public static byte[] GetSHA3_512(this object obj)
    {
        if (obj is null) ArgumentNullException.ThrowIfNull(obj);

        using (SHA3_512 sha3_512 = SHA3_512.Create())
        {
            return GetSHAFromString(SerializeObjectToJson(obj), sha3_512);
        }
    }

#endif

    /// <summary>
    /// Serializes <paramref name="obj"/> to JSON using <see cref="DefaultJsonSerializerOptions"/>.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The JSON representation of <paramref name="obj"/>.</returns>
    private static string SerializeObjectToJson(object obj)
    {
        return JsonSerializer.Serialize(obj, DefaultJsonSerializerOptions);
    }

    /// <summary>
    /// Computes the hash of the UTF-8 bytes of <paramref name="input"/> using the supplied algorithm.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <returns>The computed hash bytes.</returns>
    private static byte[] GetSHAFromString(string input, HashAlgorithm hashAlgorithm)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        return hashAlgorithm.ComputeHash(inputBytes);
    }

    /// <summary>
    /// Computes the hash of <paramref name="input"/> and folds the resulting bytes into a single 32-bit integer.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <returns>An integer derived from the computed hash bytes.</returns>
    private static int GetSHAFromStringAsInt(string input, HashAlgorithm hashAlgorithm)
    {
        int result = 0;
        byte[] hash = GetSHAFromString(input, hashAlgorithm);

        foreach (byte b in hash)
        {
            result = HashCode.Combine(result, b.GetHashCode());
        }

        return result;
    }

}
