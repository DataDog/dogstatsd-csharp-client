#if !NET461

using System.Runtime.InteropServices;

namespace StatsdClient;

/// <summary>
/// P/Invoke wrapper for Unix system calls
/// </summary>
internal static class NativeMethods
{
    /// <summary>
    /// Attempts to get the inode of the file at the given path
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="inode">The inode number if successful, 0 otherwise</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool TryStat(string path, out ulong inode)
    {
        try
        {
            int result = Stat(path, out var statBuf);
            if (result == 0)
            {
                inode = statBuf.st_ino;
                return true;
            }
        }
        catch
        {
            // P/Invoke failed
        }

        // P/Invoke failed or unsupported OS
        inode = 0;
        return false;
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "stat", CharSet = CharSet.Ansi)]
    private static extern int Stat(string pathname, out StatStruct buf);

    [StructLayout(LayoutKind.Explicit, Size = 144)]
    private struct StatStruct
    {
        [FieldOffset(0)]
        public ulong st_dev;       // device (offset 0, 8 bytes)

        [FieldOffset(8)]
        public ulong st_ino;       // inode (offset 8, 8 bytes)

        [FieldOffset(16)]
        public ulong st_nlink;     // number of hard links (offset 16, 8 bytes)

        [FieldOffset(24)]
        public uint st_mode;       // protection (offset 24, 4 bytes)

        [FieldOffset(28)]
        public uint st_uid;        // user ID (offset 28, 4 bytes)

        [FieldOffset(32)]
        public uint st_gid;        // group ID (offset 32, 4 bytes)

        // [FieldOffset(36)] - 4 bytes padding

        [FieldOffset(40)]
        public ulong st_rdev;      // device type (offset 40, 8 bytes)

        [FieldOffset(48)]
        public long st_size;       // size (offset 48, 8 bytes)

        [FieldOffset(56)]
        public long st_blksize;    // block size (offset 56, 8 bytes)

        [FieldOffset(64)]
        public long st_blocks;     // blocks allocated (offset 64, 8 bytes)

        [FieldOffset(72)]
        public long st_atime;      // access time (offset 72, 8 bytes)

        [FieldOffset(80)]
        public long st_atime_nsec; // access time nsec (offset 80, 8 bytes)

        [FieldOffset(88)]
        public long st_mtime;      // modification time (offset 88, 8 bytes)

        [FieldOffset(96)]
        public long st_mtime_nsec; // modification time nsec (offset 96, 8 bytes)

        [FieldOffset(104)]
        public long st_ctime;      // status change time (offset 104, 8 bytes)

        [FieldOffset(112)]
        public long st_ctime_nsec; // status change time nsec (offset 112, 8 bytes)

        // Total size: 144 bytes (includes 24 bytes reserved at end on x86_64)
        // Note: This struct layout matches the Linux x86_64 glibc struct stat layout.
        // Using explicit offsets to ensure correct memory marshaling across architectures.
    }
}

#endif
