// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;

namespace Yukihana.Core.Extensions.System;

public static class DateTimeExtensions
{
    private static readonly int[] FRACTION_DIVISORS =
    {
        1_000_000, // 1 digit
        100_000,   // 2 digits
        10_000,    // 3 digits
        1_000,     // 4 digits
        100,       // 5 digits
        10,        // 6 digits
        1          // 7 digits
    };

    public static string ToFastString(this DateTime dt, string format)
    {
        if (format is null)
            throw new ArgumentNullException(nameof(format));

        if (format.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(format.Length + 16);

        for (int i = 0; i < format.Length; i++)
        {
            char c = format[i];

            int count = 1;
            while (i + count < format.Length && format[i + count] == c)
                count++;

            i += count - 1;

            switch (c)
            {
                case 'y':
                    if (count == 2)
                    {
                        AppendNumber(sb, dt.Year % 100, 2);
                    }
                    else
                    {
                        AppendNumber(sb, dt.Year, count);
                    }
                    break;

                case 'M':
                    AppendNumber(sb, dt.Month, count);
                    break;

                case 'd':
                    AppendNumber(sb, dt.Day, count);
                    break;

                case 'H':
                    AppendNumber(sb, dt.Hour, count);
                    break;

                case 'm':
                    AppendNumber(sb, dt.Minute, count);
                    break;

                case 's':
                    AppendNumber(sb, dt.Second, count);
                    break;

                case 'f':
                {
                    int fraction = (int)(dt.Ticks % TimeSpan.TicksPerSecond);

                    if (count <= 7)
                    {
                        int value = fraction / FRACTION_DIVISORS[count - 1];
                        AppendNumber(sb, value, count);
                    }
                    else
                    {
                        AppendNumber(sb, fraction, 7);
                        sb.Append('0', count - 7);
                    }

                    break;
                }

                default:
                    sb.Append(c, count);
                    break;
            }
        }

        return sb.ToString();
    }

    private static void AppendNumber(StringBuilder sb, int value, int minDigits)
    {
        Span<char> buffer = stackalloc char[10];
        int pos = buffer.Length;

        uint v = (uint)value;

        do
        {
            buffer[--pos] = (char)('0' + (v % 10));
            v /= 10;
        }
        while (v != 0);

        int digitCount = buffer.Length - pos;
        int padding = minDigits - digitCount;

        if (padding > 0)
            sb.Append('0', padding);

        for (int i = pos; i < buffer.Length; i++)
            sb.Append(buffer[i]);
    }
}