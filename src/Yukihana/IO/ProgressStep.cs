// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.IO;

public readonly struct ProgressStep(long processed, long total, bool completed)
{
    public readonly long Processed = processed;
    public readonly long Total = total;
    public readonly int Percent = total <= 0 ? 0 : (int)(processed * 100 / total);
    public readonly bool IsCompleted = completed;

    public override string ToString()
        => $"{Percent}% ({Processed}/{Total})";
}
