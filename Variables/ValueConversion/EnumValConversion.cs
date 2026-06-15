namespace Qenex.QSuite.Variables.ValueConversion;

public class EnumValConversion : ValConversion
{
    public List<EnumType> Enums { get; set; }

    /// <summary>Raw (cele cislo) -> textovy nazev stavu; null kdyz neni nalezeno.</summary>
    public string? Map(double raw)
    {
        var key = (int)Math.Round(raw);
        return Enums?.FirstOrDefault(e => e.Value == key)?.Name;
    }
}