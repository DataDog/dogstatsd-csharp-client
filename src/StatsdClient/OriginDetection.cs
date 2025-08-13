using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace StatsdClient
{
    /// <summary>
    /// Functions for detecting the origin via cgroups or the `DD_EXTERNAL_ENV` environment variable.
    /// </summary>
    internal class OriginDetection
    {
        /// <summary>
        /// The controller used to identify the container-id for cgroup v1
        /// </summary>
        private const string CgroupV1BaseController = "memory";

        /// <summary>
        /// Host namespace inode number (hardcoded in the Linux kernel)
        /// </summary>
        private const ulong HostCgroupNamespaceInode = 0xEFFFFFFB;

        private const string UuidSource = @"[0-9a-f]{8}[-_][0-9a-f]{4}[-_][0-9a-f]{4}[-_][0-9a-f]{4}[-_][0-9a-f]{12}";
        private const string ContainerSource = @"[0-9a-f]{64}";
        private const string TaskSource = @"[0-9a-f]{32}-\d+";

        private static readonly Regex ExpLine = new Regex(@"^\d+:[^:]*:(.+)$", RegexOptions.Compiled);
        private static readonly Regex ExpContainerId = new Regex(
            "(" + UuidSource + "|" + ContainerSource + "|" + TaskSource + @")(?:\.scope)?$",
            RegexOptions.Compiled);

        // --- mountinfo fallback for cgroup v2 / containerd ---
        private static readonly string MountInfoPattern =
            @".*/([^\s/]+)/(" +
             @"[0-9a-f]{64}" + "|" +
             @"[0-9a-f]{32}-\d+" + "|" +
             @"[0-9a-f]{8}(?:-[0-9a-f]{4}){4}" +
             @")/[\S]*hostname";

        private static readonly Regex MountInfoRegex = new Regex(MountInfoPattern, RegexOptions.Compiled);

        private IFileSystem _fs;

        /// <summary>
        /// Initializes a new instance of the <see cref="OriginDetection"/> class.
        /// Create the class
        /// </summary>
        internal OriginDetection(IFileSystem fs, string containerID, bool originDetectionEnabled)
        {
            _fs = fs;
            ContainerID = GetContainerID(containerID, originDetectionEnabled);

            if (originDetectionEnabled)
            {
                // Read the external data from the environment variable called `DD_EXTERNAL_ENV`.
                //
                // This is injected via admission controller, in Kubernetes environments, to provide origin detection information
                // for clients that cannot otherwise detect their origin automatically.
                var externalData = Environment.GetEnvironmentVariable("DD_EXTERNAL_ENV");
                Initialize(externalData);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OriginDetection"/> class.
        /// externalData is hardcoded for use in tests.
        /// </summary>
        internal OriginDetection(string externalData)
             : this(externalData, string.Empty)
        {
        }

        internal OriginDetection(string externalData, string containerID)
        {
            Initialize(externalData);
            this.ContainerID = containerID;
        }

        /// <summary>
        /// Gets the Container ID set in the configuration.
        /// </summary>
        public string ContainerID { get; private set; }

        /// <summary>
        /// Gets the detected External Data configuration if it exists.
        /// </summary>
        internal string ExternalData { get; private set; }

        private void Initialize(string externalData)
        {
            // If we have external data, trim any leading or trailing whitespace, remove all non-printable characters, and remove all `|` characters.
            if (!string.IsNullOrEmpty(externalData))
            {
                ExternalData = Regex.Replace(externalData.Trim(), @"[\p{Cc}|]+", string.Empty);
            }
        }

        /// <summary>
        /// Detect if we're in the host's cgroup namespace
        /// </summary>
        /// <returns>true if the inode matches the host's cgroup namespace inode.</returns>
        private bool IsHostCgroupNamespace()
        {
            if (!_fs.TryStat("/proc/self/ns/cgroup", out ulong inode))
            {
                return false;
            }

            return inode == HostCgroupNamespaceInode;
        }

        /// <summary>
        /// Parse lines of /proc/self/cgroup into controller→path
        /// </summary>
        /// <returns>A dictionary of the node paths.</returns>
        private Dictionary<string, string> ParseCgroupNodePath(string content)
        {
            var res = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in content.Split('\n'))
            {
                var tokens = line.Split(':');
                if (tokens.Length != 3)
                {
                    continue;
                }

                if (tokens[1] == CgroupV1BaseController || tokens[1].Length == 0)
                {
                    res[tokens[1]] = tokens[2].TrimEnd();
                }
            }

            return res;
        }

        /// <summary>
        /// Try each controller (v1 and v2) to get an inode-based fallback
        /// </summary>
        /// <returns>
        /// The inode number for the processes cgroup directory, or empty
        /// string if it is unable to resolve the directory.
        /// </returns>
        private string GetCgroupInode(string cgroupMountPath, string procSelfCgroupPath)
        {
            string content;
            if (!_fs.TryReadAllText(procSelfCgroupPath, out content))
            {
                return string.Empty;
            }

            var paths = ParseCgroupNodePath(content);

            foreach (var controller in new[] { CgroupV1BaseController, string.Empty })
            {
                if (!paths.TryGetValue(controller, out var subpath))
                {
                    continue;
                }

                var segments = new List<string>
                {
                    cgroupMountPath.TrimEnd('/'),
                    controller.Trim('/'),
                    subpath.TrimStart('/'),
                };
                var full = Path.Combine(segments.FindAll(s => !string.IsNullOrEmpty(s)).ToArray());

                if (_fs.TryStat(full, out ulong ino))
                {
                    return "in-" + ino;
                }
            }

            return string.Empty;
        }

        // --- Container ID parsing ---
        private string ParseContainerID(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var m = ExpLine.Match(line);
                if (!m.Success || m.Groups.Count != 2)
                {
                    continue;
                }

                var candidate = m.Groups[1].Value;
                var idm = ExpContainerId.Match(candidate);
                if (idm.Success && idm.Groups.Count >= 2)
                {
                    return idm.Groups[1].Value;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Attempt to read the container ID from /proc/self/cgroup.
        /// </summary>
        /// <returns>The container ID.</returns>
        private string ReadContainerID(string path)
        {
            try
            {
                using (var sr = _fs.OpenText(path))
                {
                    return ParseContainerID(sr);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private string ParseMountInfo(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var matches = MountInfoRegex.Matches(line);
                if (matches.Count == 0)
                {
                    continue;
                }

                var m = matches[matches.Count - 1];
                var prefix = m.Groups[1].Value;
                var id = m.Groups[2].Value;
                if (!string.Equals(prefix, "sandboxes", StringComparison.Ordinal))
                {
                    return id;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Attempt to read the container ID from /proc/self/mountinfo.
        /// </summary>
        /// <returns>The container ID.</returns>
        private string ReadMountInfo(string path)
        {
            try
            {
                using (var sr = _fs.OpenText(path))
                {
                    return ParseMountInfo(sr);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 1. If user-supplied, use that.
        /// 2. Try /proc/self/cgroup (v1).
        /// 3. Try /proc/self/mountinfo.
        /// 4. If host ns, bail.
        /// 5. Finally fallback to inode.
        /// </summary>
        /// <returns>The container ID.</returns>
        private string GetContainerID(string userProvidedId, bool cgroupFallback)
        {
            if (!string.IsNullOrEmpty(userProvidedId))
            {
                return userProvidedId;
            }

            if (cgroupFallback)
            {
                var id = ReadContainerID("/proc/self/cgroup");
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                id = ReadMountInfo("/proc/self/mountinfo");
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                if (IsHostCgroupNamespace())
                {
                    return string.Empty;
                }

                return GetCgroupInode("/proc/self/mountinfo", "/proc/self/cgroup");
            }

            return string.Empty;
        }
    }
}
