using System.ComponentModel;
using System.Runtime.CompilerServices;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QLibs.QUI;

namespace Qenex.QSuite.Controls.Control;

public class ControlVariable : VariableBase, IControlVariable
{
    private string displayValue = string.Empty;
    

    public event PropertyChangedEventHandler? PropertyChanged;
    
    public virtual string DisplayValue
    {
        get => displayValue;
        set
        {
            displayValue = value;
            OnPropertyChanged();
        }
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        if (this.PropertyChanged == null) return;
        var propertyChanged = this.PropertyChanged;
        propertyChanged((object) this, new PropertyChangedEventArgs(propertyName));
    }
}