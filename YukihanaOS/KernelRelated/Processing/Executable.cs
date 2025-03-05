// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using YukihanaOS.KernelRelated.Compression;

namespace YukihanaOS.KernelRelated.Processing
{
    internal sealed class Executable
    {
        private const string APPLICATION_SIGNATURE = "CEXE";
        private const string LIBRARY_SIGNATURE = "CLIB";
        private const int SIGNATURE_SIZE = 4;
        private const int ARCHIVE_SIZE_LENGTH = 4;

        public string Signature { get; private set; }
        public bool IsLibrary => Signature == LIBRARY_SIGNATURE;
        public string Name { get; private set; }
        public List<string> Dependencies { get; private set; }
        public byte[] RawData { get; private set; }
        public int ArchiveSize { get; private set; }
        public Dictionary<string, byte[]> LuaSources { get; set; }
        private byte[] _zipContent;

        public Executable(byte[] executableBytes)
        {
            RawData = executableBytes;
            LuaSources = new Dictionary<string, byte[]>();
            Dependencies = new List<string>();
            ParseExecutable(executableBytes);
        }

        private void ParseExecutable(byte[] executableBytes)
        {
            Signature = Encoding.ASCII.GetString(executableBytes, 0, SIGNATURE_SIZE);

            if (Signature != APPLICATION_SIGNATURE && Signature != LIBRARY_SIGNATURE)
                throw new InvalidOperationException("This is not a Cosmos executable or library");

            ArchiveSize = BitConverter.ToInt32(executableBytes, SIGNATURE_SIZE);

            if (SIGNATURE_SIZE + ARCHIVE_SIZE_LENGTH + ArchiveSize > executableBytes.Length)
                throw new InvalidOperationException("Cosmos executable corrupted");

            _zipContent = new byte[ArchiveSize];

            Array.Copy(executableBytes, SIGNATURE_SIZE + ARCHIVE_SIZE_LENGTH, _zipContent, 0, ArchiveSize);

            ExtractLuaScripts();
        }

        private void ExtractLuaScripts()
        {
            bool mainFound = false;

            using(MemoryStream zipStream = new MemoryStream(_zipContent))
            {
                using(ZipStorer zip = ZipStorer.Open(zipStream, FileAccess.Read))
                {
                    List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

                    foreach(ZipStorer.ZipFileEntry entry in dir)
                    {
                        using(MemoryStream fileStream = new MemoryStream())
                        {
                            zip.ExtractFile(entry, fileStream);
                            byte[] script = fileStream.ToArray();
                            if(entry.FilenameInZip == "package.json")
                            {
                                string json = Encoding.UTF8.GetString(script);

                                DataNode root = JSONReader.ReadFromString(json);

                                foreach(var node in root["dependencies"])
                                {
                                    if (node == null)
                                        break;
                                    Dependencies.Add(node.Value);
                                }

                                Name = root["name"].Value;
                                continue;
                            }

                            LuaSources.Add(entry.FilenameInZip, script);

                            if(entry.FilenameInZip == "main.lua")
                                mainFound = true;
                        }
                    }
                }
            }

            if (!mainFound && !IsLibrary)
                throw new EntryPointNotFoundException("Runnable executable must contain entry point.");
        }
    }
}
