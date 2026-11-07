using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataLock.Functions
{
    internal sealed class CryptoOperation
    {
        internal bool Decrypting { get; init; }
        internal int Algorithm { get; init; }
        internal string Password { get; init; }
        internal string OutputDirectory { get; init; }
        internal string TempDirectory { get; init; }
        internal bool KeepOriginal { get; init; }
        internal bool EncryptFilesIndividually { get; init; }

        private async Task TransformAsync(string input, string output, bool decrypt, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            bool stream = new FileInfo(input).Length >= 2L * 1024 * 1024 * 1024;
            bool success = (decrypt, Algorithm, stream) switch
            {
                (false, 0, false) => await Encrypt.AES_GCM_Encrypt(input, output, Password, token),
                (false, 0, true) => await Encrypt.AES_GCM_Encrypt_Stream(input, output, Password, token),
                (false, 1, false) => await Encrypt.ChaCha20_Poly1305_Encrypt(input, output, Password, token),
                (false, 1, true) => await Encrypt.ChaCha20_Poly1305_Encrypt_Stream(input, output, Password, token),
                (true, 0, false) => await Decrypt.AES_GCM_Decrypt(input, output, Password, token),
                (true, 0, true) => await Decrypt.AES_GCM_Decrypt_Stream(input, output, Password, token),
                (true, 1, false) => await Decrypt.ChaCha20_Poly1305_Decrypt(input, output, Password, token),
                (true, 1, true) => await Decrypt.ChaCha20_Poly1305_Decrypt_Stream(input, output, Password, token),
                _ => throw new InvalidOperationException("Unknown algorithm.")
            };
            if (!success) throw new IOException(decrypt ? "Decryption failed. Check the password and file integrity." : "Encryption failed. The original has been kept.");
        }

        internal async Task<string> ProcessAsync(string input, bool isFolder, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string parent = OutputDirectory ?? Path.GetDirectoryName(input);
            string name = Path.GetFileName(input.TrimEnd(Path.DirectorySeparatorChar));
            if (isFolder)
            {
                string sourceRoot = Path.GetFullPath(input).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (string directory in new[] { parent, TempDirectory })
                {
                    string candidate = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (candidate.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Output and temporary folders must be outside the selected source folder.");
                }
            }
            string output;
            if (!Decrypting)
            {
                output = Path.Combine(parent, name + (isFolder ? (EncryptFilesIndividually ? ".encrec" : ".encfolder") : ".enc"));
                if (!isFolder) await TransformAsync(input, output, false, token);
                else
                {
                    using var workspace = new OperationWorkspace(TempDirectory);
                    string archiveSource = input;
                    if (EncryptFilesIndividually)
                    {
                        archiveSource = Path.Combine(workspace.Path, "encrypted");
                        Directory.CreateDirectory(archiveSource);
                        foreach (string directory in Directory.EnumerateDirectories(input, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            Directory.CreateDirectory(Path.Combine(archiveSource, Path.GetRelativePath(input, directory)));
                        }
                        foreach (string file in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            string destination = Path.Combine(archiveSource, Path.GetRelativePath(input, file) + ".enc");
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                            await TransformAsync(file, destination, false, token);
                        }
                    }
                    string zip = Path.Combine(workspace.Path, "contents.zip");
                    await OperationFiles.CreateArchiveAsync(archiveSource, zip, token);
                    await TransformAsync(zip, output, false, token);
                }
            }
            else
            {
                string extension = Path.GetExtension(input).ToLowerInvariant();
                if (isFolder || (extension != ".enc" && extension != ".encfolder" && extension != ".encrec"))
                    throw new IOException("Select .enc, .encfolder, or .encrec files to decrypt.");
                output = Path.Combine(parent, Path.GetFileNameWithoutExtension(input));
                if (extension == ".enc") await TransformAsync(input, output, true, token);
                else
                {
                    if (Directory.Exists(output) || File.Exists(output))
                        throw new IOException("The output folder already exists. Choose another output location.");
                    using var workspace = new OperationWorkspace(TempDirectory);
                    // A sibling staging folder permits a rename on the same volume after completion.
                    using var staged = new OperationWorkspace(parent);
                    string zip = Path.Combine(workspace.Path, "contents.zip");
                    await TransformAsync(input, zip, true, token);
                    if (extension == ".encfolder")
                        await OperationFiles.ExtractArchiveAsync(zip, staged.Path, token);
                    else
                    {
                        string extracted = Path.Combine(workspace.Path, "encrypted");
                        await OperationFiles.ExtractArchiveAsync(zip, extracted, token);
                        foreach (string directory in Directory.EnumerateDirectories(extracted, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            Directory.CreateDirectory(Path.Combine(staged.Path, Path.GetRelativePath(extracted, directory)));
                        }
                        foreach (string file in Directory.EnumerateFiles(extracted, "*.enc", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested();
                            string relative = Path.GetRelativePath(extracted, file);
                            string destination = Path.Combine(staged.Path, relative[..^4]);
                            Directory.CreateDirectory(Path.GetDirectoryName(destination));
                            await TransformAsync(file, destination, true, token);
                        }
                    }
                    OperationFiles.CommitDirectory(staged.Path, output, token);
                }
            }

            // The output has committed. Cancellation after this point does not undo this item.
            // If cancellation arrived during commit, keep the source as an extra precaution.
            if (!KeepOriginal && !token.IsCancellationRequested)
            {
                if (isFolder) Directory.Delete(input, recursive: true);
                else File.Delete(input);
            }
            return output;
        }
    }
}
