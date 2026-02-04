using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using Telerik.Charting;
using Telerik.Windows.Controls.ChartView;
using Color = System.Windows.Media.Color;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public sealed class GraphControlViewModel : ControlBase
{
	
	private DateTime baseTime;
	
	#region Constructor

	public GraphControlViewModel()
	{
		ZoomPanCommand = new RelayCommand<object>(p =>
		{
			 /* Implement zoom and pan logic here */
		});
		
		var rnd = new Random();
		Width = 300;
		Height = 200;
		BackgroundColor = Colors.Aqua;
		
		PlottedDataSeries = [];
		//LoadSampleData();
	}
	
	private void LoadSampleData()
	{
		// Create sample series 1
		var series1 = new ChartDataSeries("Temperature", Colors.Red);
		var series2 = new ChartDataSeries("Humidity", Colors.Blue);
		var series3 = new ChartDataSeries("Pressure", Colors.Green);
		baseTime = DateTime.UtcNow;

		var f = 1;
		for (int i = 0; i < 5000; i += 10)
		{
			series1.DataPoints.Add(new DataPoint(
				baseTime,
				baseTime.AddMilliseconds(i), 
				20 + 5 * Math.Sin(2 * Math.PI * f * i / 1000.0)));
			
			series2.DataPoints.Add(new DataPoint(
				baseTime,
				baseTime.AddMilliseconds(i), 
				60 + 15 * Math.Sin(2 * Math.PI * f * i / 1000.0)));

			series3.DataPoints.Add(new DataPoint(
				baseTime,
				baseTime.AddMilliseconds(i), 
				150 + 30 * Math.Sin(2 * Math.PI * f * i / 1000.0)));
		}

		PlottedDataSeries.Add(series1);
		PlottedDataSeries.Add(series2);
		PlottedDataSeries.Add(series3);
	}

	#endregion
	#region Properties

	public ObservableCollection<ChartDataSeries> PlottedDataSeries { get; set; }
	
	public RelayCommand<object> ZoomPanCommand { get; set; }
	
	#endregion

	#region Derived properties

	public override string ControlName => "GraphControl";
	public override string Label => "Graph";
	public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
	public override string Description => "Graph Control for displaying data in a graphical format.";

	#endregion

	#region Overrides of ControlBase

	public override Task UpdateVariableValueAsync(IVariableBase variable)
	{
		if (variable is ScalarVariable scalarVariable)
		{
			if (baseTime == DateTime.MinValue) baseTime = DateTime.UtcNow;
			var series = PlottedDataSeries.FirstOrDefault(s => s.Name.Equals(scalarVariable.Name));
			if (series == null) return Task.CompletedTask;
			
			var val = Convert.ToDouble(scalarVariable.Values is Values<int> vv ? vv.Value : 5);
			var timestamp = DateTime.UtcNow;
			
			_ = Application.Current.Dispatcher.BeginInvoke(() =>
			{
				series.DataPoints.Add(new DataPoint(baseTime, timestamp, val));
			});
		}
		return Task.CompletedTask;
	}

	public override void BindVariable(IVariableBase variable)
	{
	    Variables.Add(variable);
	    
	    var series = new ChartDataSeries(variable.Name, Colors.Red);
	    PlottedDataSeries.Add(series);
	}

	#endregion
	
	public class DataPoint
	{
		public DateTime Timestamp { get; set; }
		public double RelativeTimeMs { get; set; }
		public double Value { get; set; }

		public DataPoint(DateTime baseTime, DateTime timestamp, double value)
		{
			Timestamp = timestamp;
			RelativeTimeMs = (timestamp - baseTime).TotalMilliseconds;
			Value = value;
		}
	}
	
	public class ChartDataSeries
	{
		public string Name { get; set; }
		public Color Color { get; set; }
		public ObservableCollection<DataPoint> DataPoints { get; set; }

		public ChartDataSeries(string name, Color color)
		{
			Name = name;
			Color = color;
			DataPoints = new ObservableCollection<DataPoint>();
		}
	}
}