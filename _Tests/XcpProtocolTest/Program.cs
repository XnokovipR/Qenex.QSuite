namespace Qenex.QSuite.Tests.XcpProtocolTest;

/// <summary>Console-style unit tests for the XCP protocol (repo convention, same as the other
/// _Tests projects). Exit code 0 = all passed.</summary>
internal static class Program
{
    private static int failures;

    private static int Main()
    {
        SpecificationTests.Run();
        CodecTests.Run();
        MasterTests.Run();
        IntegrationTests.Run();

        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    internal static void Check(bool condition, string description)
    {
        if (condition)
        {
            return;
        }

        failures++;
        Console.WriteLine($"FAILED: {description}");
    }

    internal static void CheckThrows<TException>(Action action, string description) where TException : Exception
    {
        try
        {
            action();
            Check(false, $"{description} (no exception thrown)");
        }
        catch (TException)
        {
            Check(true, description);
        }
        catch (Exception e)
        {
            Check(false, $"{description} (threw {e.GetType().Name} instead of {typeof(TException).Name})");
        }
    }
}
