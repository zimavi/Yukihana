// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using IL2CPU.API.Attribs;
using YukihanaOS.KernelRelated.Debug;
using static Cosmos.Core.INTs;

namespace YukihanaPlugs
{
    [Plug(Target = typeof(Cosmos.Core.INTs))]
    public class INTs
    {
        public static void HandleException(uint aEIP, string aDescription, string aName, ref IRQContext ctx, uint lastKnownAddressValue = 0)
        {
            const string hex = "0123456789abcdef";

            string ctxInterrupt = "";
            ctxInterrupt += hex[(int)((ctx.Interrupt >> 4) & 0xf)];
            ctxInterrupt += hex[(int)(ctx.Interrupt & 0xf)];

            string lastKnownAddress = "";

            if (lastKnownAddressValue != 0)
            {
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 28) & 0xf)];
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 24) & 0xf)];
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 20) & 0xf)];
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 16) & 0xf)];
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 12) & 0xf)];
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 8) & 0xf)];
                lastKnownAddress += hex[(int)((lastKnownAddressValue >> 4) & 0xf)];
                lastKnownAddress += hex[(int)(lastKnownAddressValue & 0xf)];
            }

            KernelPanic.HW_Panic("Unknown CPU exception accured", new YukihanaOS.KernelRelated.Debug.HALException(aName, aDescription, lastKnownAddress, ctxInterrupt));
        }
    }
}
