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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                int result = StatMacOS(path, out var statBuf);
                if (result == 0)
                {
                    inode = statBuf.st_ino;
                    return true;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux (and other Unix-like systems)
                int result = StatLinux(path, out var statBuf);
                if (result == 0)
                {
                    inode = statBuf.st_ino;
                    return true;
                }
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

    // Linux stat syscall (x86_64 and ARM64/aarch64)
    [DllImport("libc", SetLastError = true, EntryPoint = "stat", CharSet = CharSet.Ansi)]
    private static extern int StatLinux(string pathname, out StatStructLinux buf);

    // macOS stat syscall (x86_64 and ARM64/Apple Silicon)
    [DllImport("libc", SetLastError = true, EntryPoint = "stat", CharSet = CharSet.Ansi)]
    private static extern int StatMacOS(string pathname, out StatStructMacOS buf);

    // Linux struct stat (asm-generic/stat.h - used by ARM64/aarch64 and x86_64)
    // Reference: https://github.com/torvalds/linux/blob/master/include/uapi/asm-generic/stat.h
    [StructLayout(LayoutKind.Sequential)]
    private struct StatStructLinux
    {
        public ulong st_dev;        // Device
        public ulong st_ino;        // File serial number (inode)
        public uint st_mode;        // File mode
        public uint st_nlink;       // Link count
        public uint st_uid;         // User ID of the file's owner
        public uint st_gid;         // Group ID of the file's group
        public ulong st_rdev;       // Device number, if device
        public ulong __pad1;
        public long st_size;        // Size of file, in bytes
        public int st_blksize;      // Optimal block size for I/O
        public int __pad2;
        public long st_blocks;      // Number 512-byte blocks allocated
        public long st_atime;       // Time of last access
        public ulong st_atime_nsec;
        public long st_mtime;       // Time of last modification
        public ulong st_mtime_nsec;
        public long st_ctime;       // Time of last status change
        public ulong st_ctime_nsec;
        public uint __unused4;
        public uint __unused5;
    }

    // macOS struct stat (sys/stat.h - x86_64 and ARM64)
    // Reference: https://stackoverflow.com/questions/39671660
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    private struct StatStructMacOS
    {
        [FieldOffset(0)]
        public int st_dev;          // Device (4 bytes)

        [FieldOffset(4)]
        public ushort st_mode;      // File mode (2 bytes)

        [FieldOffset(6)]
        public ushort st_nlink;     // Link count (2 bytes)

        [FieldOffset(8)]
        public ulong st_ino;        // File serial number (inode) (8 bytes)

        [FieldOffset(16)]
        public uint st_uid;         // User ID (4 bytes)

        [FieldOffset(20)]
        public uint st_gid;         // Group ID (4 bytes)

        [FieldOffset(24)]
        public int st_rdev;         // Device number (4 bytes)

        // Padding to align timespec at offset 32
        // [FieldOffset(28)] - 4 bytes padding

        // Four timespec structures (each 16 bytes: 8 bytes tv_sec + 8 bytes tv_nsec)
        [FieldOffset(32)]
        public long st_atimespec_sec;       // Access time seconds (8 bytes)

        [FieldOffset(40)]
        public long st_atimespec_nsec;      // Access time nanoseconds (8 bytes)

        [FieldOffset(48)]
        public long st_mtimespec_sec;       // Modification time seconds (8 bytes)

        [FieldOffset(56)]
        public long st_mtimespec_nsec;      // Modification time nanoseconds (8 bytes)

        [FieldOffset(64)]
        public long st_ctimespec_sec;       // Status change time seconds (8 bytes)

        [FieldOffset(72)]
        public long st_ctimespec_nsec;      // Status change time nanoseconds (8 bytes)

        [FieldOffset(80)]
        public long st_birthtimespec_sec;   // Birth time seconds (8 bytes) - macOS specific

        [FieldOffset(88)]
        public long st_birthtimespec_nsec;  // Birth time nanoseconds (8 bytes) - macOS specific

        [FieldOffset(96)]
        public long st_size;                // File size in bytes (8 bytes)

        [FieldOffset(104)]
        public long st_blocks;              // Blocks allocated (8 bytes)

        [FieldOffset(112)]
        public int st_blksize;              // Optimal block size (4 bytes)

        [FieldOffset(116)]
        public uint st_flags;               // User defined flags - macOS specific (4 bytes)

        [FieldOffset(120)]
        public uint st_gen;                 // File generation number - macOS specific (4 bytes)

        // Remaining fields up to 144 bytes
    }
}

#endif
