using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.ViewModels;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using Telerik.Windows.DragDrop;
using System.Windows.Shapes;
using System.Windows.Media;

namespace Qenex.QInsight.DragDrop;

public class WorkspaceDragAndDropBehavior : Behavior<RadDiagram>
{
    private ContentControl dragVisualControl;
    
    protected override void OnAttached()
    {
        base.OnAttached();
        DragDropManager.AddDragEnterHandler(this.AssociatedObject, OnDragEnter);
        DragDropManager.AddDropHandler(this.AssociatedObject, OnDrop);
    }
    
    private void OnDragEnter(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
    {
        // var control = DragDropPayloadManager.GetDataFromObject(e.Data, "NewDraggedControl");
        // if (control is not IControlBase iControl) return;
        //
        // if (sender is not RadDiagram radDiagram) return;
        // if (radDiagram.DataContext is not WorkspaceViewModel vm) return;
        //
        // var position = e.GetPosition(radDiagram);
        
        // ContentControl contentControl = (ContentControl)DragDropPayloadManager.GetDataFromObject(e.Data, "NewDraggedControlDragVisual");
        //
        // var r = new TextBlock()
        // {
        //     Height = iControl.Height,
        //     Width = iControl.Width,
        //     Fill = Brushes.LightGray,
        //     Stroke = Brushes.LightGray,
        // };
        // contentControl.Content = 
        

    }

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