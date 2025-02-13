using System.Windows.Controls;
using System.Windows;
using Qenex.QSuite.LogSystem;
using Syncfusion.UI.Xaml.Grid;

namespace Qenex.QSuite.Views;

public class LogLevelStyleSelector : StyleSelector
{
    #region Properties

    public Style DefaultLogBackgroundStyle { get; set; }
    public Style InfoLogBackgroundStyle { get; set; }
    public Style WarningLogBackgroundStyle { get; set; }
    public Style ErrorLogBackgroundStyle { get; set; }
    public Style FatalLogBackgroundStyle { get; set; }
    public Style TraceLogBackgroundStyle { get; set; }
    public Style DebugLogBackgroundStyle { get; set; }

    #endregion

    public override Style SelectStyle(object item, DependencyObject container)
    {
        if (item is not DataRow dataRow)
        {
            return DefaultLogBackgroundStyle;
        }

        if (dataRow.RowData is not LogMessage logMessage)
        {
            return DefaultLogBackgroundStyle;
        }

        switch (logMessage.Level)
        {
            case LogLevel.Info: return InfoLogBackgroundStyle;
            case LogLevel.Warn: return WarningLogBackgroundStyle;
            case LogLevel.Error: return ErrorLogBackgroundStyle;
            case LogLevel.Fatal: return FatalLogBackgroundStyle;
            case LogLevel.Trace: return TraceLogBackgroundStyle;
            case LogLevel.Debug: return DebugLogBackgroundStyle;
        }

        return DefaultLogBackgroundStyle;
    }    
}