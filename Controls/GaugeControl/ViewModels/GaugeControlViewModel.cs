using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.GaugeControl.ViewModels;

/// <summary>Typ/orientace gauge prvku.</summary>
public enum GaugeKind
{
	Radial,
	LinearHorizontal,
	LinearVertical
}

/// <summary>Stav zony / aktualni hodnoty z pohledu prahu.</summary>
public enum GaugeZoneKind
{
	Normal,
	Warning,
	Error
}

/// <summary>Datovy popis jedne barevne zony (bez Telerik zavislosti - mapuje se ve View).</summary>
public sealed class GaugeZone
{
	public double Min { get; init; }
	public double Max { get; init; }
	public GaugeZoneKind Kind { get; init; }
}

[DataContract]
public class GaugeControlViewModel : ControlBase
{
	private DateTime previousUpdateTime = DateTime.MinValue;

	public GaugeControlViewModel()
	{
		Width = 200;
		Height = 200;
		VariableLabel = "----------";
		VariableUnit = "-";
	}

	#region Display properties (neserializovane)

	[IgnoreDataMember]
	public string VariableLabel { get; set { field = value; OnPropertyChanged(); } }

	[IgnoreDataMember]
	public string VariableUnit { get; set { field = value; OnPropertyChanged(); } }

	[IgnoreDataMember]
	public double Value
	{
		get;
		set { field = value; OnPropertyChanged(); UpdateAlarmState(); }
	}

	[IgnoreDataMember]
	public GaugeZoneKind AlarmState
	{
		get;
		private set { field = value; OnPropertyChanged(); }
	}

	private static readonly GaugeKind[] AllKinds = Enum.GetValues<GaugeKind>();

	/// <summary>Nabidka typu pro prepinac v nastaveni controlu. Staticke - funguje i po deserializaci
	/// projektu (DataContractSerializer obchazi konstruktor i property initializery).</summary>
	[IgnoreDataMember]
	public IReadOnlyList<GaugeKind> AvailableKinds => AllKinds;

	#endregion

	#region Configuration (serializovane)

	[DataMember]
	public GaugeKind Kind { get; set { field = value; OnPropertyChanged(); } } = GaugeKind.Radial;

	[DataMember]
	public double Minimum { get; set { field = value; OnPropertyChanged(); UpdateAlarmState(); } }

	[DataMember]
	public double Maximum { get; set { field = value; OnPropertyChanged(); UpdateAlarmState(); } } = 100;

	[DataMember]
	public double? LowErrorLevel { get; set { field = value; OnPropertyChanged(); UpdateAlarmState(); } }

	[DataMember]
	public double? LowWarningLevel { get; set { field = value; OnPropertyChanged(); UpdateAlarmState(); } }

	[DataMember]
	public double? HighWarningLevel { get; set { field = value; OnPropertyChanged(); UpdateAlarmState(); } }

	[DataMember]
	public double? HighErrorLevel { get; set { field = value; OnPropertyChanged(); UpdateAlarmState(); } }

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

	#endregion

	#region Derived properties

	public override string ControlName => "GaugeControl";
	public override string Label => "Gauge";
	public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/Gauge.png");
	public override string Description => "Gauge (radial / linear) s hladinami Warning a Error pro spodni i horni mez.";

	#endregion

	#region Levels / zones

	/// <summary>Zarazeni hodnoty do zony podle nastavenych prahu (4 hladiny, kazda volitelna).</summary>
	public GaugeZoneKind Classify(double value)
	{
		if (LowErrorLevel is { } lowError && value < lowError) return GaugeZoneKind.Error;
		if (HighErrorLevel is { } highError && value > highError) return GaugeZoneKind.Error;
		if (LowWarningLevel is { } lowWarning && value < lowWarning) return GaugeZoneKind.Warning;
		if (HighWarningLevel is { } highWarning && value > highWarning) return GaugeZoneKind.Warning;
		return GaugeZoneKind.Normal;
	}

	/// <summary>Souvisle barevne zony pres cely rozsah (Min..Max) podle nastavenych prahu.</summary>
	public IReadOnlyList<GaugeZone> GetZones()
	{
		var bounds = new List<double> { Minimum, Maximum };
		foreach (var level in new[] { LowErrorLevel, LowWarningLevel, HighWarningLevel, HighErrorLevel })
		{
			if (level is { } value && value > Minimum && value < Maximum)
			{
				bounds.Add(value);
			}
		}

		bounds = bounds.Distinct().OrderBy(x => x).ToList();

		// Nepatrna mezera na vnitrnich hranicich, aby se sousedni GaugeRange neprekryvaly
		// (Telerik kresli pozdeji pridanou zonu pres sdilenou hranici -> "cervena nad zlutou").
		var range = Maximum - Minimum;
		var gap = range > 0 ? range * 0.004 : 0;

		var zones = new List<GaugeZone>();
		for (var i = 0; i < bounds.Count - 1; i++)
		{
			var min = bounds[i];
			var max = bounds[i + 1];
			if (max <= min) continue;

			var kind = Classify((min + max) / 2);
			var zoneMin = i == 0 ? min : min + gap / 2;
			var zoneMax = i == bounds.Count - 2 ? max : max - gap / 2;
			if (zoneMax <= zoneMin) continue;

			zones.Add(new GaugeZone { Min = zoneMin, Max = zoneMax, Kind = kind });
		}

		return zones;
	}

	private void UpdateAlarmState() => AlarmState = Classify(Value);

	#endregion

	#region Variable binding

	// Rucicka ukazuje jednu ciselnou hodnotu: jen skalarni promenne
	public override bool CanBindVariable(IVariableBase variable) => variable is ScalarVariable;

	public override void BindVariable(IVariableBase protVariable)
	{
		if (!CanBindVariable(protVariable) || Variables.Any(v => v.Equals(protVariable)))
		{
			return;
		}

		// Single-variable control: replace the previous variable (host unsubscribes the old).
		Variables.Clear();
		LinkedVariables.Clear();

		RememberVariableBinding(protVariable);
		Variables.Add(protVariable);
		VariableLabel = protVariable.Label;
		VariableUnit = protVariable is ScalarVariable variable ? variable.Values.ValPresentation.Unit : string.Empty;
	}

	public override void RefreshVariableBinding(IVariableBase variable)
	{
		base.RefreshVariableBinding(variable);
		if (!IsVariableBound(variable))
		{
			return;
		}

		VariableLabel = variable.Label;
		VariableUnit = variable is ScalarVariable scalarVariable
			? scalarVariable.Values.ValPresentation.Unit
			: string.Empty;
	}

	public override async Task UpdateVariableValueAsync(IVariableBase protVariable)
	{
		double engValue;
		switch (protVariable)
		{
			case ScalarVariable scalar:
				engValue = scalar.GetEngValue();
				break;
			case StringVariable str when double.TryParse(str.Values, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
				engValue = parsed;
				break;
			default:
				return;
		}

		if (protVariable.Timestamp < previousUpdateTime) previousUpdateTime = DateTime.MinValue;
		if ((protVariable.Timestamp - previousUpdateTime).TotalMilliseconds < RefreshTime) return;
		previousUpdateTime = protVariable.Timestamp;

		_ = Application.Current.Dispatcher.BeginInvoke(() => Value = engValue);
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext context)
	{
		VariableLabel ??= "----------";
		VariableUnit ??= string.Empty;
		Variables ??= [];
		LinkedVariables ??= [];
		UpdateAlarmState();
	}

	#endregion
}
