namespace Qenex.QSuite.Tests.ModbusProtocolTest;

/// <summary>Console-style unit tests for the Modbus stack (repo convention, same as the other
/// _Tests projects). Exit code 0 = all passed.</summary>
internal static class Program
{
    private static int failures;

    private static int Main()
    {
        CoreTests.Run();
        MasterEngineTests.Run();
        SlaveEngineTests.Run();
        ProtocolTests.Run();
        LoopbackTests.Run();
        DriverLoopbackTests.Run();

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

    internal static async Task CheckThrowsAsync<TException>(Func<Task> action, string description) where TException : Exception
    {
        try
        {
            await action();
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
