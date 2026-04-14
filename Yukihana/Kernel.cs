// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Reflection;
using Cosmos.Kernel.Core.IO;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Yukihana.Core.Debug;
using Yukihana.Core.Extensions.Primitives;
using Yukihana.Core.Extensions.System;
using Yukihana.Core.IO;
using Yukihana.Core.IO.Loaders;
using Yukihana.Core.IO.Loaders.Optional;
using Yukihana.Core.IO.Vfs.Backends;
using Yukihana.Core.Resources;
using Yukihana.Core.Security;
using Sys = Cosmos.Kernel.System;

namespace Yukihana;

public class Kernel : Sys.Kernel
{
    public static AuthService AuthService { get; private set; } = null!;
    public static UserSession UserSession { get; private set; } = null!;

    public static DateTime BootTime { get; }

    private const string RAMFS_PATH = "Yukihana.Core.Resources";
    private const string RAMFS_FILE = "initramfs.cpio.gz";

    private static string _ramfs_resource_key => string.Join('.', RAMFS_PATH, RAMFS_FILE);

    private static readonly Logger _kernelLogger;

    static Kernel()
    {
        BootTime = DateTime.Now;

        _kernelLogger = new();
        _kernelLogger.Info("Static Kernel constructor called");
        _kernelLogger.Info($"Booted at {BootTime:dd-MM-yyyy HH:mm:ss.fff}");
    }

    protected override void BeforeRun()
    {
        try
        {
            KernelLog.LogToScreen = true;
            Init();
        }
        catch
        {
            KernelPanic.Panic("Unhandled exception during boot");
        }
    }

    private void Init()
    {
        //
        // STAGE 1 -> Early boot
        //
        
        var logger = new Logger("init");

        logger.Info($"Fetching \"{RAMFS_FILE}\".");
        byte[] ramfsBytes;

        var assembly = Assembly.GetExecutingAssembly();

        using (var result = assembly.GetManifestResourceStream(_ramfs_resource_key).ToOption())
        using (var memStream = new MemoryStream())
        {
            var stream = result.OrPanic($"Unable to locate \"{RAMFS_FILE}\".");
            stream.CopyTo(memStream);
            ramfsBytes = memStream.ToArray();
        }

        logger.Info("Loading initramfs.");
        var initramfs = new InitRamFs(ramfsBytes);

        logger.Info("Mounting initramfs as root.");
        VFS.Mount("/", initramfs);

        var fontGroup = new OptionalResourceGroup<FontState>(
            name:           "Fonts",
            createState:    () => new FontState(),
            commit:         state => {},
            provider:       new VfsResourceProvider()
        );

        fontGroup.Add(
            relativePath:   "/usr/share/fonts/zap-ext-light18.psf", 
            description:    "Console font", 
            applyCallback:  (s, data) => s.Font = PCScreenFont.LoadFont(data)
        );

        fontGroup.TryLoad().Switch(
            some =>
            {
                KernelConsole.Default!.Font = some.Font;
                logger.Info("Applyied font to console.");
            },
            none: () => {}
        );

        logger.Info("Mounting partitions.");

        VFS.Mount("/tmp", new TempFs());
        VFS.Mount("/var", new TempFs());

        VFS.Mount("/root", new TempFs());
        VFS.Mount("/home", new TempFs());

        VFS.Mount("/dev", new DevFs());
        VFS.Mount("/proc", new ProcFs());

        VFS.Mount("/etc", new TempFs());

        var store = UserSystemInitializer.CreateDefault();

        if (!VFS.FileExists("/etc/passwd") || !VFS.FileExists("/etc/shadow") || !VFS.FileExists("/etc/group"))
        {
            logger.Warn("Unable to locate user files. Trying to save defaults.");

            var snapshot = IdentitySerializer.Serialize(store);

            VFS.WriteAllText("/etc/passwd", snapshot.Passwd);
            VFS.WriteAllText("/etc/shadow", snapshot.Shadow);
            VFS.WriteAllText("/etc/group", snapshot.Group);
        }
        else
        {
            var passwd = VFS.ReadAllText("/etc/passwd");
            var shadow = VFS.ReadAllText("/etc/shadow");
            var group = VFS.ReadAllText("/etc/group");

            if (passwd.IsSuccess && shadow.IsSuccess && group.IsSuccess)
            {
                store = IdentitySerializer.Deserialize(
                    passwd.Value,
                    shadow.Value,
                    group.Value);
            }
            else
                logger.Warn("Unable to read user files. Using defaults.");
        }

        logger.Info("Initalizing user auth service and empty user session.");

        AuthService = new AuthService(store);
        UserSession = new UserSession(User.None);

        logger.Info($"Base kernel initialization finished at {DateTime.Now:dd-MM-yyyy HH:mm:ss.fff}.");

        KernelPanic.Panic("test");

        Stop();
    }

    protected override void Run()
    { }

    protected override void AfterRun()
    {
        Console.WriteLine("\n");
        
        _kernelLogger.Info("Reached AfterRun().");

        _kernelLogger.Info("Kernel cleanup finished.");
        _kernelLogger.Info("Bye.");
    }
}
