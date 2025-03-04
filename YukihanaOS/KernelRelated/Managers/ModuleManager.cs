// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System.Collections.Generic;
using YukihanaOS.KernelRelated.Debug;
using YukihanaOS.KernelRelated.Modules;
using YukihanaOS.KernelRelated.Utils;

namespace YukihanaOS.KernelRelated.Managers
{
    internal class ModuleManager : IManager
    {
        public string Name => "ModuleManager";
        public string Description => "Manages kernel modules";
        public bool IsInitialized => _isInitialized;

        private bool _isInitialized;

        public List<IKernelModule> _modules { get; private set; }

        public bool Initialize(out string error)
        {
            _modules = new List<IKernelModule>()
            {
#if MOD_COROUTINES
                new CoroutineModule(),
#endif
#if MOD_TTY
                new TtyModule(),
#endif
            };

            foreach (var module in _modules)
            {
                ShellPrint.WorkK("Initialize modules: " + module.Name);
                
                if(!module.Initialize(out string err))
                {
                    ShellPrint.ErrorK("Initialize modules: " + module.Name, true);
                    KernelPanic.Panic("Module initialization failed: " + err);
                }

                ShellPrint.OkK("Initialize modules: " + module.Name);
            }

            ShellPrint.OkK("All kernel modules have been initialized (" + _modules.Count + ")", true);

            _isInitialized = true;
            error = null;
            return true;
        }

        public void Shutdown()
        {
            _isInitialized = false;
        }

        public IKernelModule GetModuleInstance(string name)
        {
            foreach (var module in _modules)
            {
                if (module.Name.Equals(name))
                {
                    return module;
                }
            }
            return null;
        }

        public object SendModuleMessage(string name, out bool hasBeenSent, params object[] args)
        {
            foreach (var module in _modules)
            {
                if (module.Name.Equals(name))
                {
                    hasBeenSent = true;
                    return module.SendMessage(args);
                }
            }
            hasBeenSent = false;
            return null;
        }

    }
}
