#if !NETFRAMEWORK

using System;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using StatsdClient;

namespace Tests
{
    /// <summary>
    /// Integration tests for the native libfs library on Linux.
    /// These tests interact directly with the native library rather than mocking.
    /// </summary>
    [TestFixture]
    public class NativeLibraryTests
    {
        private FileSystem _fileSystem;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new FileSystem();
        }

        [Test]
        public void TryStat_WithValidFile_ReturnsTrue()
        {
            // Only run on Linux where the native library is available
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            // Use a file that should always exist on Linux
            var testPath = "/proc/self/exe";

            var result = _fileSystem.TryStat(testPath, out ulong inode);

            Assert.IsTrue(result, "TryStat should succeed for valid file");
            Assert.Greater(inode, 0UL, "Inode should be greater than 0");
        }

        [Test]
        public void TryStat_WithNonExistentFile_ReturnsFalse()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            var testPath = "/this/path/does/not/exist/file.txt";

            var result = _fileSystem.TryStat(testPath, out ulong inode);

            Assert.IsFalse(result, "TryStat should fail for non-existent file");
            Assert.AreEqual(0UL, inode, "Inode should be 0 on failure");
        }

        [Test]
        public void TryStat_WithSameFileTwice_ReturnsSameInode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            var testPath = "/proc/self/exe";

            var result1 = _fileSystem.TryStat(testPath, out ulong inode1);
            var result2 = _fileSystem.TryStat(testPath, out ulong inode2);

            Assert.IsTrue(result1, "First TryStat should succeed");
            Assert.IsTrue(result2, "Second TryStat should succeed");
            Assert.AreEqual(inode1, inode2, "Same file should have same inode");
        }

        [Test]
        public void TryStat_WithDifferentFiles_ReturnsDifferentInodes()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            var path1 = "/proc/self/exe";
            var path2 = "/proc/self/cmdline";

            var result1 = _fileSystem.TryStat(path1, out ulong inode1);
            var result2 = _fileSystem.TryStat(path2, out ulong inode2);

            Assert.IsTrue(result1, "First TryStat should succeed");
            Assert.IsTrue(result2, "Second TryStat should succeed");
            Assert.AreNotEqual(inode1, inode2, "Different files should have different inodes");
        }

        [Test]
        public void TryStat_WithTemporaryFile_ReturnsValidInode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            // Create a temporary file
            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFile, "test content");

                var result = _fileSystem.TryStat(tempFile, out ulong inode);

                Assert.IsTrue(result, "TryStat should succeed for temp file");
                Assert.Greater(inode, 0UL, "Inode should be greater than 0");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void TryStat_WithDirectory_ReturnsValidInode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            var testPath = "/proc/self";

            var result = _fileSystem.TryStat(testPath, out ulong inode);

            Assert.IsTrue(result, "TryStat should succeed for directory");
            Assert.Greater(inode, 0UL, "Inode should be greater than 0 for directory");
        }

        [Test]
        public void TryStat_OnNonLinux_ReturnsFalse()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on non-Linux platforms");
            }

            var testPath = "/some/path";

            var result = _fileSystem.TryStat(testPath, out ulong inode);

            Assert.IsFalse(result, "TryStat should return false on non-Linux platforms");
            Assert.AreEqual(0UL, inode, "Inode should be 0 on non-Linux platforms");
        }

        [Test]
        public void TryStat_VerifyHostCgroupNamespaceInode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            var cgroupNsPath = "/proc/self/ns/cgroup";

            // This test verifies we can read the cgroup namespace inode
            // It will be 0xEFFFFFFB (4026531835) if running in the host namespace
            var result = _fileSystem.TryStat(cgroupNsPath, out ulong inode);

            Assert.IsTrue(result, "TryStat should succeed for cgroup namespace");
            Assert.Greater(inode, 0UL, "Inode should be greater than 0");

            // Note: We don't assert the specific value because it depends on whether
            // we're running in a container or on the host
            Console.WriteLine($"Cgroup namespace inode: {inode} (0x{inode:X})");
        }

        [Test]
        public void TryStat_WithSymlink_ReturnsTargetInode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            // /proc/self/exe is typically a symlink to the actual executable
            var symlinkPath = "/proc/self/exe";

            var result = _fileSystem.TryStat(symlinkPath, out ulong inode);

            Assert.IsTrue(result, "TryStat should succeed for symlink");
            Assert.Greater(inode, 0UL, "Inode should be greater than 0");

            // stat() follows symlinks by default, so we should get the target's inode
            // We can't easily verify this without lstat support, but at least verify it works
        }

        [Test]
        public void NativeMethods_DirectCall_WithValidFile()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Ignore("Test only runs on Linux");
            }

            var testPath = "/proc/self/exe";

            // Test the NativeMethods wrapper directly
            var result = NativeMethods.TryGetInode(testPath, out ulong inode);

            Assert.IsTrue(result, "NativeMethods.TryGetInode should succeed");
            Assert.Greater(inode, 0UL, "Inode should be greater than 0");
        }

        [Test]
        public void NativeMethods_IsSupported_ReflectsPlatform()
        {
            var isSupported = NativeMethods.IsSupported;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.IsTrue(isSupported, "IsSupported should be true on Linux");
            }
            else
            {
                Assert.IsFalse(isSupported, "IsSupported should be false on non-Linux");
            }
        }
    }
}
#endif
