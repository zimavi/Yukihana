// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

namespace YukihanaOS.KernelRelated.Modules
{
    internal interface IKernelModule
    {
        public string Name { get; }
        public string Description { get; }

        public object SendMessage(params object[] args);
        public bool Initialize(out string error);
        public void Shutdown();
    }
}
