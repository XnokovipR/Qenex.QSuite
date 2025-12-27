using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.ViewModels;
using Qenex.QInsight.Views;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using Telerik.Windows.DragDrop;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace Qenex.QInsight.DragDrop;

public class ControlsDragBehavior : Behavior<ItemsControl>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        DragDropManager.AddDragInitializeHandler(this.AssociatedObject, OnDragInitialized);
        DragDropManager.AddDragDropCompletedHandler(this.AssociatedObject, OnDropComleted);
    }
    
    private void OnDragInitialized(object sender, DragInitializeEventArgs e)
    {
        if (sender is not RadListBox radListBox) return;
        if (radListBox.DataContext is not ControlsViewModel vm) return;
        if (((FrameworkElement)e.OriginalSource).DataContext is not IControlBase control) return;
        
        
        var dragVisualControl = new ContentControl();
        var payload = DragDropPayloadManager.GeneratePayload(null);

        var controlId = vm.LastControlId + 1;
        payload.SetData("ControlId", controlId);
        payload.SetData("NewDraggedControl", control);
        payload.SetData("NewDraggedControlDragVisual", dragVisualControl);
        e.Data = payload;

        var bgColor = ShellWindow.MainAppSettings.Design.AppTheme == ApplicationTheme.Dark
            ? ShellWindow.MainAppSettings.Design.DarkThemeControlBackgroundColor
            : ShellWindow.MainAppSettings.Design.LightThemeControlBackgroundColor;
        
        var fgColor = ShellWindow.MainAppSettings.Design.AppTheme == ApplicationTheme.Dark
            ? ShellWindow.MainAppSettings.Design.DarkThemeTextColor
            : ShellWindow.MainAppSettings.Design.LightThemeTextColor;
        
        var border = new Border
        {
            Height = control.Height,
            Width = control.Width,
            Background = new SolidColorBrush(bgColor),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                FontSize = 14,
                Text = control.Label,
                Foreground = new SolidColorBrush(fgColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        dragVisualControl.Content = border;
        
        e.AllowedEffects = DragDropEffects.All;
        e.DragVisual = dragVisualControl;
        e.DragVisualOffset = new Point(e.RelativeStartPoint.X, e.RelativeStartPoint.Y);
        e.Handled = true;
    }

    private void OnDropComleted(object sender, DragDropCompletedEventArgs e)
    {
        if (sender is not RadListBox radListBox) return;
        if (radListBox.DataContext is not ControlsViewModel vm) return;
        
        var controlId = (int)DragDropPayloadManager.GetDataFromObject(e.Data, "LastControlId");
        vm.LastControlId = controlId;
        
        e.Handled = true;
    }
}