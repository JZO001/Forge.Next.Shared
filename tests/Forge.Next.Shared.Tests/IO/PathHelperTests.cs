using ErrorOr;
using Forge.Next.Shared.IO;
using Shouldly;
using Xunit;

namespace Forge.Next.Shared.Tests.IO;

/// <summary>
/// Unit tests for <see cref="PathExtensions"/>.
///
/// The class implements <see cref="IDisposable"/> so xunit disposes it after the test runs; that gives us a
/// hook to clean up any directories created on disk during the write-probe scenarios.
/// </summary>
public sealed class PathHelperTests : IDisposable
{
    /// <summary>Tracks the temp directories handed out by <see cref="CreateUniqueTempPath"/> for cleanup.</summary>
    private readonly List<string> _createdDirectories = new();

    /// <summary>
    /// Tests <see cref="PathExtensions.PerformFolderSecurityCheck(string)"/> across all of its branches:
    /// an existing writable folder, a folder that must be created, an empty path, an illegal path, and null.
    /// The method now returns an <see cref="ErrorOr{TValue}"/> of <see cref="Success"/>, so success is asserted
    /// via <c>IsError == false</c> and failures via <c>IsError == true</c> (null produces a validation error
    /// rather than throwing).
    /// </summary>
    [Fact]
    public void PerformFolderSecurityCheckTest()
    {
        // 1. An existing, writable directory -> the create/write/delete probe succeeds -> a Success result.
        string existing = CreateUniqueTempPath();
        Directory.CreateDirectory(existing);
        PathExtensions.PerformFolderSecurityCheck(existing).IsError.ShouldBeFalse();

        // 2. A not-yet-existing directory under temp -> the method creates it and succeeds.
        string toCreate = CreateUniqueTempPath();
        Directory.Exists(toCreate).ShouldBeFalse();
        PathExtensions.PerformFolderSecurityCheck(toCreate).IsError.ShouldBeFalse();
        Directory.Exists(toCreate).ShouldBeTrue();           // created as a side effect of the probe

        // 3. An empty path -> new DirectoryInfo("") throws; the exception is caught and converted to an error.
        PathExtensions.PerformFolderSecurityCheck(string.Empty).IsError.ShouldBeTrue();

        // 4. A path with an illegal embedded null character -> the file-system call throws, is caught -> error.
        string invalid = Path.GetTempPath() + "\0invalid";
        PathExtensions.PerformFolderSecurityCheck(invalid).IsError.ShouldBeTrue();

        // 5. A null path -> NO exception is thrown; a Validation error is returned instead.
        ErrorOr<Success> nullResult = PathExtensions.PerformFolderSecurityCheck(null!);
        nullResult.IsError.ShouldBeTrue();
        nullResult.FirstError.Type.ShouldBe(ErrorType.Validation);
        nullResult.FirstError.Description.ShouldBe("Path cannot be null");
    }

    /// <summary>
    /// Produces a unique (not-yet-created) path under the system temp folder and records it for later
    /// cleanup. The directory is intentionally NOT created here so callers can choose whether to create it.
    /// </summary>
    /// <returns>A unique candidate directory path.</returns>
    private string CreateUniqueTempPath()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "Forge.Next.Shared.Tests",
            Guid.NewGuid().ToString("N"));

        _createdDirectories.Add(path);
        return path;
    }

    /// <summary>
    /// Best-effort removal of every directory produced during the test. Cleanup failures are swallowed
    /// because temp directories are transient and must never fail the test run.
    /// </summary>
    public void Dispose()
    {
        foreach (string dir in _createdDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // Ignore: cleanup is non-critical.
            }
        }
    }
}
