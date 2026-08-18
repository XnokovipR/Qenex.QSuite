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
/// signal default) plus a live parameter source — the current value of a writable parameter
/// variable (see <see cref="SimulSignalCatalog"/> parameter keys) looked up by key on every
/// sample; null when the project has no such parameter variable.
/// </summary>
internal sealed record SimulSignalSettings(
    double? Amplitude, double? Frequency, double? Nonlinearity, Func<string, double?>? ParameterProvider)
{
    public static readonly SimulSignalSettings Empty = new(null, null, null, null);

    public double? Parameter(string key) => ParameterProvider?.Invoke(key);
}

/// <summary>
/// LAOS excitation: pure sine strain, amplitude and frequency set via amp=/freq=. The
/// fundamental frequency follows the live "h1freq" parameter when the project has one, so
/// the strain stays coherent with the stress fundamental. Pair with "laosstress" — time runs
/// on the shared protocol clock, so the pair stays phase-coherent for stress-strain
/// Lissajous loops on an XY graph.
/// </summary>
internal sealed class LaosStrainSignal(SimulSignalSettings settings) : ISimulSignal
{
    private readonly double amplitude = settings.Amplitude ?? 800.0;
    private readonly double frequency = settings.Frequency ?? 0.5;

    public double Next(double tSeconds)
    {
        var fundamental = settings.Parameter(SimulSignalCatalog.HarmonicFrequencyKey(1)) ?? frequency;
        return amplitude * Math.Sin(2.0 * Math.PI * fundamental * tSeconds);
    }
}

/// <summary>
/// LAOS response: fundamental plus odd harmonics 3-9 with slightly shifted phases. By default
/// the harmonic amplitudes are driven by the nonlinearity parameter: harmonic k scales with
/// nonlin^k, so nonlin 0 gives a clean phase-shifted sine and rising nonlin grows the
/// harmonic content. The parameter is read live from the writable "nonlin" variable
/// (fallback: nonlin= in the commParam, then 0). The first four harmonics (1, 3, 5, 7) can be
/// tuned individually while the simulation runs: a writable "h1amp".."h4amp" parameter
/// replaces the amplitude of that harmonic (absolute, in stress units), "h1freq".."h4freq"
/// replaces its frequency in Hz and "h1phase".."h4phase" its phase in degrees — so the demo
/// can detune or re-phase single harmonics on purpose.
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
            settings.Parameter(SimulSignalCatalog.NonlinParamKey) ?? settings.Nonlinearity ?? 0.0, 0.0, 2.0);
        var sum = 0.0;
        for (var k = 0; k < Coefficients.Length; k++)
        {
            var n = 2 * k + 1;
            var harmonicAmplitude = amplitude * Coefficients[k] * Math.Pow(nonlinearity, k);
            var harmonicFrequency = n * frequency;
            var harmonicPhase = n * FundamentalPhase + 0.12 * k;   // radians
            if (k < SimulSignalCatalog.TunableHarmonics)
            {
                harmonicAmplitude = settings.Parameter(SimulSignalCatalog.HarmonicAmplitudeKey(k + 1)) ?? harmonicAmplitude;
                harmonicFrequency = settings.Parameter(SimulSignalCatalog.HarmonicFrequencyKey(k + 1)) ?? harmonicFrequency;
                var phaseDegrees = settings.Parameter(SimulSignalCatalog.HarmonicPhaseKey(k + 1));
                if (phaseDegrees.HasValue)
                {
                    harmonicPhase = phaseDegrees.Value * Math.PI / 180.0;
                }
            }

            // Default frequencies (n * fundamental) and phases reproduce the original n * (omega t + phase) form.
            sum += harmonicAmplitude * Math.Sin(2.0 * Math.PI * harmonicFrequency * tSeconds + harmonicPhase);
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

/// <summary>
/// One simulated matrix source: fills the raw buffer of a MatrixVariable (axes + data) in place.
/// </summary>
internal interface ISimulMatrixSignal
{
    /// <param name="tSeconds">Continuous time in seconds since the protocol started.</param>
    void Fill(MatrixVariable matrix, double tSeconds);
}

/// <summary>
/// Thermal field on a plate: a hot spot of amp= degrees above the 25 degC base circles the plate
/// once per 1/freq= seconds (default 40 s) over a small Gaussian footprint, plus +-0.3 degC noise
/// per cell. Axes are position breakpoints in mm (10 mm pitch), written once at the first
/// sample so user edits of the axes survive; only the data cells are regenerated afterwards.
/// Values are engineering values — the matrix presentation (conversion) maps them to raw.
/// </summary>
internal sealed class ThermalFieldMatrixSignal(SimulSignalSettings settings, Random random) : ISimulMatrixSignal
{
    private const double BaseTemperature = 25.0;
    private const double AxisPitchMm = 10.0;
    private const double NoiseAmplitude = 0.3;

    private readonly double hotSpotRise = settings.Amplitude ?? 40.0;
    private readonly double orbitFrequency = settings.Frequency ?? 0.025;
    private bool axesWritten;

    public void Fill(MatrixVariable matrix, double tSeconds)
    {
        var xCount = Math.Max(1, matrix.XCount);
        var yCount = Math.Max(1, matrix.YCount);

        if (!axesWritten)
        {
            for (var x = 0; x < matrix.XCount; x++)
            {
                matrix.TrySetEngValue(MatrixSectionKind.XAxis, x, x * AxisPitchMm);
            }

            for (var y = 0; y < matrix.YCount; y++)
            {
                matrix.TrySetEngValue(MatrixSectionKind.YAxis, y, y * AxisPitchMm);
            }

            axesWritten = true;
        }

        // Hot spot orbits around the plate centre; sigma ~ a quarter of the shorter side.
        var centreX = (xCount - 1) / 2.0;
        var centreY = (yCount - 1) / 2.0;
        var radius = 0.35 * Math.Min(xCount - 1, yCount - 1);
        var angle = 2.0 * Math.PI * orbitFrequency * tSeconds;
        var spotX = centreX + radius * Math.Cos(angle);
        var spotY = centreY + radius * Math.Sin(angle);
        var sigma = Math.Max(0.75, 0.25 * Math.Min(xCount, yCount));
        var twoSigmaSquared = 2.0 * sigma * sigma;

        var dataCount = matrix.DataCount;
        for (var index = 0; index < dataCount; index++)
        {
            var x = matrix.XCount == 0 ? index : index % xCount;
            var y = matrix.XCount == 0 ? 0 : index / xCount;
            var distanceSquared = (x - spotX) * (x - spotX) + (y - spotY) * (y - spotY);
            var temperature = BaseTemperature
                              + hotSpotRise * Math.Exp(-distanceSquared / twoSigmaSquared)
                              + NoiseAmplitude * (2.0 * random.NextDouble() - 1.0);
            matrix.TrySetEngValue(MatrixSectionKind.Data, index, temperature);
        }
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

    /// <summary>Matrix generator (MatrixVariable only): thermal field with an orbiting hot spot.</summary>
    public const string ThermalMatrixKey = "thermal";

    /// <summary>
    /// Writable parameter variable (direction="write"), not a generator: its live value is
    /// the nonlinearity for "laosstress". If it is misconfigured as read, it generates a
    /// constant so the protocol does not fault.
    /// </summary>
    public const string NonlinParamKey = "nonlin";

    /// <summary>
    /// Writable parameter: while its value is non-zero, matrix generators keep their data
    /// untouched (the user can edit cells without the simulation overwriting them).
    /// </summary>
    public const string HoldParamKey = "hold";

    /// <summary>Number of LAOS stress harmonics (1, 3, 5, 7) tunable via h{n}amp / h{n}freq / h{n}phase parameters.</summary>
    public const int TunableHarmonics = 4;

    /// <summary>Writable parameter key: absolute amplitude of LAOS stress harmonic number <paramref name="harmonic"/> (1-based; odd harmonic 2n-1).</summary>
    public static string HarmonicAmplitudeKey(int harmonic) => $"h{harmonic}amp";

    /// <summary>Writable parameter key: frequency [Hz] of LAOS stress harmonic number <paramref name="harmonic"/> (1-based).</summary>
    public static string HarmonicFrequencyKey(int harmonic) => $"h{harmonic}freq";

    /// <summary>Writable parameter key: phase [degrees] of LAOS stress harmonic number <paramref name="harmonic"/> (1-based).</summary>
    public static string HarmonicPhaseKey(int harmonic) => $"h{harmonic}phase";

    private static readonly HashSet<string> ParameterKeys = BuildParameterKeys();

    private static HashSet<string> BuildParameterKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal) { NonlinParamKey, HoldParamKey };
        for (var harmonic = 1; harmonic <= TunableHarmonics; harmonic++)
        {
            keys.Add(HarmonicAmplitudeKey(harmonic));
            keys.Add(HarmonicFrequencyKey(harmonic));
            keys.Add(HarmonicPhaseKey(harmonic));
        }

        return keys;
    }

    /// <summary>True for the writable parameter keys (nonlin, hold, h1amp..h4amp, h1freq..h4freq, h1phase..h4phase).</summary>
    public static bool IsParameterKey(string key) => ParameterKeys.Contains(key);

    public static bool IsMatrixKey(string key) => key is ThermalMatrixKey;

    public static bool IsKnown(string key) => key is StepKey or NoisyStepKey or Walk1Key or Walk2Key or Walk3Key
        or LaosStrainKey or LaosStressKey || IsMatrixKey(key) || IsParameterKey(key);

    public static ISimulSignal Create(string key, SimulSignalSettings settings)
    {
        if (IsParameterKey(key))
        {
            // Parameter misconfigured as a read signal: generate a constant so the protocol does not fault.
            return new ConstantSignal(key == NonlinParamKey ? settings.Nonlinearity ?? 0.0 : 0.0);
        }

        return key switch
        {
            StepKey => new StepSineSignal(),
            NoisyStepKey => new NoisyStepSineSignal(new Random()),
            Walk1Key => new MeanRevertingWalkSignal(new Random(), offset: 0.0, step: 0.5, reversion: 0.01),
            Walk2Key => new MeanRevertingWalkSignal(new Random(), offset: 10.0, step: 1.0, reversion: 0.02),
            Walk3Key => new MeanRevertingWalkSignal(new Random(), offset: -5.0, step: 0.3, reversion: 0.015),
            LaosStrainKey => new LaosStrainSignal(settings),
            LaosStressKey => new LaosStressSignal(settings),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown simulation signal.")
        };
    }

    public static ISimulMatrixSignal CreateMatrix(string key, SimulSignalSettings settings)
    {
        return key switch
        {
            ThermalMatrixKey => new ThermalFieldMatrixSignal(settings, new Random()),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown simulation matrix signal.")
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
