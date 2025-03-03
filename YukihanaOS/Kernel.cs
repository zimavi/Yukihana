using Cosmos.Core.Multiboot;
using System.Threading;
using YukihanaOS.KernelRelated.Utils;
using Sys = Cosmos.System;

namespace YukihanaOS
{
    public class Kernel : Sys.Kernel
    {

        public const string KERNEL_VERSION = "HikariKernel v1.0";

        protected override void BeforeRun()
        {
            ShellPrint.WorkK("Booting up: " + KERNEL_VERSION + "...");
            Thread.Sleep(3000);
            ShellPrint.OkK("Booting up");
        }

        // This shouldn't run
        protected override void Run()
        { }
    }
}
