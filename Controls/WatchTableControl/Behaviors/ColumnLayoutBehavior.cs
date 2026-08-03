using System.ComponentModel;
using System.Windows;
using Qenex.QSuite.Controls.WatchTableControl.Models;
using RadGridView = Telerik.Windows.Controls.RadGridView;
using TelerikGridViewColumn = Telerik.Windows.Controls.GridViewColumn;
using GridViewLength = Telerik.Windows.Controls.GridViewLength;

namespace Qenex.QSuite.Controls.WatchTableControl.Behaviors;

/// <summary>
/// Attached behavior syncing the RadGridView column layout (order, width, visibility)
/// with a bindable list so the view model can persist it without any code-behind.
/// Usage: IsEnabled="True" + Layout="{Binding ..., Mode=TwoWay}" on the grid.
/// </summary>
public static class ColumnLayoutBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(ColumnLayoutBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty LayoutProperty = DependencyProperty.RegisterAttached(
        "Layout", typeof(List<GridColumnLayout>), typeof(ColumnLayoutBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLayoutChanged));

    private static readonly DependencyProperty ControllerProperty = DependencyProperty.RegisterAttached(
        "Controller", typeof(Controller), typeof(ColumnLayoutBehavior), new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static List<GridColumnLayout>? GetLayout(DependencyObject obj) => (List<GridColumnLayout>?)obj.GetValue(LayoutProperty);
    public static void SetLayout(DependencyObject obj, List<GridColumnLayout>? value) => obj.SetValue(LayoutProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RadGridView grid && (bool)e.NewValue && grid.GetValue(ControllerProperty) == null)
        {
            grid.SetValue(ControllerProperty, new Controller(grid));
        }
    }

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Controller?)d.GetValue(ControllerProperty))?.OnLayoutBindingChanged();
    }

    private sealed class Controller
    {
        private readonly RadGridView grid;
        private bool initialized;
        private bool applying;
        private bool capturing;

        public Controller(RadGridView grid)
        {
            this.grid = grid;
            if (grid.IsLoaded)
            {
                Initialize();
            }
            else
            {
                grid.Loaded += OnGridLoaded;
            }
        }

        private void OnGridLoaded(object sender, RoutedEventArgs e)
        {
            grid.Loaded -= OnGridLoaded;
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            Apply();

            // Track order (DisplayIndex), width (Width) and visibility (IsVisible) of the columns.
            var indexDescriptor = DependencyPropertyDescriptor.FromProperty(TelerikGridViewColumn.DisplayIndexProperty, typeof(TelerikGridViewColumn));
            var widthDescriptor = DependencyPropertyDescriptor.FromProperty(TelerikGridViewColumn.WidthProperty, typeof(TelerikGridViewColumn));
            var visibleDescriptor = DependencyPropertyDescriptor.FromProperty(TelerikGridViewColumn.IsVisibleProperty, typeof(TelerikGridViewColumn));

            foreach (var column in grid.Columns.Cast<TelerikGridViewColumn>())
            {
                indexDescriptor.AddValueChanged(column, OnColumnLayoutChanged);
                widthDescriptor.AddValueChanged(column, OnColumnLayoutChanged);
                visibleDescriptor.AddValueChanged(column, OnColumnLayoutChanged);
            }

            initialized = true;
        }

        public void OnLayoutBindingChanged()
        {
            // Ignore the echo of our own capture; a genuine new list from the view model
            // (e.g. after project load) is re-applied to the columns.
            if (capturing || !initialized)
            {
                return;
            }

            Apply();
        }

        private void OnColumnLayoutChanged(object? sender, EventArgs e)
        {
            Capture();
        }

        private void Capture()
        {
            if (applying)
            {
                return;
            }

            var layout = grid.Columns
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

            capturing = true;
            try
            {
                // SetCurrentValue keeps the TwoWay binding alive and pushes the list to the source.
                grid.SetCurrentValue(LayoutProperty, layout);
            }
            finally
            {
                capturing = false;
            }
        }

        private void Apply()
        {
            var layout = GetLayout(grid);
            if (layout == null || layout.Count == 0)
            {
                return;
            }

            applying = true;
            try
            {
                foreach (var saved in layout)
                {
                    var column = FindColumn(saved.UniqueName);
                    if (column == null) continue;
                    column.IsVisible = saved.IsVisible;
                    if (saved.Width > 0) column.Width = new GridViewLength(saved.Width);
                }

                // Apply order lowest index first so the columns end up in the saved sequence.
                foreach (var saved in layout.OrderBy(item => item.DisplayIndex))
                {
                    var column = FindColumn(saved.UniqueName);
                    if (column != null) column.DisplayIndex = saved.DisplayIndex;
                }
            }
            finally
            {
                applying = false;
            }
        }

        private TelerikGridViewColumn? FindColumn(string uniqueName)
            => grid.Columns.Cast<TelerikGridViewColumn>().FirstOrDefault(c => c.UniqueName == uniqueName);
    }
}
