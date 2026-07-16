using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Control schopny zapisovat hodnotu promenne do zarizeni. Oba delegaty injektuje host
/// (QInsight) pri konfiguraci controlu - control sam o protokolech/driverech nic nevi
/// (stejny vzor jako IFileDialogAwareControl).
/// </summary>
public interface IVariableWriteControl
{
    /// <summary>
    /// Host injektuje: vraci true, pokud promennou lze zapisovat do zarizeni
    /// (capability urcuje protokol, napr. Modbus Direction ci XCP calibration).
    /// </summary>
    Func<IVariableBase, bool>? CanWriteVariableProvider { get; set; }

    /// <summary>
    /// Host injektuje: zapise inzenyrskou hodnotu promenne do zarizeni
    /// (inverzni konverze eng->raw + notifikace command driveru probiha na strane hosta).
    /// </summary>
    Func<IVariableBase, double, Task<bool>>? WriteVariableEngValueAsync { get; set; }

    /// <summary>
    /// Prehodnoti dostupnost zapisu pro aktualne navazane promenne.
    /// Host vola po injektazi delegatu a po kazdem (re)bindu promenne.
    /// </summary>
    void RefreshWriteCapability();
}
