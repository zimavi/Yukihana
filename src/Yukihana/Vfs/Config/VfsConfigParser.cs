// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text.RegularExpressions;
using Cosmos.Kernel.HAL.Vfs;
using Yukihana.Debug;

namespace Yukihana.Vfs.Config;

public sealed partial class VfsConfigParser
{
    private static readonly Regex s_fstabLineRegex = FstabRegex();

    private static readonly Logger s_logger = new("vfspar");

    public static IReadOnlyList<VfsMountConfig> Parse(string content)
    {
        s_logger.Info("Parsing fstab content");
        List<VfsMountConfig> configs = [];

        if (string.IsNullOrEmpty(content))
        {
            return configs;
        }

        string[] lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            Match match = s_fstabLineRegex.Match(trimmed);
            if (!match.Success)
            {
                continue;
            }

            VfsMountConfig config = new()
            {
                Source = match.Groups[1].Value,
                MountPoint = match.Groups[2].Value,
                FilesystemType = match.Groups[3].Value,
                Flags = ParseMountFlags(match.Groups[4].Value),
                DumpFrequency = int.Parse(match.Groups[5].Value),
                PassNumber = int.Parse(match.Groups[6].Value)
            };

            config.SourceType = DetectSourceType(config.Source);
            configs.Add(config);

            s_logger.Info($"Found config: src='{config.Source}', src_t='{config.SourceType}', mnt='{config.MountPoint}', fstype='{config.FilesystemType}', flags='{match.Groups[4].Value}'");
        }

        return configs;
    }

    private static MountFlags ParseMountFlags(string flagsString)
    {
        MountFlags flags = MountFlags.None;
        string[] flagList = flagsString.Split(',');

        foreach (string flag in flagList)
        {
            switch (flag.ToLowerInvariant())
            {
                case "ro":
                case "readonly":
                    flags |= MountFlags.ReadOnly;
                    break;
                case "rw":
                case "readwrite":
                    break;
                case "nosuid":
                    flags |= MountFlags.NoSuid;
                    break;
                case "nodev":
                    flags |= MountFlags.NoDev;
                    break;
                case "noexec":
                    flags |= MountFlags.NoExec;
                    break;
            }
        }

        return flags;
    }

    private static SourceType DetectSourceType(string source)
    {
        if (UuidRegex().IsMatch(source.AsSpan(5)))
        {
            return SourceType.Uuid;
        }

        if (source.StartsWith("LABEL="))
        {
            return SourceType.Label;
        }

        if (int.TryParse(source, out _))
        {
            return SourceType.PartitionIndex;
        }

        return SourceType.Path;
    }

    [GeneratedRegex(@"^([^#\s]+)\s+([^#\s]+)\s+([^#\s]+)\s+([^#\s]+)\s+(\d+)\s+(\d+)")]
    private static partial Regex FstabRegex();
    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex UuidRegex();
}
