using ErrorOr;

namespace Forge.Next.Shared.IO;

/// <summary>
/// Helper methods for working with file-system paths.
/// </summary>
public static class PathExtensions
{

    /// <summary>
    /// Verifies that the specified folder is usable by performing a write probe: it creates the directory when
    /// it does not yet exist, then creates and deletes a uniquely named temporary file inside it.
    /// </summary>
    /// <param name="path">The folder path to check. A <see langword="null"/> value yields a validation error.</param>
    /// <returns>
    /// A successful <see cref="ErrorOr{TValue}"/> of <see cref="Success"/> when the directory exists (or could
    /// be created) and a file could be created and deleted within it. Otherwise an error result: a
    /// <see cref="ErrorType.Validation"/> error when <paramref name="path"/> is <see langword="null"/>, or the
    /// error(s) converted from the caught exception when a file-system operation fails (for example due to
    /// missing permissions or an invalid path).
    /// </returns>
    public static ErrorOr<Success> PerformFolderSecurityCheck(string path)
    {
        if (path is null) return Error.Validation(description: "Path cannot be null");

        try
        {
            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                di.Create();
            }

            FileInfo testFile = new FileInfo(Path.Combine(path, $"{Guid.NewGuid()}"));

            if (testFile.Exists) testFile.Delete();

            testFile
                .Create()
                .Dispose();

            testFile.Delete();
        }
        catch (Exception ex)
        {
            return ex.ToErrorOrError();
        }

        return Result.Success;
    }

}
