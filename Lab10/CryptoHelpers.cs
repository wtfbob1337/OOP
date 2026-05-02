using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Lab10;

internal static class CryptoHelpers
{
    private static readonly byte[] PiTable =
    {
        217, 120, 249, 196, 25, 221, 181, 237, 40, 233, 253, 121, 74, 160, 216, 157,
        198, 126, 55, 131, 43, 118, 83, 142, 98, 76, 100, 136, 68, 139, 251, 162,
        23, 154, 89, 245, 135, 179, 79, 19, 97, 69, 109, 141, 9, 129, 125, 50,
        189, 143, 64, 235, 134, 183, 123, 11, 240, 149, 33, 34, 92, 107, 78, 130,
        84, 214, 101, 147, 206, 96, 178, 28, 115, 86, 192, 20, 167, 140, 241, 220,
        18, 117, 202, 31, 59, 190, 228, 209, 66, 61, 212, 48, 163, 60, 182, 38,
        111, 191, 14, 218, 70, 105, 7, 87, 39, 242, 29, 155, 188, 148, 67, 3,
        248, 17, 199, 246, 144, 239, 62, 231, 6, 195, 213, 47, 200, 102, 30, 215,
        8, 232, 234, 222, 128, 82, 238, 247, 132, 170, 114, 172, 53, 77, 106, 42,
        150, 26, 210, 113, 90, 21, 73, 116, 75, 159, 208, 94, 4, 24, 164, 236,
        194, 224, 65, 110, 15, 81, 203, 204, 36, 145, 175, 80, 161, 244, 112, 57,
        153, 124, 58, 133, 35, 184, 180, 122, 252, 2, 54, 91, 37, 85, 151, 49,
        45, 93, 250, 152, 227, 138, 146, 174, 5, 223, 41, 16, 103, 108, 186, 201,
        211, 0, 230, 207, 225, 158, 168, 44, 99, 22, 1, 63, 88, 226, 137, 169,
        13, 56, 52, 27, 171, 51, 255, 176, 187, 72, 12, 95, 185, 177, 205, 46,
        197, 243, 219, 71, 229, 165, 156, 119, 10, 166, 32, 104, 254, 127, 193, 173
    };

    public static Rc2Result RunRc2(string text, string key, CancellationToken token, IProgress<int> progress)
    {
        var started = DateTime.Now;
        var keyBytes = PrepareKey(key);
        var schedule = ExpandKey(keyBytes, Math.Clamp(keyBytes.Length * 8, 8, 1024));
        var source = Encoding.UTF8.GetBytes(text);
        var padded = Pad(source);
        var encrypted = new byte[padded.Length];
        var decrypted = new byte[padded.Length];

        for (var i = 0; i < padded.Length; i += 8)
        {
            token.ThrowIfCancellationRequested();
            var block = padded.Skip(i).Take(8).ToArray();
            EncryptRc2Block(block, schedule);
            Array.Copy(block, 0, encrypted, i, 8);
            progress.Report((i + 8) * 45 / padded.Length);
            Thread.Sleep(18);
        }

        for (var i = 0; i < encrypted.Length; i += 8)
        {
            token.ThrowIfCancellationRequested();
            var block = encrypted.Skip(i).Take(8).ToArray();
            DecryptRc2Block(block, schedule);
            Array.Copy(block, 0, decrypted, i, 8);
            progress.Report(45 + (i + 8) * 45 / encrypted.Length);
            Thread.Sleep(18);
        }

        progress.Report(100);
        return new Rc2Result(
            Convert.ToHexString(keyBytes),
            Convert.ToHexString(encrypted),
            Encoding.UTF8.GetString(Unpad(decrypted)),
            padded.Length / 8,
            Environment.CurrentManagedThreadId,
            DateTime.Now - started);
    }

    public static Mdc2Result RunMdc2(string text, CancellationToken token, IProgress<int> progress)
    {
        var started = DateTime.Now;
        var data = ZeroPad(Encoding.UTF8.GetBytes(text));
        var a = Enumerable.Repeat((byte)0x52, 8).ToArray();
        var b = Enumerable.Repeat((byte)0x25, 8).ToArray();
        var blocks = data.Length == 0 ? 0 : data.Length / 8;

        for (var offset = 0; offset < data.Length; offset += 8)
        {
            token.ThrowIfCancellationRequested();
            a[0] = (byte)((a[0] & 0x9f) ^ 0x40);
            b[0] = (byte)((b[0] & 0x9f) ^ 0x20);
            var message = data.Skip(offset).Take(8).ToArray();
            var e1 = EncryptRc2BlockCopy(message, ExpandKey(a, 64));
            var e2 = EncryptRc2BlockCopy(message, ExpandKey(b, 64));
            var v = Xor(message, e1);
            var w = Xor(message, e2);
            a = v.Take(4).Concat(w.Skip(4).Take(4)).ToArray();
            b = w.Take(4).Concat(v.Skip(4).Take(4)).ToArray();
            progress.Report((offset + 8) * 100 / data.Length);
            Thread.Sleep(24);
        }

        progress.Report(100);
        return new Mdc2Result(
            Convert.ToHexString(a.Concat(b).ToArray()).ToLowerInvariant(),
            blocks,
            Environment.CurrentManagedThreadId,
            DateTime.Now - started);
    }

    public static EsignResult RunEsign(string text, CancellationToken token, IProgress<int> progress)
    {
        var started = DateTime.Now;
        var p = new BigInteger(65537);
        var q = new BigInteger(65539);
        var n = p * p * q;
        var phi = p * (p - 1) * (q - 1);
        var e = new BigInteger(17);
        var d = ModInverse(e, phi);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var message = PositiveBigInteger(hash) % n;

        for (var i = 1; i <= 10; i++)
        {
            token.ThrowIfCancellationRequested();
            progress.Report(i * 6);
            Thread.Sleep(22);
        }

        var signature = BigInteger.ModPow(message, d, n);

        for (var i = 1; i <= 8; i++)
        {
            token.ThrowIfCancellationRequested();
            progress.Report(60 + i * 4);
            Thread.Sleep(22);
        }

        var verified = BigInteger.ModPow(signature, e, n) == message;
        progress.Report(100);
        return new EsignResult(
            p.ToString(),
            q.ToString(),
            n.ToString(),
            e.ToString(),
            Convert.ToHexString(hash).ToLowerInvariant(),
            message.ToString(),
            signature.ToString(),
            verified,
            Environment.CurrentManagedThreadId,
            DateTime.Now - started);
    }

    private static byte[] PrepareKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(key) ? "rc2-key" : key);
        return bytes.Length <= 128 ? bytes : SHA512.HashData(bytes).Concat(SHA512.HashData(bytes.Reverse().ToArray())).Take(128).ToArray();
    }

    private static ushort[] ExpandKey(byte[] key, int effectiveBits)
    {
        var t = key.Length;
        var l = new byte[128];
        Array.Copy(key, l, t);

        for (var i = t; i < 128; i++)
        {
            l[i] = PiTable[(l[i - 1] + l[i - t]) & 255];
        }

        var t8 = (effectiveBits + 7) / 8;
        var tm = 255 >> (8 * t8 - effectiveBits);
        l[128 - t8] = PiTable[l[128 - t8] & tm];

        for (var i = 127 - t8; i >= 0; i--)
        {
            l[i] = PiTable[l[i + 1] ^ l[i + t8]];
        }

        var words = new ushort[64];

        for (var i = 0; i < words.Length; i++)
        {
            words[i] = (ushort)(l[2 * i] | (l[2 * i + 1] << 8));
        }

        return words;
    }

    private static byte[] EncryptRc2BlockCopy(byte[] block, ushort[] schedule)
    {
        var copy = block.ToArray();
        EncryptRc2Block(copy, schedule);
        return copy;
    }

    private static void EncryptRc2Block(byte[] block, ushort[] key)
    {
        var r0 = ReadWord(block, 0);
        var r1 = ReadWord(block, 2);
        var r2 = ReadWord(block, 4);
        var r3 = ReadWord(block, 6);
        var j = 0;

        Mix(ref r0, ref r1, ref r2, ref r3, key, ref j, 5);
        Mash(ref r0, ref r1, ref r2, ref r3, key);
        Mix(ref r0, ref r1, ref r2, ref r3, key, ref j, 6);
        Mash(ref r0, ref r1, ref r2, ref r3, key);
        Mix(ref r0, ref r1, ref r2, ref r3, key, ref j, 5);
        WriteWord(block, 0, r0);
        WriteWord(block, 2, r1);
        WriteWord(block, 4, r2);
        WriteWord(block, 6, r3);
    }

    private static void DecryptRc2Block(byte[] block, ushort[] key)
    {
        var r0 = ReadWord(block, 0);
        var r1 = ReadWord(block, 2);
        var r2 = ReadWord(block, 4);
        var r3 = ReadWord(block, 6);
        var j = 63;

        RMix(ref r0, ref r1, ref r2, ref r3, key, ref j, 5);
        RMash(ref r0, ref r1, ref r2, ref r3, key);
        RMix(ref r0, ref r1, ref r2, ref r3, key, ref j, 6);
        RMash(ref r0, ref r1, ref r2, ref r3, key);
        RMix(ref r0, ref r1, ref r2, ref r3, key, ref j, 5);
        WriteWord(block, 0, r0);
        WriteWord(block, 2, r1);
        WriteWord(block, 4, r2);
        WriteWord(block, 6, r3);
    }

    private static void Mix(ref ushort r0, ref ushort r1, ref ushort r2, ref ushort r3, ushort[] key, ref int j, int rounds)
    {
        for (var i = 0; i < rounds; i++)
        {
            r0 = Rol((ushort)(r0 + key[j++] + (r3 & r2) + (~r3 & r1)), 1);
            r1 = Rol((ushort)(r1 + key[j++] + (r0 & r3) + (~r0 & r2)), 2);
            r2 = Rol((ushort)(r2 + key[j++] + (r1 & r0) + (~r1 & r3)), 3);
            r3 = Rol((ushort)(r3 + key[j++] + (r2 & r1) + (~r2 & r0)), 5);
        }
    }

    private static void RMix(ref ushort r0, ref ushort r1, ref ushort r2, ref ushort r3, ushort[] key, ref int j, int rounds)
    {
        for (var i = 0; i < rounds; i++)
        {
            r3 = (ushort)(Ror(r3, 5) - key[j--] - (r2 & r1) - (~r2 & r0));
            r2 = (ushort)(Ror(r2, 3) - key[j--] - (r1 & r0) - (~r1 & r3));
            r1 = (ushort)(Ror(r1, 2) - key[j--] - (r0 & r3) - (~r0 & r2));
            r0 = (ushort)(Ror(r0, 1) - key[j--] - (r3 & r2) - (~r3 & r1));
        }
    }

    private static void Mash(ref ushort r0, ref ushort r1, ref ushort r2, ref ushort r3, ushort[] key)
    {
        r0 = (ushort)(r0 + key[r3 & 63]);
        r1 = (ushort)(r1 + key[r0 & 63]);
        r2 = (ushort)(r2 + key[r1 & 63]);
        r3 = (ushort)(r3 + key[r2 & 63]);
    }

    private static void RMash(ref ushort r0, ref ushort r1, ref ushort r2, ref ushort r3, ushort[] key)
    {
        r3 = (ushort)(r3 - key[r2 & 63]);
        r2 = (ushort)(r2 - key[r1 & 63]);
        r1 = (ushort)(r1 - key[r0 & 63]);
        r0 = (ushort)(r0 - key[r3 & 63]);
    }

    private static ushort ReadWord(byte[] data, int index)
    {
        return (ushort)(data[index] | (data[index + 1] << 8));
    }

    private static void WriteWord(byte[] data, int index, ushort value)
    {
        data[index] = (byte)(value & 255);
        data[index + 1] = (byte)(value >> 8);
    }

    private static ushort Rol(ushort value, int count)
    {
        return (ushort)((value << count) | (value >> (16 - count)));
    }

    private static ushort Ror(ushort value, int count)
    {
        return (ushort)((value >> count) | (value << (16 - count)));
    }

    private static byte[] Pad(byte[] data)
    {
        var add = 8 - data.Length % 8;
        var result = new byte[data.Length + add];
        Array.Copy(data, result, data.Length);
        Array.Fill(result, (byte)add, data.Length, add);
        return result;
    }

    private static byte[] ZeroPad(byte[] data)
    {
        if (data.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var add = (8 - data.Length % 8) % 8;
        var result = new byte[data.Length + add];
        Array.Copy(data, result, data.Length);
        return result;
    }

    private static byte[] Unpad(byte[] data)
    {
        if (data.Length == 0)
        {
            return data;
        }

        var add = data[^1];

        if (add <= 0 || add > 8 || add > data.Length)
        {
            return data;
        }

        return data.Take(data.Length - add).ToArray();
    }

    private static byte[] Xor(byte[] left, byte[] right)
    {
        var result = new byte[left.Length];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = (byte)(left[i] ^ right[i]);
        }

        return result;
    }

    private static BigInteger PositiveBigInteger(byte[] bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    private static BigInteger ModInverse(BigInteger value, BigInteger modulus)
    {
        var a = value;
        var m = modulus;
        BigInteger x0 = 1;
        BigInteger x1 = 0;

        while (m != 0)
        {
            var q = a / m;
            (a, m) = (m, a % m);
            (x0, x1) = (x1, x0 - q * x1);
        }

        return (x0 % modulus + modulus) % modulus;
    }
}

internal sealed record Rc2Result(string KeyHex, string CipherHex, string PlainText, int Blocks, int ThreadId, TimeSpan Duration);
internal sealed record Mdc2Result(string HashHex, int Blocks, int ThreadId, TimeSpan Duration);
internal sealed record EsignResult(string P, string Q, string N, string E, string HashHex, string MessageNumber, string Signature, bool Verified, int ThreadId, TimeSpan Duration);
