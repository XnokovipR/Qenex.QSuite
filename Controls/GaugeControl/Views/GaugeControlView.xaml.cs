using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Qenex.QSuite.Controls.GaugeControl.ViewModels;
using Telerik.Windows.Controls.Gauge;

namespace Qenex.QSuite.Controls.GaugeControl.Views;

public partial class GaugeControlView : UserControl
{
	private static readonly SolidColorBrush NormalBrush = CreateFrozen(Colors.Green);
	private static readonly SolidColorBrush WarningBrush = CreateFrozen(Colors.Yellow);
	private static readonly SolidColorBrush ErrorBrush = CreateFrozen(Colors.Red);

	private GaugeControlViewModel? viewModel;

	public GaugeControlView()
	{
		InitializeComponent();
		Loaded += (_, _) => RebuildRanges();
		DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (viewModel != null)
		{
			viewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}

		viewModel = e.NewValue as GaugeControlViewModel;

		if (viewModel != null)
		{
			viewModel.PropertyChanged += OnViewModelPropertyChanged;
		}

		RebuildRanges();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(GaugeControlViewModel.Minimum):
			case nameof(GaugeControlViewModel.Maximum):
			case nameof(GaugeControlViewModel.LowErrorLevel):
			case nameof(GaugeControlViewModel.LowWarningLevel):
			case nameof(GaugeControlViewModel.HighWarningLevel):
			case nameof(GaugeControlViewModel.HighErrorLevel):
			case nameof(GaugeControlViewModel.Kind):
				RebuildRanges();
				break;
		}
	}

	private void RebuildRanges()
	{
		if (viewModel == null)
		{
			return;
		}

		var zones = viewModel.GetZones();
		ApplyRanges(RadialScale?.Ranges, zones);
		ApplyRanges(VerticalScale?.Ranges, zones);
		ApplyRanges(HorizontalScale?.Ranges, zones);
	}

	private static void ApplyRanges(GaugeRangeCollection? ranges, IReadOnlyList<GaugeZone> zones)
	{
		if (ranges == null)
		{
			return;
		}

		ranges.Clear();
		foreach (var zone in zones)
		{
			ranges.Add(new GaugeRange
			{
				Min = zone.Min,
				Max = zone.Max,
				StartWidth = 0.1,
				EndWidth = 0.1,
				Background = BrushFor(zone.Kind)
			});
		}
	}

	private static Brush BrushFor(GaugeZoneKind kind) => kind switch
	{
		GaugeZoneKind.Error => ErrorBrush,
		GaugeZoneKind.Warning => WarningBrush,
		_ => NormalBrush
	};

	private static SolidColorBrush CreateFrozen(Color color)
	{
		var brush = new SolidColorBrush(color);
		brush.Freeze();
		return brush;
	}
}
