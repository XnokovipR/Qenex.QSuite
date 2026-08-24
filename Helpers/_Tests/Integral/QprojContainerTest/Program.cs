using System.Text;
using Qenex.QSuite.Helpers.ProjectFile;

namespace Qenex.QSuite.Helpers.Tests.QprojContainerTest;

/// <summary>
/// Headless tests of the encrypted .qproj container: round-trips in both key modes,
/// password flow statuses, tamper detection, legacy plain-zip reading and the
/// silent conversion to an encrypted container on save.
/// </summary>
internal class Program
{
    private static int failures;

    private static async Task<int> Main()
    {
        T1_BuiltInKeyRoundTrip();
        T2_PasswordRoundTrip();
        T3_PasswordFlowStatuses();
        T4_TamperDetection();
        T5_DetectKinds();
        await T6_FileRoundTripBuiltInAsync();
        await T7_FileRoundTripPasswordAsync();
        await T8_LegacyZipReadAndConvertAsync();
        T9_UnknownAndDamagedFiles();

        Console.WriteLine(failures == 0 ? "ALL PASSED" : $"{failures} FAILURE(S)");
        return failures == 0 ? 0 : 1;
    }

    private static void T1_BuiltInKeyRoundTrip()
    {
        var plain = Encoding.UTF8.GetBytes("plain project zip payload");
        var container = QprojContainer.Encrypt(plain, null);
        var status = QprojContainer.TryDecrypt(container, null, out var decrypted);

        Check("T1 built-in key round-trip",
            status == QprojDecryptStatus.Success && decrypted!.SequenceEqual(plain));

        var container2 = QprojContainer.Encrypt(plain, null);
        Check("T1b random nonce - two encryptions differ", !container.SequenceEqual(container2));
    }

    private static void T2_PasswordRoundTrip()
    {
        var plain = Encoding.UTF8.GetBytes("password protected payload");
        var container = QprojContainer.Encrypt(plain, "letmein-Ω");
        var status = QprojContainer.TryDecrypt(container, "letmein-Ω", out var decrypted);

        Check("T2 password round-trip (incl. non-ASCII password)",
            status == QprojDecryptStatus.Success && decrypted!.SequenceEqual(plain));
    }

    private static void T3_PasswordFlowStatuses()
    {
        var container = QprojContainer.Encrypt("secret"u8.ToArray(), "correct");

        Check("T3a no password -> PasswordRequired",
            QprojContainer.TryDecrypt(container, null, out _) == QprojDecryptStatus.PasswordRequired);
        Check("T3b wrong password -> WrongPasswordOrCorrupt",
            QprojContainer.TryDecrypt(container, "wrong", out _) == QprojDecryptStatus.WrongPasswordOrCorrupt);
        Check("T3c built-in container ignores a supplied password",
            QprojContainer.TryDecrypt(QprojContainer.Encrypt("x"u8.ToArray(), null), "ignored", out _)
                == QprojDecryptStatus.Success);
    }

    private static void T4_TamperDetection()
    {
        var builtIn = QprojContainer.Encrypt("payload-builtin"u8.ToArray(), null);
        builtIn[^1] ^= 0xFF;
        Check("T4a tampered ciphertext (built-in) -> Corrupt",
            QprojContainer.TryDecrypt(builtIn, null, out _) == QprojDecryptStatus.Corrupt);

        var withPassword = QprojContainer.Encrypt("payload-password"u8.ToArray(), "pw");
        withPassword[^1] ^= 0xFF;
        Check("T4b tampered ciphertext (password) -> WrongPasswordOrCorrupt",
            QprojContainer.TryDecrypt(withPassword, "pw", out _) == QprojDecryptStatus.WrongPasswordOrCorrupt);

        var modeFlipped = QprojContainer.Encrypt("payload"u8.ToArray(), null);
        modeFlipped[6] = 1; // built-in -> password; header is authenticated, must not decrypt
        Check("T4c tampered header mode byte does not decrypt",
            QprojContainer.TryDecrypt(modeFlipped, "anything", out _) != QprojDecryptStatus.Success);

        var truncated = QprojContainer.Encrypt("payload"u8.ToArray(), null)[..10];
        Check("T4d truncated file -> Corrupt",
            QprojContainer.TryDecrypt(truncated, null, out _) == QprojDecryptStatus.Corrupt);
    }

    private static void T5_DetectKinds()
    {
        Check("T5a container magic detected",
            QprojContainer.DetectKind(QprojContainer.Encrypt([1, 2, 3], null)) == QprojFileKind.Container);
        Check("T5b zip magic detected as legacy",
            QprojContainer.DetectKind([0x50, 0x4B, 0x03, 0x04, 0x00]) == QprojFileKind.LegacyZip);
        Check("T5c other content unknown",
            QprojContainer.DetectKind("<?xml version=\"1.0\"?>"u8) == QprojFileKind.Unknown);
        Check("T5d key mode reported",
            QprojContainer.DetectKeyMode(QprojContainer.Encrypt([1], "pw")) == QprojKeyMode.Password);
    }

    private static async Task T6_FileRoundTripBuiltInAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qproj_test_{Guid.NewGuid():N}.qproj");
        try
        {
            var streams = MakeStreams(("XmlModule.xml", "<module/>"), ("init.py", "val=0.0"));
            await new ProjectZip().ZipProjectAsync(path, streams);

            Check("T6a saved file is a container, not a zip",
                QprojContainer.DetectKind(await File.ReadAllBytesAsync(path)) == QprojFileKind.Container);

            var result = new ProjectZip().UnzipProject(path);
            Check("T6b file round-trip (built-in key)",
                result.Status == ProjectUnzipStatus.Success
                && ReadEntry(result, "XmlModule.xml") == "<module/>"
                && ReadEntry(result, "init.py") == "val=0.0");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task T7_FileRoundTripPasswordAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qproj_test_{Guid.NewGuid():N}.qproj");
        try
        {
            await new ProjectZip().ZipProjectAsync(path, MakeStreams(("XmlModule.xml", "<module/>")), "tajne heslo");

            var prjZip = new ProjectZip();
            Check("T7a no password -> PasswordRequired",
                prjZip.UnzipProject(path).Status == ProjectUnzipStatus.PasswordRequired);
            Check("T7b wrong password -> WrongPasswordOrCorrupt",
                prjZip.UnzipProject(path, "spatne").Status == ProjectUnzipStatus.WrongPasswordOrCorrupt);

            var result = prjZip.UnzipProject(path, "tajne heslo");
            Check("T7c correct password opens",
                result.Status == ProjectUnzipStatus.Success && ReadEntry(result, "XmlModule.xml") == "<module/>");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task T8_LegacyZipReadAndConvertAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qproj_test_{Guid.NewGuid():N}.qproj");
        try
        {
            // A project saved before encryption existed = plain zip on disk.
            await new ProjectZip().ZipAsync(path, MakeStreams(("XmlModule.xml", "<module label=\"old\"/>")));
            Check("T8a legacy file is a plain zip",
                QprojContainer.DetectKind(await File.ReadAllBytesAsync(path)) == QprojFileKind.LegacyZip);

            var opened = new ProjectZip().UnzipProject(path);
            Check("T8b legacy zip opens without a password",
                opened.Status == ProjectUnzipStatus.Success && ReadEntry(opened, "XmlModule.xml") == "<module label=\"old\"/>");

            // First save converts silently to an encrypted container.
            await new ProjectZip().ZipProjectAsync(path, opened.Streams);
            var converted = await File.ReadAllBytesAsync(path);
            Check("T8c saving converts to a container",
                QprojContainer.DetectKind(converted) == QprojFileKind.Container);
            Check("T8d converted file still opens",
                new ProjectZip().UnzipProject(path).Status == ProjectUnzipStatus.Success);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void T9_UnknownAndDamagedFiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qproj_test_{Guid.NewGuid():N}.qproj");
        try
        {
            File.WriteAllText(path, "this is not a project file at all");
            Check("T9a unknown content -> Error",
                new ProjectZip().UnzipProject(path).Status == ProjectUnzipStatus.Error);

            var damaged = QprojContainer.Encrypt("payload"u8.ToArray(), null);
            damaged[^2] ^= 0x55;
            File.WriteAllBytes(path, damaged);
            Check("T9b damaged container -> Error",
                new ProjectZip().UnzipProject(path).Status == ProjectUnzipStatus.Error);

            Check("T9c missing file -> Error",
                new ProjectZip().UnzipProject(path + ".missing").Status == ProjectUnzipStatus.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Dictionary<string, Stream> MakeStreams(params (string Name, string Content)[] entries)
    {
        return entries.ToDictionary(
            e => e.Name,
            e => (Stream)new MemoryStream(Encoding.UTF8.GetBytes(e.Content)));
    }

    private static string ReadEntry(ProjectUnzipResult result, string name)
    {
        var stream = result.Streams[name];
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
    }

    private static void Check(string name, bool passed)
    {
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")}  {name}");
        if (!passed)
        {
            failures++;
        }
    }
}
