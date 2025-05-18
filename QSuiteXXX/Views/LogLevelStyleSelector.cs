using System.Windows.Controls;
using System.Windows;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Views;

public class LogLevelStyleSelector : StyleSelector
{
    #region Properties

    public Style DefaultLogBackgroundStyle { get; set; } = null!;
    public Style InfoLogBackgroundStyle { get; set; } = null!;
    public Style WarningLogBackgroundStyle { get; set; } = null!;
    public Style ErrorLogBackgroundStyle { get; set; } = null!;
    public Style FatalLogBackgroundStyle { get; set; } = null!;
	public Style TraceLogBackgroundStyle { get; set; } = null!;
    public Style DebugLogBackgroundStyle { get; set; } = null!;

    #endregion

    public override Style SelectStyle(object item, DependencyObject container)
    {
		var logMessage = item as ILogMessage;

        if (logMessage == null)	return DefaultLogBackgroundStyle;

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