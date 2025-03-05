// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Collections.Generic;
using Cosmos.Core;
using Cosmos.System.Coroutines;
using Cosmos.System.FileSystem;
using Cosmos.System.FileSystem.VFS;
using YukihanaOS.KernelRelated.Debug;
using YukihanaOS.KernelRelated.Managers;
using YukihanaOS.KernelRelated.Managers.PipesIO;
using YukihanaOS.KernelRelated.Modules;
using YukihanaOS.KernelRelated.Resources;
using YukihanaOS.KernelRelated.Utils;
using Sys = Cosmos.System;

namespace YukihanaOS
{
    public class Kernel : Sys.Kernel
    {

        public const string KERNEL_VERSION = "HikariKernel v1.0";

        public static string CPU_Brand { get; private set; }
        public static string CPU_Vendor { get; private set; }
        public static long CPU_ClockSpeed { get; private set; }
        public static ulong CPU_Uptime { get; private set; }

        #region Managers

        internal static ModuleManager ModuleManager { get; private set; }
        public static IOManager IOManager { get; private set; }

        // Made to avoid calling IOManager every time you need IO
        public static IPipeIO IO => IOManager.Pipe;

        #endregion

        public static CosmosVFS FileSystem { get; private set; }

        protected override void BeforeRun()
        {
            try
            {
                IOManager = new IOManager();
                IOManager.Initialize(out _);

                ShellPrint.WorkK("Collecting hardware info");
                if (!CollectCPUID(out string error))
                    ShellPrint.WarnK("Collecting hardware info: Failed to collect hardware info", true);
                else
                    ShellPrint.OkK("Collecting hardware info");

                // VFX won't be as kernel component as there aren't options
                ShellPrint.PrintK("Initializing VFS");
                FileSystem = new();
                VFSManager.RegisterVFS(FileSystem);

                BootstrapResourceLoader.LoadResources();

                ModuleManager = new ModuleManager();
                if (!ModuleManager.Initialize(out error))
                {
                    KernelPanic.Panic("Module manager threw and exception: " + error);
                }

#if MOD_TTY
                Logger.DoBootLog("Initializing TTY screen");
                ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)0, (uint)1280, (uint)720, Fonts.Font18);
                Logger.DoBootLog("Piping IO to TTY");
                IOManager.Pipe = new TtyPipe();
#endif

#if MOD_COROUTINES
                ModuleManager.SendModuleMessage(nameof(CoroutineModule), out _, (uint)0, new Action(SystemThread));
                ModuleManager.SendModuleMessage(nameof(CoroutineModule), out _, (uint)2, new Coroutine(CoroutineExample()));
                ModuleManager.SendModuleMessage(nameof(CoroutineModule), out _, (uint)1);
#else
            while(true)
            {
                SystemThread();
            }
#endif
            }
            catch (Exception ex)
            {
                KernelPanic.Panic(ex.Message);
            }
        }

        // This shouldn't run
        protected override void Run()
        { }

        private bool CollectCPUID(out string error)
        {
            try
            {
                if (CPU.CanReadCPUID() != 0)
                {
                    CPU_Brand = CPU.GetCPUBrandString();
                    CPU_Vendor = CPU.GetCPUVendorName();
                    CPU_Uptime = CPU.GetCPUUptime();

                    // should run last as may through exception
                    CPU_ClockSpeed = CPU.GetCPUCycleSpeed();
                    error = null;
                    return true;
                }
                else
                {
                    error = "Cannot read CPUID";
                    return false;
                }
            }
            catch(Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private void SystemThread()
        {
            // #if MOD_TTY
            //             ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, "System thread!");
            // #else
            //             Console.WriteLine("System thread!");
            // #endif

            IO.WriteLine("System thread!");
        }

        private IEnumerator<CoroutineControlPoint> CoroutineExample()
        {
            while (true)
            {
                // #if MOD_TTY
                //                 ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, "I'm running once per 3 seconds!");
                // #else
                //                 Console.WriteLine("I'm running once per 3 seconds!");
                // #endif
                IO.WriteLine("I'm running once per 3 seconds!");

                yield return WaitFor.Seconds(3);
            }
        }
    }
}
