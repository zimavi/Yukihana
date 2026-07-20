// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

namespace Yukihana.Core.IO.Vfs.Filesystem.Ext4.Superblock;

internal enum Ext4EncryptAlgorithms : byte
{
    Invalid = 0,
    Aes256Xts,
    Aes256Gcm,
    Aes256Cbc,
}