namespace Qenex.QSuite.Variables.ValueConversion;

public class LinearValConversion : ValConversion
{
    public double Multiplier { get; set; }
    public double Offset { get; set; }

    /// <summary>Raw -> engineering: eng = raw * Multiplier + Offset.</summary>
    public double Apply(double raw) => raw * Multiplier + Offset;

    /// <summary>Engineering -> raw (inverzni). Pouziti az pro zapisove controly; Multiplier 0 -> 0.</summary>
    public double Invert(double eng) => Multiplier == 0 ? 0 : (eng - Offset) / Multiplier;
}