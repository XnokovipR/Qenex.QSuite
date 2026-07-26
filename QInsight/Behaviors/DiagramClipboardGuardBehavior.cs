using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;

namespace Qenex.QInsight.Behaviors;

/// <summary>
/// Disables the diagram's built-in clipboard commands (Ctrl+C/X/V and context-menu items).
/// RadDiagram's own copy/paste clones only the visual shape — without the control's view
/// model, Id, and variable bindings — which produces an unusable orphan. Remove this guard
/// once a real control copy (deep clone + new Id + rebinding) is implemented.
/// </summary>
public class DiagramClipboardGuardBehavior : Behavior<RadDiagram>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        CommandManager.AddPreviewCanExecuteHandler(AssociatedObject, OnPreviewCanExecute);
    }

    protected override void OnDetaching()
    {
        CommandManager.RemovePreviewCanExecuteHandler(AssociatedObject, OnPreviewCanExecute);
        base.OnDetaching();
    }

    private static void OnPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Command == DiagramCommands.Copy
            || e.Command == DiagramCommands.Cut
            || e.Command == DiagramCommands.Paste)
        {
            e.CanExecute = false;
            e.ContinueRouting = false;
            e.Handled = true;
        }
    }
}
