using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;
using System.Drawing;
using System.Windows.Media;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Charting;
using Telerik.Windows.Controls.ChartView;
using Color = System.Windows.Media.Color;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public sealed class GraphControlViewModel : ControlBase
{
	
	#region Constructor

	public GraphControlViewModel()
	{
		var rnd = new Random();
		Width = 300;
		Height = 200;
		BackgroundColor = Colors.Aqua;
		
		PlottedDataSeries = [];
		LoadSampleData();

	}
	
	private void LoadSampleData()
	{
		// Create sample series 1
		var series1 = new ChartDataSeries("Temperature", Colors.Red);
		var series2 = new ChartDataSeries("Humidity", Colors.Blue);
		var series3 = new ChartDataSeries("Pressure", Colors.Green);
		var baseTime = DateTime.Now.AddHours(-24);
        
		for (int i = 0; i < 1000; i++)
		{
			series1.DataPoints.Add(new DataPoint(
				baseTime.AddHours(0.1 * i), 
				20 + 5*Math.Sin(0.1 * i)));
			
			series2.DataPoints.Add(new DataPoint(
				baseTime.AddHours(0.1 * i), 
				60 + 15*Math.Cos(0.1 * i)));

			series3.DataPoints.Add(new DataPoint(
				baseTime.AddHours(0.1 * i), 
				150 + 30*Math.Sin(0.1 * i)));
		}

		PlottedDataSeries.Add(series1);
		PlottedDataSeries.Add(series2);
		PlottedDataSeries.Add(series3);
	}

	#endregion
	#region Properties

	public ObservableCollection<ChartDataSeries> PlottedDataSeries { get; set; }
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
		return Task.CompletedTask;
	}

	public override void BindVariable(IVariableBase protVariable)
	{
	    
	}

	#endregion
	
	public class DataPoint
	{
		public DateTime Timestamp { get; set; }
		public double Value { get; set; }

		public DataPoint(DateTime timestamp, double value)
		{
			Timestamp = timestamp;
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