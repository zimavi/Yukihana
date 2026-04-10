// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Yukihana.Core.Debug;
using Yukihana.Core.IO;
using Yukihana.Core.IO.Loaders;
using Yukihana.Core.IO.Loaders.Optional;
using Yukihana.Core.IO.RamFS;
using Yukihana.Core.IO.Vfs.Backends;
using Yukihana.Core.Resources;
using Yukihana.Core.Security;
using Sys = Cosmos.Kernel.System;

namespace Yukihana;

public class Kernel : Sys.Kernel
{
    public static AuthService AuthService { get; private set; } = null!;
    public static UserSession UserSession { get; private set; } = null!;
    
    public static bool SpeedrunShutdown { get; set; } = false;

    public static Kernel Instance { get; private set; } = null!;
    public static DateTime BootTime { get; } = DateTime.Now;

    protected override void BeforeRun()
    {
        Instance = this;

        Logger.ReportLevel = LogLevel.Trace;

        var ramfsTask = ShellPrint.CreateTask("Loading initramfs", "init").Progress(0).Work().Display();
        var initramfs = new InitRamFs(RamFsData.Data);
        ramfsTask.Ok().Display();

        ShellPrint.InfoK("Initializing VFS...", "init");

        ShellPrint.InfoK("Mounting initramfs as root", "init");

        VFS.Mount("/", initramfs);

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
        
        VFS.Mount("/tmp", new TempFs());
        VFS.Mount("/var", new TempFs());

        VFS.Mount("/dev", new DevFs());
        VFS.Mount("/proc", new ProcFs());

        VFS.Mount("/usr", new OverlayFs(new SubtreeFs(initramfs, "usr"), new TempFs()));
        VFS.Mount("/etc", new OverlayFs(new SubtreeFs(initramfs, "etc"), new TempFs()));

        ShellPrint.InfoK("Initializing user manager...", "init");
        
        AuthService = new AuthService(UserSystemInitializer.CreateDefault(out var toAuth));
        UserSession = new UserSession(toAuth);

        ShellPrint.InfoK(
            $"Logged in as '{toAuth.Name}' uid={toAuth.Id} gid={toAuth.PrimaryGroupId} root={toAuth.PrimaryGroupId == 0}", 
            "init");

        ShellPrint.OkK($"System initialization finished at {DateTime.Now:dd-MM-yyyy HH:mm:ss.fff}", "init");

        ShellPrint.KernelPrintEnabled = false;
        
        Stop();
    }

    protected override void Run()
    {
    }

    protected override void AfterRun()
    {
        ShellPrint.KernelPrintEnabled = true;
        if (SpeedrunShutdown)
            return;
        ShellPrint.InfoK("AfterRun", "AfterRun() called");
        ShellPrint.InfoK("AfterRun", "Goodbye!");
    }
}
