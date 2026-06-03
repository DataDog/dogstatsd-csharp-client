#if !NETFRAMEWORK

using System;
using System.Runtime.InteropServices;

namespace StatsdClient
{
    /// <summary>
    /// P/Invoke wrapper for the libc <c>statx</c> system call to retrieve file inodes on Linux.
    /// </summary>
    /// <remarks>
    /// <c>statx</c> is used instead of <c>stat</c> because its result structure (<c>struct statx</c>)
    /// has a fixed, architecture-independent layout defined by the kernel, so a single managed struct
    /// definition works across all architectures and libc implementations (glibc and musl). The older
    /// <c>stat</c> call has a layout that varies by architecture and libc, which is not safe to marshal directly.
    /// </remarks>
    internal static class NativeMethods
    {
        // Resolve the path relative to the current working directory (used when the path is not a dirfd-relative path).
        private const int AT_FDCWD = -100;

        // Request only the inode number (stx_ino) from statx.
        private const uint STATX_INO = 0x00000100;

        /// <summary>
        /// Gets a value indicating whether the inode lookup is supported on the current platform (Linux only).
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
            inode = 0;

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (!IsSupported)
            {
                return false;
            }
            try
            {
                // flags = 0 means symlinks are followed, matching stat() behavior (needed for the
                // /proc/self/ns/cgroup magic link). statx returns -1 on failure, including ENOSYS on
                // kernels older than 4.11, in which case we fall back to no inode.
                if (Statx(AT_FDCWD, path, 0, STATX_INO, out var buffer) != 0)
                {
                    return false;
                }

                inode = buffer.StatxIno;
                return true;
            }
            catch (DllNotFoundException)
            {
                // libc not available
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                // statx not available (e.g. glibc older than 2.28)
                return false;
            }
        }

        [DllImport("libc", EntryPoint = "statx", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern int Statx(int dirfd, string pathname, int flags, uint mask, out StatxBuffer buffer);

        /// <summary>
        /// Mirror of the kernel's <c>struct statx</c> (256 bytes). Only the inode field is read; the
        /// full size is preserved so the kernel does not write past the buffer.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 256)]
        private struct StatxBuffer
        {
            // stx_ino lives at byte offset 32 in struct statx.
            [FieldOffset(32)]
            public ulong StatxIno;
        }
    }
}
#endif
