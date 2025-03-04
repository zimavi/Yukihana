// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.IO;
using Cosmos.System.Graphics.Fonts;
using YukihanaOS.KernelRelated.Debug;
using YukihanaOS.KernelRelated.Resources;

namespace YukihanaOS.KernelRelated.Utils
{
    internal class Resource
    {
        public string Path { get; }
        public Action<byte[]> LoadCallback { get; }
        public string Description { get; }

        public Resource(string path, string desc, Action<byte[]> callback)
        {
            Path = path;
            Description = desc;
            LoadCallback = callback;
        }
    }
    internal static class BootstrapResourceLoader
    {
        private static void handleResourceLoadingError(Exception ex, string resourceName, string error)
        {
            Logger.DoBootLog("Cannot load resource '" + resourceName + "' -> " +  ex.Message);
            ShellPrint.ErrorK("Loading '" + resourceName + "'", true);
            KernelPanic.Panic("Kernel load failed: Invalid resource file or it was not found");
        }

        public static void LoadResources()
        {
            ShellPrint.WorkK("Loading kernel resources");

            var resources = new List<Resource>
            {
                new(@"\boot\.resources\fonts\zap-ext-light18.psf", "Font 'zap-ext-light18.psf'", fontData => Fonts.Font18 = PCScreenFont.LoadFont(fontData))
            };

            int loadedResources = 0;
            foreach (var disk in Kernel.FileSystem.GetDisks())
            {
                foreach (var part in disk.Partitions)
                {
                    if (!part.HasFileSystem)
                        continue;

                    foreach(var resource in resources)
                    {
                        string resourcePath = part.RootPath + resource.Path;

                        if (File.Exists(resourcePath))
                        {
                            try
                            {
                                Logger.DoBootLog("Found '" + resourcePath + "'");
                                ShellPrint.WorkK("Loading '" + resource.Description + "'");

                                byte[] rawData = File.ReadAllBytes(resourcePath);
                                resource.LoadCallback?.Invoke(rawData);

                                loadedResources++;

                                ShellPrint.OkK("Loading '" + resource.Description + "'");
                            }
                            catch (ArgumentException ex)
                            {
                                handleResourceLoadingError(ex, resource.Description, "Invalid file");
                            }
                            catch (Exception ex)
                            {
                                handleResourceLoadingError(ex, resource.Description, ex.Message);
                            }
                        }
                    }
                    if (loadedResources >= resources.Count)
                        break;
                }
                if (loadedResources >= resources.Count)
                    break;
            }

            if (loadedResources < resources.Count)
            {
                ShellPrint.ErrorK("Loading kernel resources failed. Missing files. Found (" + loadedResources + "/" + resources.Count + ")");
                KernelPanic.Panic("Kernel load failed: Unable to locate required resources");
            }

            ShellPrint.OkK("Loading kernel resources", true);
        }
    }
}
