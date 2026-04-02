// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Yukihana.Core.Debug;
using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.IO;
using Yukihana.Core.IO.Loaders;
using Yukihana.Core.IO.Loaders.Optional;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.Resources;
using Sys = Cosmos.Kernel.System;

namespace Yukihana;

public class Kernel : Sys.Kernel
{
    public static RamFs RamFS { get; private set; } = null!;
    
    public static bool SpeedrunShutdown { get; set; } = false;

    public static Kernel Instance { get; private set; } = null!;

    protected override void BeforeRun()
    {
        Instance = this;

        Logger.ReportLevel = LogLevel.Trace;

        var ramfsTask = ShellPrint.CreateTask("Initializing...", "ramfs").Progress(0).Work().Display();

        RamFS = RamFs.FromArchive(RamFsData.Data).OrPanic("RamFS was not initialized");

        ramfsTask.Ok().Display();

        var fontGroup = new OptionalResourceGroup<FontState>(
            "Fonts",
            () => new FontState(),
            state => {},
            new RamFsResourceProvider(RamFS)
        );

        fontGroup.Add("fonts/zap-ext-light18.psf", "Console font", (s, data) => s.Font = PCScreenFont.LoadFont(data));

        fontGroup.TryLoad().Switch(
            some =>
            {
                KernelConsole.Default!.Font = some.Font;
                ShellPrint.InfoK("Applied font to console", "init");
            },
            () => {}
        );

        ShellPrint.OkK($"System initialization finished at {DateTime.UtcNow:dd-MM-yyyy HH:mm:ss.fff}", "init");
        
        ShellPrint.InfoK("Panicking for fun :)", "init");

        KernelPanic.Panic("Test panic");

    }

    protected override void Run()
    {
    }

    protected override void AfterRun()
    {
        if (SpeedrunShutdown)
            return;
        ShellPrint.InfoK("AfterRun", "AfterRun() called");
        ShellPrint.InfoK("AfterRun", "Goodbye!");
    }
}
