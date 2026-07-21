// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Primitives;

namespace Yukihana.Core.Extensions.System;

public static class NullableExtensions
{
    public static Option<T> ToOption<T>(this T? nullable) => Option<T>.From(nullable);
}
