// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Yukihana.Core.Debug;
using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.IO;
using Yukihana.Core.IO.Loaders;
using Yukihana.Core.IO.Loaders.Optional;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.IO.Vfs.Backends;
using Yukihana.Core.Resources;
using Sys = Cosmos.Kernel.System;

namespace Yukihana;

public class Kernel : Sys.Kernel
{
    private static RamFs _initRamFs = null!;
    
    public static bool SpeedrunShutdown { get; set; } = false;

    public static Kernel Instance { get; private set; } = null!;

    protected override void BeforeRun()
    {
        Instance = this;

        Logger.ReportLevel = LogLevel.Trace;

        var ramfsTask = ShellPrint.CreateTask("Loading initramfs", "init").Progress(0).Work().Display();

        _initRamFs = RamFs.FromArchive(RamFsData.Data).OrPanic("RamFS was not initialized");

        ramfsTask.Ok().Display();

        ShellPrint.InfoK("Initializing VFs...", "init");

        ShellPrint.InfoK("Mounting initramfs as root", "init");

        VFS.Mount("/", _initRamFs);
        VFS.Mount("/tmp", new TempFs());
        VFS.Mount("/var", new TempFs());
        VFS.Mount("/proc", new TempFs());
        VFS.Mount("/sys", new TempFs());

        VFS.Mount("/usr", new OverlayFs(new SubtreeFs(_initRamFs, "usr"), new TempFs()));
        VFS.Mount("/etc", new OverlayFs(new SubtreeFs(_initRamFs, "etc"), new TempFs()));

        var fontGroup = new OptionalResourceGroup<FontState>(
            "Fonts",
            () => new FontState(),
            state => {},
            new VfsResourceProvider()
        );

        fontGroup.Add("/usr/share/fonts/zap-ext-light18.psf", "Console font", (s, data) => s.Font = PCScreenFont.LoadFont(data));

        fontGroup.TryLoad().Switch(
            some =>
            {
                KernelConsole.Default!.Font = some.Font;
                ShellPrint.InfoK("Applied font to console", "init");
            },
            () => {}
        );

        ShellPrint.OkK($"System initialization finished at {DateTime.UtcNow:dd-MM-yyyy HH:mm:ss.fff}", "init");

        ShellPrint.InfoK($"Testing VFS reading", "init");
        var str = VFS.ReadAllText("/test.txt").OrPanic("Could not read file").Trim();
        ShellPrint.InfoK($"/test.txt -> '{str}'", "init");

        ShellPrint.InfoK($"Testing VFS streams (write)", "init");

        string msg = "Hello, world!";
        var u8 = Encoding.UTF8.GetBytes(msg);

        using (var stream = VFS.Open("/tmp/test.txt", FileMode.CreateNew, FileAccess.Write).OrPanic("Failed to create stream"))
        {
            stream.Write(u8);
            stream.Flush();
        }

        ShellPrint.InfoK($"Testing VFS streams (read)", "init");
        using (var stream = VFS.Open("/tmp/test.txt", FileMode.Open, FileAccess.Read).OrPanic("Failed to open stream"))
        {
            byte[] buffer = new byte[u8.Length];
            int read = stream.Read(buffer);
            ShellPrint.InfoK($"Read {read} bytes", "init");
            string content = Encoding.UTF8.GetString(buffer);
            ShellPrint.InfoK($"/tmp/test.txt -> '{content}'", "init");
        }

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
