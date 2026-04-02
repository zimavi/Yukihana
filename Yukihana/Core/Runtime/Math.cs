// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Runtime;

namespace Yukihana.Core.Runtime;

internal static class Math
{
    
    [RuntimeExport("modf")]
    internal static unsafe double ModF(double x, double* intptr)
    {
        long intPart = (long)x;
        *intptr = (double)intPart;
        return x - intPart;
    }
}