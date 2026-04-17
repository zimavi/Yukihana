// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.Debug;

public enum BootStage
{
    EarlyKernel,
    CoreInit,
    ServiceInit,
}

public static class BootEnvironment
{
    public static BootStage Stage { get; set; } = BootStage.EarlyKernel;
}
