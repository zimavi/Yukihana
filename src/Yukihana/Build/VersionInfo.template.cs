// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

// This file is used as a template during the build.
// The generated source is written to obj/VersionInfo.g.cs.

namespace Yukihana.Core;

public static class VersionInfo
{
    public static readonly FrameworkInfo Framework = new(
        name: "CosmosOS",
        version: new Version(
            /*COSMOS_VERSION*/ 0, 0, 0)
    );

    public static readonly KernelInfo Kernel = new(
        name: "Yukihana",
        version: new Version(
            /*YUKIHANA_VERSION*/ 0, 0, 0),
        revision: new DateTime(
            /*YEAR*/ 2000,
            /*MONTH*/ 1,
            /*DAY*/ 1,
            /*HOUR*/ 0,
            /*MINUTE*/ 0,
            0,
            DateTimeKind.Utc)
    );
}