namespace Qenex.QSuite.Controls.XYGraphControl.Helpers;

public class ChartHelper
{
    public static readonly Dictionary<int, System.Windows.Media.Color> OldChartColors = new Dictionary<int, System.Windows.Media.Color>
    {
        { 0, System.Windows.Media.Colors.Blue },
        { 1, System.Windows.Media.Colors.Green },
        { 2, System.Windows.Media.Colors.Red },
        { 3, System.Windows.Media.Colors.Orange },
        { 4, System.Windows.Media.Colors.Magenta },
        { 5, System.Windows.Media.Colors.Olive },
        { 6, System.Windows.Media.Colors.Yellow },
        { 7, System.Windows.Media.Colors.Cyan },
        { 8, System.Windows.Media.Colors.Lime },
        { 9, System.Windows.Media.Colors.Silver },
        { 10, System.Windows.Media.Colors.Coral },
        { 11, System.Windows.Media.Colors.Purple },
        { 12, System.Windows.Media.Colors.Brown },
        { 13, System.Windows.Media.Colors.Gray },
        { 14, System.Windows.Media.Colors.Pink },
        { 15, System.Windows.Media.Colors.Teal },
        { 16, System.Windows.Media.Colors.Navy },
        { 17, System.Windows.Media.Colors.Maroon },
        { 18, System.Windows.Media.Colors.Gold },
        { 19, System.Windows.Media.Colors.Turquoise }
    };
    
    public static readonly Dictionary<int, System.Windows.Media.Color> ChartColors = new Dictionary<int, System.Windows.Media.Color>
    {
        // Dobře viditelné na bílém i tmavě šedém pozadí, vzájemně dobře odlišitelné
        { 0, System.Windows.Media.Color.FromRgb(220, 50, 50) },    // Red (sytá červená)
        { 1, System.Windows.Media.Color.FromRgb(40, 130, 220) },   // Blue (světlejší modrá, čitelná na šedé)
        { 2, System.Windows.Media.Color.FromRgb(40, 170, 70) },    // Green (středně sytá zelená)
        { 3, System.Windows.Media.Color.FromRgb(255, 140, 0) },    // Orange (DarkOrange)
        { 4, System.Windows.Media.Color.FromRgb(190, 60, 200) },   // Magenta/Purple
        { 5, System.Windows.Media.Color.FromRgb(0, 170, 170) },    // Teal
        { 6, System.Windows.Media.Color.FromRgb(150, 90, 40) },    // Brown
        { 7, System.Windows.Media.Color.FromRgb(230, 100, 150) },  // Pink (sytější)
        { 8, System.Windows.Media.Color.FromRgb(120, 110, 30) },   // Olive (tmavší)
        { 9, System.Windows.Media.Color.FromRgb(100, 90, 220) },   // Indigo/Violet
        { 10, System.Windows.Media.Color.FromRgb(220, 130, 40) },  // Coral/Amber
        { 11, System.Windows.Media.Color.FromRgb(80, 160, 160) },  // Cadet
        { 12, System.Windows.Media.Color.FromRgb(180, 40, 100) },  // Crimson/Rose
        { 13, System.Windows.Media.Color.FromRgb(70, 140, 50) },   // Forest green
        { 14, System.Windows.Media.Color.FromRgb(160, 100, 200) }, // Lavender (sytější)
        { 15, System.Windows.Media.Color.FromRgb(200, 110, 80) },  // Terracotta
        { 16, System.Windows.Media.Color.FromRgb(110, 130, 40) },  // Moss
        { 17, System.Windows.Media.Color.FromRgb(50, 150, 200) },  // Sky blue (tmavší)
        { 18, System.Windows.Media.Color.FromRgb(190, 90, 60) },   // Sienna
        { 19, System.Windows.Media.Color.FromRgb(130, 70, 160) }   // Plum
    };
}
