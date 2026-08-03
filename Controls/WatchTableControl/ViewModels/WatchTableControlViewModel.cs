using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.Controls.WatchTableControl.Models;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.WatchTableControl.ViewModels;

[DataContract]
public class WatchTableControlViewModel : ControlBase
{
	public WatchTableControlViewModel()
	{
		Width = 280;
		Height = 180;
	}

	#region Properties (not serialized - lazy, keeps working after deserialization)

	/// <summary>Table rows (one per bound variable). Lazy - DataContractSerializer
	/// does not run ctors/initializers, hence `field ??=`.</summary>
	[IgnoreDataMember]
	public ObservableCollection<WatchRow> Rows => field ??= [];

	[IgnoreDataMember]
	public WatchRow? SelectedRow
	{
		get;
		set
		{
			field = value;
			OnPropertyChanged();
			RemoveSelectedCommand.OnCanExecuteChanged();
		}
	}

	[IgnoreDataMember]
	public RelayCommand<object> RemoveSelectedCommand =>
		field ??= new RelayCommand<object>(_ => RemoveSelected(), _ => SelectedRow != null);

	// Column visibility flags bound by the context menu and (via BindingProxy) by the grid
	// columns. Runtime state only - persisted through ColumnLayout, which the layout behavior
	// re-applies to the columns after load. Nullable backing defaults to visible because
	// DataContractSerializer does not run initializers.
	private bool? isNameColumnVisible;
	private bool? isValueColumnVisible;
	private bool? isUnitColumnVisible;
	private bool? isTimeColumnVisible;

	[IgnoreDataMember]
	public bool IsNameColumnVisible
	{
		get => isNameColumnVisible ?? true;
		set { isNameColumnVisible = value; OnPropertyChanged(); }
	}

	[IgnoreDataMember]
	public bool IsValueColumnVisible
	{
		get => isValueColumnVisible ?? true;
		set { isValueColumnVisible = value; OnPropertyChanged(); }
	}

	[IgnoreDataMember]
	public bool IsUnitColumnVisible
	{
		get => isUnitColumnVisible ?? true;
		set { isUnitColumnVisible = value; OnPropertyChanged(); }
	}

	[IgnoreDataMember]
	public bool IsTimeColumnVisible
	{
		get => isTimeColumnVisible ?? true;
		set { isTimeColumnVisible = value; OnPropertyChanged(); }
	}

	[DataMember]
	public int RefreshTime
	{
		get;
		set
		{
			if (value < 0) value = 0;
			field = value;
			OnPropertyChanged();
		}
	} = 250;

	/// <summary>Column layout (order + width + visibility) captured from the grid by
	/// ColumnLayoutBehavior. Serialized into the project and re-applied after load.</summary>
	[DataMember]
	public List<GridColumnLayout>? ColumnLayout
	{
		get;
		set
		{
			field = value;
			OnPropertyChanged();
		}
	}

	#endregion

	#region Derived properties

	public override string ControlName => "WatchTableControl";
	public override string Label => "Watch Table";
	public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/WatchTable.png");
	public override string Description => "Watch table - several variables at once (name, value, unit, time).";

	#endregion

	#region Variable binding

	// Table rows are single-value: scalar or string (matrices have their own control)
	public override bool CanBindVariable(IVariableBase variable) => variable is ScalarVariable or StringVariable;

	public override void BindVariable(IVariableBase protVariable)
	{
		if (!CanBindVariable(protVariable))
		{
			return;
		}

		RememberVariableBinding(protVariable);
		var reference = GetVariableReference(protVariable);
		if (Rows.Any(row => row.Reference == reference))
		{
			return;
		}

		if (!Variables.Any(v => v.Equals(protVariable)))
		{
			Variables.Add(protVariable);
		}

		Rows.Add(new WatchRow
		{
			Reference = reference,
			Name = protVariable.Label,
			Unit = protVariable is ScalarVariable scalar ? scalar.Values.ValPresentation.Unit : string.Empty
		});
	}

	public override void RefreshVariableBinding(IVariableBase variable)
	{
		base.RefreshVariableBinding(variable);
		var row = Rows.FirstOrDefault(r => r.Reference == GetVariableReference(variable));
		if (row == null)
		{
			return;
		}

		row.Name = variable.Label;
		row.Unit = variable is ScalarVariable scalar ? scalar.Values.ValPresentation.Unit : string.Empty;
	}

	public override async Task UpdateVariableValueAsync(IVariableBase protVariable)
	{
		var raw = protVariable switch
		{
			ScalarVariable scalar => scalar.GetPresentationText(),
			StringVariable str => str.Values,
			_ => null
		};

		if (raw == null) return;

		var row = Rows.FirstOrDefault(r => r.Reference == GetVariableReference(protVariable));
		if (row == null) return;

		if (protVariable.Timestamp < row.LastUpdate) row.LastUpdate = DateTime.MinValue;
		if ((protVariable.Timestamp - row.LastUpdate).TotalMilliseconds < RefreshTime) return;
		row.LastUpdate = protVariable.Timestamp;

		var timestamp = protVariable.Timestamp;
		_ = Application.Current.Dispatcher.BeginInvoke(() =>
		{
			row.Value = raw;
			row.Time = timestamp;
		});
	}

	private void RemoveSelected()
	{
		var row = SelectedRow;
		if (row == null) return;

		Rows.Remove(row);
		LinkedVariables.RemoveAll(reference => reference == row.Reference);
		var variable = Variables.FirstOrDefault(v => GetVariableReference(v) == row.Reference);
		if (variable != null)
		{
			Variables.Remove(variable);
		}

		RaiseVariableBindingsChanged();

		SelectedRow = null;
	}

	#endregion
}
