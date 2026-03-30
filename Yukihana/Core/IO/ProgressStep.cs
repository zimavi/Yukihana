// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO;

public readonly struct ProgressStep
{
    public readonly long Processed;
    public readonly long Total;
    public readonly int Percent;
    public readonly bool IsCompleted;

    public ProgressStep(long processed, long total, bool completed)
    {
        Processed = processed;
        Total = total;
        IsCompleted = completed;

        Percent = total <= 0 ? 0 : (int)(processed * 100 / total);
    }

    public override string ToString()
        => $"{Percent}% ({Processed}/{Total})";
}