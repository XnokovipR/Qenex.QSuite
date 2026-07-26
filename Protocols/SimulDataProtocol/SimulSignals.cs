using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;

namespace Qenex.QSuite.Protocols.SimulDataProtocol;

/// <summary>
/// One simulated signal source; every generated variable owns its own instance (own RNG, own state).
/// </summary>
internal interface ISimulSignal
{
    /// <param name="tSeconds">Continuous time in seconds since the protocol started.</param>
    double Next(double tSeconds);
}

/// <summary>Pure staircase sine: the value changes once per whole second (60 s period, 4 waves).</summary>
internal sealed class StepSineSignal : ISimulSignal
{
    public double Next(double tSeconds)
    {
        return 750.0 * Math.Sin(Math.Floor(tSeconds) % 60.0 / 60.0 * 8 * Math.PI);
    }
}

/// <summary>Staircase sine (8 waves per 60 s) with uniform noise of +-50 on every sample.</summary>
internal sealed class NoisyStepSineSignal(Random random) : ISimulSignal
{
    public double Next(double tSeconds)
    {
        return 1000.0 * Math.Sin(Math.Floor(tSeconds) % 60.0 / 60.0 * 16 * Math.PI)
               + 100.0 * random.NextDouble() - 50.0;
    }
}

/// <summary>
/// Random walk with weak mean reversion: every sample moves by uniform(+-step) and is pulled
/// back towards the offset so it does not drift off the chart (see Scripts/can_rnd_sigs_rnd.py).
/// </summary>
internal sealed class MeanRevertingWalkSignal : ISimulSignal
{
    private readonly Random random;
    private readonly double offset;
    private readonly double step;
    private readonly double reversion;
    private double current;

    public MeanRevertingWalkSignal(Random random, double offset, double step, double reversion)
    {
        this.random = random;
        this.offset = offset;
        this.step = step;
        this.reversion = reversion;
        current = offset;
    }

    public double Next(double tSeconds)
    {
        current += (random.NextDouble() * 2.0 - 1.0) * step + reversion * (offset - current);
        return current;
    }
}

/// <summary>Fixed catalog of the five simulated signals selectable via the "signal" commParam.</summary>
internal static class SimulSignalCatalog
{
    public const string StepKey = "step";
    public const string NoisyStepKey = "noisystep";
    public const string Walk1Key = "walk1";
    public const string Walk2Key = "walk2";
    public const string Walk3Key = "walk3";

    public static bool IsKnown(string key) => key is StepKey or NoisyStepKey or Walk1Key or Walk2Key or Walk3Key;

    public static ISimulSignal Create(string key)
    {
        return key switch
        {
            StepKey => new StepSineSignal(),
            NoisyStepKey => new NoisyStepSineSignal(new Random()),
            Walk1Key => new MeanRevertingWalkSignal(new Random(), offset: 0.0, step: 0.5, reversion: 0.01),
            Walk2Key => new MeanRevertingWalkSignal(new Random(), offset: 10.0, step: 1.0, reversion: 0.02),
            Walk3Key => new MeanRevertingWalkSignal(new Random(), offset: -5.0, step: 0.3, reversion: 0.015),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown simulation signal.")
        };
    }

    /// <summary>Legacy projects have no "signal" commParam; keep their historical shape by value type.</summary>
    public static string FallbackKey(ScalarVariable scalarVariable)
    {
        return scalarVariable.Values switch
        {
            Values<int> => NoisyStepKey,
            Values<float> => StepKey,
            _ => Walk1Key
        };
    }
}
