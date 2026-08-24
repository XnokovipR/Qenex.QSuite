using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Qenex.QSuite.Helpers.ProjectFile;

public enum QprojFileKind
{
    Container,
    LegacyZip,
    Unknown
}

public enum QprojKeyMode : byte
{
    BuiltIn = 0,
    Password = 1
}

public enum QprojDecryptStatus
{
    Success,
    PasswordRequired,
    WrongPasswordOrCorrupt,
    Corrupt,
    NotAContainer
}

/// <summary>
/// Encrypted .qproj container: "QPROJ" magic, container version, key mode,
/// mode-specific header and an AES-256-GCM payload holding the project zip.
/// Files without the magic are legacy plain-zip projects (read-only support;
/// saving always produces a container).
/// </summary>
public static class QprojContainer
{
    private static readonly byte[] Magic = "QPROJ"u8.ToArray();
    private const byte ContainerVersion = 0x01;

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int KdfSaltSize = 16;
    private const int DefaultKdfIterations = 600_000;
    // Sanity bounds for the iteration count read from a file, so a crafted
    // header cannot stall the open path.
    private const int MinKdfIterations = 10_000;
    private const int MaxKdfIterations = 10_000_000;

    private static readonly byte[] ZipMagic = [0x50, 0x4B, 0x03, 0x04];

    #region Detection

    public static QprojFileKind DetectKind(ReadOnlySpan<byte> fileBytes)
    {
        if (fileBytes.Length >= Magic.Length && fileBytes[..Magic.Length].SequenceEqual(Magic))
        {
            return QprojFileKind.Container;
        }

        if (fileBytes.Length >= ZipMagic.Length && fileBytes[..ZipMagic.Length].SequenceEqual(ZipMagic))
        {
            return QprojFileKind.LegacyZip;
        }

        return QprojFileKind.Unknown;
    }

    public static QprojKeyMode? DetectKeyMode(ReadOnlySpan<byte> fileBytes)
    {
        if (DetectKind(fileBytes) != QprojFileKind.Container || fileBytes.Length < Magic.Length + 2)
        {
            return null;
        }

        var mode = fileBytes[Magic.Length + 1];
        return Enum.IsDefined(typeof(QprojKeyMode), mode) ? (QprojKeyMode)mode : null;
    }

    #endregion

    #region Encrypt

    /// <summary>Wraps the plain project zip into a container. A null password selects the built-in key.</summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> plainZip, string? password)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plainZip.Length];

        byte[] header;
        byte[] key;
        if (password == null)
        {
            header = BuildHeader(QprojKeyMode.BuiltIn, [], nonce);
            key = BuiltInKey.Assemble();
        }
        else
        {
            var salt = RandomNumberGenerator.GetBytes(KdfSaltSize);
            var kdfBlock = new byte[KdfSaltSize + sizeof(uint)];
            salt.CopyTo(kdfBlock, 0);
            BinaryPrimitives.WriteUInt32LittleEndian(kdfBlock.AsSpan(KdfSaltSize), DefaultKdfIterations);
            header = BuildHeader(QprojKeyMode.Password, kdfBlock, nonce);
            key = DeriveKey(password, salt, DefaultKdfIterations);
        }

        try
        {
            using var aes = new AesGcm(key, TagSize);
            // The header is authenticated as associated data, so magic/version/mode
            // tampering is caught by the tag check as well.
            aes.Encrypt(nonce, plainZip, ciphertext, tag, header);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        var result = new byte[header.Length + TagSize + ciphertext.Length];
        header.CopyTo(result, 0);
        tag.CopyTo(result, header.Length);
        ciphertext.CopyTo(result, header.Length + TagSize);
        return result;
    }

    private static byte[] BuildHeader(QprojKeyMode mode, byte[] kdfBlock, byte[] nonce)
    {
        var header = new byte[Magic.Length + 2 + kdfBlock.Length + NonceSize];
        var offset = 0;
        Magic.CopyTo(header, offset);
        offset += Magic.Length;
        header[offset++] = ContainerVersion;
        header[offset++] = (byte)mode;
        kdfBlock.CopyTo(header, offset);
        offset += kdfBlock.Length;
        nonce.CopyTo(header, offset);
        return header;
    }

    #endregion

    #region Decrypt

    /// <summary>
    /// Unwraps a container back to the plain project zip. Pass the user password for
    /// password-protected files; it is ignored for built-in-key files.
    /// </summary>
    public static QprojDecryptStatus TryDecrypt(byte[] fileBytes, string? password, out byte[]? plainZip)
    {
        plainZip = null;

        if (DetectKind(fileBytes) != QprojFileKind.Container)
        {
            return QprojDecryptStatus.NotAContainer;
        }

        var offset = Magic.Length;
        if (fileBytes.Length < offset + 2)
        {
            return QprojDecryptStatus.Corrupt;
        }

        var version = fileBytes[offset++];
        if (version != ContainerVersion)
        {
            return QprojDecryptStatus.Corrupt;
        }

        var modeByte = fileBytes[offset++];
        if (!Enum.IsDefined(typeof(QprojKeyMode), modeByte))
        {
            return QprojDecryptStatus.Corrupt;
        }

        var mode = (QprojKeyMode)modeByte;
        byte[] key;
        if (mode == QprojKeyMode.BuiltIn)
        {
            key = BuiltInKey.Assemble();
        }
        else
        {
            if (fileBytes.Length < offset + KdfSaltSize + sizeof(uint))
            {
                return QprojDecryptStatus.Corrupt;
            }

            if (password == null)
            {
                return QprojDecryptStatus.PasswordRequired;
            }

            var salt = fileBytes.AsSpan(offset, KdfSaltSize).ToArray();
            offset += KdfSaltSize;
            var iterations = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(offset));
            offset += sizeof(uint);
            if (iterations is < MinKdfIterations or > MaxKdfIterations)
            {
                return QprojDecryptStatus.Corrupt;
            }

            key = DeriveKey(password, salt, (int)iterations);
        }

        try
        {
            if (fileBytes.Length < offset + NonceSize + TagSize)
            {
                return QprojDecryptStatus.Corrupt;
            }

            var nonce = fileBytes.AsSpan(offset, NonceSize).ToArray();
            var header = fileBytes.AsSpan(0, offset + NonceSize).ToArray();
            offset += NonceSize;
            var tag = fileBytes.AsSpan(offset, TagSize).ToArray();
            offset += TagSize;
            var ciphertext = fileBytes.AsSpan(offset).ToArray();

            var plain = new byte[ciphertext.Length];
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plain, header);
            }
            catch (AuthenticationTagMismatchException)
            {
                return mode == QprojKeyMode.Password
                    ? QprojDecryptStatus.WrongPasswordOrCorrupt
                    : QprojDecryptStatus.Corrupt;
            }
            catch (CryptographicException)
            {
                return QprojDecryptStatus.Corrupt;
            }

            plainZip = plain;
            return QprojDecryptStatus.Success;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    #endregion

    private static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeySize);
    }

    /// <summary>
    /// The application key for projects without a password. Identical in every
    /// installation so a passwordless project opens in any QInsight. Assembled
    /// from scattered obfuscated fragments instead of one literal constant.
    /// </summary>
    private static class BuiltInKey
    {
        private static readonly byte[] FragmentA = [0xD3, 0x1F, 0x8A, 0x4C, 0x77, 0xE2, 0x09, 0xB5];
        private static readonly byte[] MaskA = [0x5A, 0xC3, 0x2E, 0x91, 0x0B, 0x64, 0xF7, 0x18];

        private static readonly byte[] FragmentB = [0x2C, 0x96, 0x41, 0xEB, 0x1D, 0x70, 0xA8, 0x53];
        private static readonly byte[] MaskB = [0xE1, 0x0F, 0xB4, 0x66, 0xC9, 0x32, 0x5D, 0x8E];

        private static readonly byte[] FragmentC = [0x88, 0x35, 0xF0, 0x6A, 0xBE, 0x47, 0x12, 0xDC];
        private static readonly byte[] MaskC = [0x73, 0xA6, 0x09, 0xD5, 0x40, 0xFB, 0x27, 0x9C];

        private static readonly byte[] FragmentD = [0x64, 0xC1, 0x3B, 0x97, 0x02, 0xAF, 0x58, 0xE9];
        private static readonly byte[] MaskD = [0x1E, 0x82, 0xF5, 0x49, 0xB0, 0x6D, 0xCA, 0x37];

        internal static byte[] Assemble()
        {
            var key = new byte[KeySize];
            Combine(FragmentA, MaskA, key, 0);
            Combine(FragmentB, MaskB, key, 8);
            Combine(FragmentC, MaskC, key, 16);
            Combine(FragmentD, MaskD, key, 24);
            return key;
        }

        private static void Combine(byte[] fragment, byte[] mask, byte[] destination, int offset)
        {
            for (var i = 0; i < fragment.Length; i++)
            {
                destination[offset + i] = (byte)(fragment[i] ^ mask[i]);
            }
        }
    }
}
