using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.ViewModels;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using Telerik.Windows.DragDrop;
using System.Windows.Shapes;
using System.Windows.Media;
using Qenex.QSuite.Variables.QVariables;
using DragEventArgs = Telerik.Windows.DragDrop.DragEventArgs;

namespace Qenex.QInsight.DragDrop;

public class WorkspaceDragAndDropBehavior : Behavior<RadDiagram>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        
        DragDropManager.AddDropHandler(this.AssociatedObject, OnDrop);
    }
    
    

    private void OnDrop(object sender, Telerik.Windows.DragDrop.DragEventArgs e)
    {
        var variable = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedVariable");
        if (variable is IVariableBase)
        {
            return; // Nezpracovávej, nenastavuj e.Handled
        }
        
        var controlId = (int)DragDropPayloadManager.GetDataFromObject(e.Data, "ControlId"); 
        var control = DragDropPayloadManager.GetDataFromObject(e.Data, "NewDraggedControl");
        if (control is not IControlBase) return;
        
        var newControl = (IControlBase)Activator.CreateInstance(control.GetType())!;
        newControl.Id = controlId;
        
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
        
        DragDropPayloadManager.SetData(e.Data, "LastControlId", controlId);
        e.Handled = true;
    }


}