using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace DataLock.Functions
{
    internal static class OperationFiles
    {
        // Commit only complete output. Cancellation or failure leaves an existing destination intact.
        internal static async Task<bool> WriteAsync(string input, string output,
            Func<string, CancellationToken, Task<bool>> write, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(Path.GetFullPath(input), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Input and output must be different files.");
            string staged = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)), $".cipherflow-{Guid.NewGuid():N}.tmp");
            try
            {
                if (!await write(staged, cancellationToken)) return false;
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(staged, output, overwrite: true);
                return true;
            }
            finally
            {
                if (File.Exists(staged)) File.Delete(staged);
            }
        }

        internal static async Task CreateArchiveAsync(string source, string output, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using var archive = ZipFile.Open(output, ZipArchiveMode.Create);
            foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            {
                token.ThrowIfCancellationRequested();
                archive.CreateEntry(Path.GetRelativePath(source, directory).Replace('\\', '/') + "/");
            }
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                token.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(Path.GetRelativePath(source, file).Replace('\\', '/'), CompressionLevel.Fastest);
                using var input = File.OpenRead(file);
                using var destination = entry.Open();
                await input.CopyToAsync(destination, token);
            }
        }

        internal static async Task ExtractArchiveAsync(string input, string output, CancellationToken token)
        {
            string root = Path.GetFullPath(output) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(root);
            using var archive = ZipFile.OpenRead(input);
            foreach (var entry in archive.Entries)
            {
                token.ThrowIfCancellationRequested();
                string path = Path.GetFullPath(Path.Combine(root, entry.FullName));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Archive entry escapes its output folder.");
                if (string.IsNullOrEmpty(entry.Name)) Directory.CreateDirectory(path);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var source = entry.Open();
                    using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    await source.CopyToAsync(destination, token);
                }
            }
        }

        // Folder results are staged next to their destination and renamed only after success.
        // Refuse an existing destination rather than destroying its contents or merging partial results.
        internal static void CommitDirectory(string staged, string output, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (Directory.Exists(output) || File.Exists(output))
                throw new IOException("The output folder already exists. Choose another output location.");
            Directory.Move(staged, output);
        }
    }

    internal sealed class OperationWorkspace : IDisposable
    {
        internal string Path { get; }
        internal OperationWorkspace(string parent)
        {
            Path = System.IO.Path.Combine(parent, "cipherflow-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
