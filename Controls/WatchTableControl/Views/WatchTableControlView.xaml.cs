using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Qenex.QSuite.Controls.WatchTableControl.Models;
using Qenex.QSuite.Controls.WatchTableControl.ViewModels;
using GridViewLength = Telerik.Windows.Controls.GridViewLength;
using RadMenuItem = Telerik.Windows.Controls.RadMenuItem;
using TelerikGridViewColumn = Telerik.Windows.Controls.GridViewColumn;

namespace Qenex.QSuite.Controls.WatchTableControl.Views;

public partial class WatchTableControlView : UserControl
{
	private bool listenersAttached;
	private bool isApplyingLayout;

	public WatchTableControlView()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		AttachColumnListeners();
		ApplyLayout();
		SyncColumnMenu();
	}

	// Sleduj poradi (DisplayIndex), sirku (Width) i viditelnost (IsVisible) sloupcu.
	private void AttachColumnListeners()
	{
		if (listenersAttached)
		{
			return;
		}

		var indexDescriptor = DependencyPropertyDescriptor.FromProperty(TelerikGridViewColumn.DisplayIndexProperty, typeof(TelerikGridViewColumn));
		var widthDescriptor = DependencyPropertyDescriptor.FromProperty(TelerikGridViewColumn.WidthProperty, typeof(TelerikGridViewColumn));
		var visibleDescriptor = DependencyPropertyDescriptor.FromProperty(TelerikGridViewColumn.IsVisibleProperty, typeof(TelerikGridViewColumn));

		foreach (var column in VariablesGrid.Columns)
		{
			indexDescriptor.AddValueChanged(column, OnColumnLayoutChanged);
			widthDescriptor.AddValueChanged(column, OnColumnLayoutChanged);
			visibleDescriptor.AddValueChanged(column, OnColumnLayoutChanged);
		}

		listenersAttached = true;
	}

	private void OnColumnLayoutChanged(object? sender, EventArgs e)
	{
		CaptureLayout();
		SyncColumnMenu();
	}

	private void CaptureLayout()
	{
		if (isApplyingLayout || DataContext is not WatchTableControlViewModel viewModel)
		{
			return;
		}

		viewModel.ColumnLayout = VariablesGrid.Columns
			.Cast<TelerikGridViewColumn>()
			.Where(column => !string.IsNullOrEmpty(column.UniqueName))
			.Select(column => new GridColumnLayout
			{
				UniqueName = column.UniqueName,
				DisplayIndex = column.DisplayIndex,
				Width = column.Width.IsAbsolute ? column.Width.Value : column.ActualWidth,
				IsVisible = column.IsVisible
			})
			.ToList();
	}

	private void ApplyLayout()
	{
		if (DataContext is not WatchTableControlViewModel viewModel)
		{
			return;
		}

		var layout = viewModel.ColumnLayout;
		if (layout == null || layout.Count == 0)
		{
			return;
		}

		isApplyingLayout = true;
		try
		{
			foreach (var saved in layout)
			{
				var column = FindColumn(saved.UniqueName);
				if (column == null) continue;
				column.IsVisible = saved.IsVisible;
				if (saved.Width > 0) column.Width = new GridViewLength(saved.Width);
			}

			foreach (var saved in layout.OrderBy(item => item.DisplayIndex))
			{
				var column = FindColumn(saved.UniqueName);
				if (column != null) column.DisplayIndex = saved.DisplayIndex;
			}
		}
		finally
		{
			isApplyingLayout = false;
		}
	}

	private TelerikGridViewColumn? FindColumn(string uniqueName)
		=> VariablesGrid.Columns.Cast<TelerikGridViewColumn>().FirstOrDefault(c => c.UniqueName == uniqueName);

	private void OnColumnVisibilityClick(object sender, RoutedEventArgs e)
	{
		if (sender is not RadMenuItem item || item.Tag is not string uniqueName)
		{
			return;
		}

		var column = FindColumn(uniqueName);
		if (column != null)
		{
			column.IsVisible = item.IsChecked;
		}
	}

	private void SyncColumnMenu()
	{
		SetItemChecked(ColNameItem, "Name");
		SetItemChecked(ColValueItem, "Value");
		SetItemChecked(ColUnitItem, "Unit");
		SetItemChecked(ColTimeItem, "Time");
	}

	private void SetItemChecked(RadMenuItem item, string uniqueName)
	{
		var column = FindColumn(uniqueName);
		if (column != null)
		{
			item.IsChecked = column.IsVisible;
		}
	}
}
