using System.ComponentModel;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.Control;

public interface IControlVariable : IVariableBase, INotifyPropertyChanged
{
    string DisplayValue { get; set; }
}