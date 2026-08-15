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

/// <summary>
/// Optional generator configuration: amp=/freq=/nonlin= parsed from the commParam (null =
/// signal default) plus a live nonlinearity source — the current value of the writable
/// "nonlin" parameter variable, read on every sample.
/// </summary>
internal sealed record SimulSignalSettings(
    double? Amplitude, double? Frequency, double? Nonlinearity, Func<double>? NonlinearityProvider)
{
    public static readonly SimulSignalSettings Empty = new(null, null, null, null);
}

/// <summary>
/// LAOS excitation: pure sine strain, amplitude and frequency set via amp=/freq=.
/// Pair with "laosstress" (same amp/freq) — time runs on the shared protocol clock,
/// so the pair stays phase-coherent for stress-strain Lissajous loops on an XY graph.
/// </summary>
internal sealed class LaosStrainSignal(SimulSignalSettings settings) : ISimulSignal
{
    private readonly double amplitude = settings.Amplitude ?? 800.0;
    private readonly double frequency = settings.Frequency ?? 0.5;

    public double Next(double tSeconds)
    {
        return amplitude * Math.Sin(2.0 * Math.PI * frequency * tSeconds);
    }
}

/// <summary>
/// LAOS response: fundamental plus odd harmonics 3-9 with slightly shifted phases. The
/// harmonic amplitudes are driven by the nonlinearity parameter: harmonic k scales with
/// nonlin^k, so nonlin 0 gives a clean phase-shifted sine and rising nonlin grows the
/// harmonic content. The parameter is read live from the writable "nonlin" variable
/// (fallback: nonlin= in the commParam, then 0).
/// </summary>
internal sealed class LaosStressSignal(SimulSignalSettings settings) : ISimulSignal
{
    private static readonly double[] Coefficients = [0.85, 0.30, 0.18, 0.10, 0.06];
    private const double FundamentalPhase = 0.4;

    private readonly double amplitude = settings.Amplitude ?? 800.0;
    private readonly double frequency = settings.Frequency ?? 0.5;

    public double Next(double tSeconds)
    {
        var nonlinearity = Math.Clamp(
            settings.NonlinearityProvider?.Invoke() ?? settings.Nonlinearity ?? 0.0, 0.0, 2.0);
        var omegaT = 2.0 * Math.PI * frequency * tSeconds;
        var sum = 0.0;
        for (var k = 0; k < Coefficients.Length; k++)
        {
            var n = 2 * k + 1;
            var harmonicAmplitude = amplitude * Coefficients[k] * Math.Pow(nonlinearity, k);
            sum += harmonicAmplitude * Math.Sin(n * (omegaT + FundamentalPhase) + 0.12 * k);
        }

        return sum;
    }
}

/// <summary>Constant value; used only when a parameter variable is misconfigured as read.</summary>
internal sealed class ConstantSignal(double value) : ISimulSignal
{
    public double Next(double tSeconds)
    {
        return value;
    }
}

/// <summary>Fixed catalog of the simulated signals selectable via the "signal" commParam.</summary>
internal static class SimulSignalCatalog
{
    public const string StepKey = "step";
    public const string NoisyStepKey = "noisystep";
    public const string Walk1Key = "walk1";
    public const string Walk2Key = "walk2";
    public const string Walk3Key = "walk3";
    public const string LaosStrainKey = "laosstrain";
    public const string LaosStressKey = "laosstress";

    /// <summary>
    /// Writable parameter variable (direction="write"), not a generator: its live value is
    /// the nonlinearity for "laosstress". If it is misconfigured as read, it generates a
    /// constant so the protocol does not fault.
    /// </summary>
    public const string NonlinParamKey = "nonlin";

    public static bool IsKnown(string key) => key is StepKey or NoisyStepKey or Walk1Key or Walk2Key or Walk3Key
        or LaosStrainKey or LaosStressKey or NonlinParamKey;

    public static ISimulSignal Create(string key, SimulSignalSettings settings)
    {
        return key switch
        {
            StepKey => new StepSineSignal(),
            NoisyStepKey => new NoisyStepSineSignal(new Random()),
            Walk1Key => new MeanRevertingWalkSignal(new Random(), offset: 0.0, step: 0.5, reversion: 0.01),
            Walk2Key => new MeanRevertingWalkSignal(new Random(), offset: 10.0, step: 1.0, reversion: 0.02),
            Walk3Key => new MeanRevertingWalkSignal(new Random(), offset: -5.0, step: 0.3, reversion: 0.015),
            LaosStrainKey => new LaosStrainSignal(settings),
            LaosStressKey => new LaosStressSignal(settings),
            NonlinParamKey => new ConstantSignal(settings.Nonlinearity ?? 0.0),
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
