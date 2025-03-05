// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

namespace YukihanaOS.KernelRelated.Managers
{
    internal interface IManager
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsInitialized { get; }

        public bool Initialize(out string error);
        public void Shutdown();
    }
}
