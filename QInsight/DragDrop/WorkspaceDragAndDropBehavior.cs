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
        e.Handled = false;
        if (TryGetPayload<IVariableBase>(e.Data, "DraggedVariable", out _))
        {
            return; 
        }
        
        if (sender is not RadDiagram radDiagram) return;
        if (radDiagram.DataContext is not WorkspaceViewModel vm) return;
        
        if (!TryGetPayload<IControlBase>(e.Data, "NewDraggedControl", out var control)) return;

        if (Activator.CreateInstance(control.GetType()) is not IControlBase newControl) return;
        // The Id comes from the target workspace state (max of the same control type + 1) — a
        // session counter produced duplicates with controls loaded from the project, and such
        // controls silently lost their variable bindings on the next project open.
        newControl.Id = vm.CreateUniqueControlId(control.GetType());

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
        e.Handled = true;
    }

    private static bool TryGetPayload<T>(object data, string key, out T value)
    {
        value = default!;

        try
        {
            if (DragDropPayloadManager.GetDataFromObject(data, key) is T typedValue)
            {
                value = typedValue;
                return true;
            }
        }
        catch (NullReferenceException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }

        return false;
    }

}
