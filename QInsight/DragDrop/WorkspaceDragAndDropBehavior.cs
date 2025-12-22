using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.ViewModels;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using Telerik.Windows.DragDrop;
using System.Windows.Shapes;
using System.Windows.Media;
using DragEventArgs = Telerik.Windows.DragDrop.DragEventArgs;

namespace Qenex.QInsight.DragDrop;

public class WorkspaceDragAndDropBehavior : Behavior<RadDiagram>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        //DragDropManager.AddDragEnterHandler(this.AssociatedObject, OnDragEnter);
        DragDropManager.AddDropHandler(this.AssociatedObject, OnDrop);
    }
    
    // private void OnDragEnter(object sender, DragEventArgs e)
    // {
    //     var draggedControl = DragDropPayloadManager.GetDataFromObject(e.Data, "NewDraggedControl");
    //     if (draggedControl is not IControlBase control) return;
    //     
    //     var dragVisual = DragDropPayloadManager.GetDataFromObject(e.Data, "NewDraggedControlDragVisual");
    //     if (dragVisual is not ContentControl dragVisualContentControl) return;
    //     
    //     if (sender is not RadDiagram radDiagram) return;
    //     if (radDiagram.DataContext is not WorkspaceViewModel vm) return;
    //     
    //     var position = e.GetPosition(radDiagram);
    //     
    //     var dragVisualControl = new ContentControl
    //     {
    //         Content = new TextBlock() 
    //         {
    //             Width = control.Width,
    //             Height =  control.Height,
    //             Background = Brushes.Green,
    //             Text = control.Label,
    //         }
    //     };
    //     e.DragVisual = dragVisualControl;
    //     e.Effects = DragDropEffects.All;
    //     e.Handled = true;
    // }

    private void OnDrop(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
    {
        var control = DragDropPayloadManager.GetDataFromObject(e.Data, "NewDraggedControl");
        if (control is not IControlBase) return;
        
        var newControl = (IControlBase)Activator.CreateInstance(control.GetType())!;
        
        if (sender is not RadDiagram radDiagram) return;
        if (radDiagram.DataContext is not WorkspaceViewModel vm) return;
        
        var position = e.GetPosition(radDiagram);

        var snappedX = position.X;
        var snappedY = position.Y;

        if (vm.IsSnapToGridEnabled)
        {
            var cellSize = vm.GridCellSize;
            snappedX = Math.Round(position.X / cellSize) * cellSize;
            snappedY = Math.Round(position.Y / cellSize) * cellSize;
        }
        
        vm.AddControlToDiagram(newControl, snappedX, snappedY);
    }


}