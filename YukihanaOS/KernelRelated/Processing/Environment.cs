// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System.Collections.Generic;

namespace YukihanaOS.KernelRelated.Processing
{
    internal static class Environment
    {
        public static Dictionary<uint, List<KeyValuePair<string, string>>> PerProcessEnvironmentVariables { get; } = new Dictionary<uint, List<KeyValuePair<string, string>>>();
    }
}
