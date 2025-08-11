using System;
using System.IO;
using Mono.Unix.Native;

namespace StatsdClient
{
    /// <summary>
    /// Interface into the filesystem which allows us to mock out file access
    /// in tests.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Attempts to read the entire file at path.
        /// </summary>
        /// <param name="path">The file path to read from</param>
        /// <param name="contents">The file contents if successful, null otherwise</param>
        /// <returns>True if the file was successfully read, false otherwise</returns>
        bool TryReadAllText(string path, out string contents);

        /// <summary>
        /// Attempts to get the inode of the file at the given path.
        /// </summary>
        /// <param name="path">The file path to get inode for</param>
        /// <param name="inode">The inode number if successful, 0 otherwise</param>
        /// <returns>True if the file stat was successful, false otherwise</returns>
        bool TryStat(string path, out ulong inode);

        /// <summary>
        /// Opens a reader at the given path.
        /// Caller is responsible for disposing the returned TextReader.
        /// </summary>
        /// <param name="path">The file path to open for reading</param>
        /// <returns>A TextReader for the specified file</returns>
        TextReader OpenText(string path);
    }

    /// <summary>
    /// Implementation of IFileSystem that access the underlying file system.
    /// </summary>
    public class FileSystem : IFileSystem
    {
        /// <summary>
        /// Attempts to read the entire file at path.
        /// </summary>
        /// <param name="path">The file path to read from</param>
        /// <param name="content">The file contents if successful, null otherwise</param>
        /// <returns>True if the file was successfully read, false otherwise</returns>
        public bool TryReadAllText(string path, out string content)
        {
            try
            {
                content = File.ReadAllText(path);
                return true;
            }
            catch
            {
                content = null;
                return false;
            }
        }

        /// <summary>
        /// Opens a reader at the given path.
        /// Caller is responsible for disposing the returned TextReader.
        /// </summary>
        /// <param name="path">The file path to open for reading</param>
        /// <returns>A TextReader for the specified file</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the file cannot be found</exception>
        /// <exception cref="System.UnauthorizedAccessException">Thrown when access to the file is denied</exception>
        public TextReader OpenText(string path)
        {
            return new StreamReader(File.OpenRead(path));
        }

        /// <summary>
        /// Attempts to get the inode of the file at the given path.
        /// </summary>
        /// <param name="path">The file path to get inode for</param>
        /// <param name="inode">The inode number if successful, 0 otherwise</param>
        /// <returns>True if the file stat was successful, false otherwise</returns>
        public bool TryStat(string path, out ulong inode)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix &&
                Syscall.stat(path, out var stat) > 0)
            {
                inode = stat.st_ino;
                return true;
            }

            inode = 0;
            return false;
        }
    }
}
