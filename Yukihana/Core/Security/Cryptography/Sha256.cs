// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using System.Text;

namespace Yukihana.Core.Security.Cryptography;

public static class Sha256
{
    private static readonly uint[] _k =
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
        0x3956c25f, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
        0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
        0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
        0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
        0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
        0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
        0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
        0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    ];

    public static byte[] ComputeHash(byte[] data)
    {
        uint h0 = 0x6a09e667;
        uint h1 = 0xbb67ae85;
        uint h2 = 0x3c6ef372;
        uint h3 = 0xa54ff53a;
        uint h4 = 0x510e527f;
        uint h5 = 0x9b05688c;
        uint h6 = 0x1f83d9ab;
        uint h7 = 0x5be0cd19;

        byte[] padded = Pad(data);

        uint[] w = new uint[64];

        for(int i = 0; i < padded.Length; i += 64)
        {
            for(int t = 0; t < 16; t++)
            {
                int j = i + t * 4;
                w[t] = ((uint)padded[j] << 24)
                     | ((uint)padded[j + 1] << 16)
                     | ((uint)padded[j + 2] << 8)
                     | padded[j + 3];
            }

            for(int t = 16; t < 64; t++)
            {
                uint s0 = SmallSigma0(w[t - 15]);
                uint s1 = SmallSigma1(w[t - 2]);
                w[t] = unchecked(w[t - 16] + s0 + w[t - 7] + s1);
            }

            uint a = h0;
            uint b = h1;
            uint c = h2;
            uint d = h3;
            uint e = h4;
            uint f = h5;
            uint g = h6;
            uint h = h7;

            for (int t = 0; t < 64; t++)
            {
                uint t1 = unchecked(h + BigSigma1(e) + Ch(e, f, g) + _k[t] + w[t]);
                uint t2 = unchecked(BigSigma0(a) + Maj(a, b, c));

                h = g;
                g = f;
                f = e;
                e = unchecked(d + t1);
                d = c;
                c = b;
                b = a;
                a = unchecked(t1 + t2);
            }

            h0 = unchecked(h0 + a);
            h1 = unchecked(h1 + b);
            h2 = unchecked(h2 + c);
            h3 = unchecked(h3 + d);
            h4 = unchecked(h4 + e);
            h5 = unchecked(h5 + f);
            h6 = unchecked(h6 + g);
            h7 = unchecked(h7 + h);
        }

        return ToBytes(h0, h1, h2, h3, h4, h5, h6, h7);
    }

    public static string ComputeHashString(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] Pad(byte[] data)
    {
        ulong bitLength = (ulong)data.Length * 8;

        int paddingLength = 64 - (int)((data.Length + 9) % 64);

        if (paddingLength < 0)
            paddingLength += 64;
        
        byte[] padded = new byte[data.Length + 1 + paddingLength + 8];

        Array.Copy(data, padded, data.Length);

        padded[data.Length] = 0x80;

        for(int i = 0; i < 8; i++)
        {
            padded[padded.Length - 1 - i] = (byte)(bitLength >> (8 * i));
        }

        return padded;
    }

    private static byte[] ToBytes(uint h0, uint h1, uint h2, uint h3, uint h4, uint h5, uint h6, uint h7)
    {
        byte[] result = new byte[32];

        WriteUInt(result, 0, h0);
        WriteUInt(result, 4, h1);
        WriteUInt(result, 8, h2);
        WriteUInt(result, 12, h3);
        WriteUInt(result, 16, h4);
        WriteUInt(result, 20, h5);
        WriteUInt(result, 24, h6);
        WriteUInt(result, 28, h7);

        return result;
    }

    private static void WriteUInt(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint RotateRight(uint x, int n) => (x >> n) | (x << (32 - n));

    private static uint Ch(uint x, uint y, uint z) => (x & y) ^ (~x & z);

    private static uint Maj(uint x, uint y, uint z) => (x & y) ^ (x & z) ^ (y & z);

    private static uint BigSigma0(uint x) =>
        RotateRight(x, 2) ^ RotateRight(x, 13) ^ RotateRight(x, 22);

    private static uint BigSigma1(uint x) =>
        RotateRight(x, 6) ^ RotateRight(x, 11) ^ RotateRight(x, 25);

    private static uint SmallSigma0(uint x) =>
        RotateRight(x, 7) ^ RotateRight(x, 18) ^ (x >> 3);

    private static uint SmallSigma1(uint x) =>
        RotateRight(x, 17) ^ RotateRight(x, 19) ^ (x >> 10);
}