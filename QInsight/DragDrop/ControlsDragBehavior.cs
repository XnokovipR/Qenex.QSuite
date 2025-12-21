using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.DragDrop;

namespace Qenex.QInsight.DragDrop;

public class ControlsDragBehavior : Behavior<ItemsControl>
{
    //private ContentControl dragVisualControl;
    
    protected override void OnAttached()
    {
        base.OnAttached();
        DragDropManager.AddDragInitializeHandler(this.AssociatedObject, OnDragInitialized);
    }
    
    private void OnDragInitialized(object sender, DragInitializeEventArgs e)
    {
        var dragVisualControl = new ContentControl();
        e.AllowedEffects = DragDropEffects.Copy;
        var payload = DragDropPayloadManager.GeneratePayload(null);
        if (((FrameworkElement)e.OriginalSource).DataContext is not IControlBase control) return;
        
        payload.SetData("NewDraggedControl", control);
        payload.SetData("NewDraggedControlDragVisual", dragVisualControl);
        e.Data = payload;
        
        dragVisualControl.Content = new TextBlock() 
        {
            Width = control.Width,
            Height =  control.Height,
            Background = Brushes.Aqua,
            Text = control.Label,
            FontSize = 13, 
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        e.AllowedEffects = DragDropEffects.All;
        e.DragVisual = dragVisualControl;
        e.DragVisualOffset = new Point(e.RelativeStartPoint.X, e.RelativeStartPoint.Y);
        e.Handled = true;
    }
    
}