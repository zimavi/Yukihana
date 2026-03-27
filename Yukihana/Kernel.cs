using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Yukihana.Core.Debug;
using Yukihana.Core.Generated;
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

        var ramfsTask = ShellPrint.CreateTask("Initializing...", "RamFS").Progress(0).Work().Display();

        RamFS = new(RamFsData.Blob, RamFsData.Files);

        ramfsTask.Ok().Display();

        var fontGroup = new OptionalResourceGroup<FontState>(
            "Fonts",
            () => new FontState(),
            state => {},
            new RamFsResourceProvider(RamFS)
        );

        fontGroup.Add("fonts/zap-ext-light18.psf", "Console font", (s, data) => s.Font = PCScreenFont.LoadFont(data));

        var result = fontGroup.TryLoad();

        result.Switch(
            some =>
            {
                KernelConsole.Default!.Font = some.Font;
                ShellPrint.InfoK("Applied font to console", "init");
            },
            () => {}
        );
        
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
