#if !NETFRAMEWORK

using System;
using System.Runtime.InteropServices;

namespace StatsdClient
{
    /// <summary>
    /// P/Invoke wrapper for libfs native library to retrieve file inodes on Linux.
    /// </summary>
    internal static class NativeMethods
    {
        private const string LibraryName = "fs";

        /// <summary>
        /// Gets a value indicating whether the native library is supported on the current platform (Linux only).
        /// </summary>
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Attempts to get the inode number for the specified file path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="inode">The inode number if successful, 0 otherwise.</param>
        /// <returns>true if the inode was successfully retrieved, false otherwise.</returns>
        public static bool TryGetInode(string path, out ulong inode)
        {
            if (!IsSupported)
            {
                inode = 0;
                return false;
            }

            try
            {
                return GetInode(path, out inode) == 0;
            }
            catch (DllNotFoundException)
            {
                // Native library not available
                inode = 0;
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                // Function not found in library
                inode = 0;
                return false;
            }
        }

        /// <summary>
        /// Gets the inode number for a file path using the native libfs library.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="inode">The inode number if successful.</param>
        /// <returns>0 on success, -1 on failure.</returns>
        [DllImport(LibraryName, EntryPoint = "get_inode", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int GetInode(
            string path,
            out ulong inode);
    }
}
#endif
