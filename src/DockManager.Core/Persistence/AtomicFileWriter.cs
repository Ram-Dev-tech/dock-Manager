using System.Text;

namespace DockManager.Core.Persistence;

/// <summary>
/// Writes small JSON documents without ever leaving a half written file behind, which matters
/// because the dock saves on every settings change and on shutdown.
/// </summary>
public static class AtomicFileWriter
{
    public static void Write(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        var temp = path + ".tmp";
        File.WriteAllText(temp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Moves a file aside so a corrupt document is never silently overwritten.</summary>
    public static string? MoveAside(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var backup = $"{path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(path, backup, overwrite: true);
            return backup;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
