using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.ViewModels;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QInsight.Views;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.Controls;
using Telerik.Windows.DragDrop;

namespace Qenex.QInsight.DragDrop;

public class VariableDragAndDropBehavior : Behavior<ItemsControl>
{
    public static bool IsOverValidTarget { get; set; }
    
    protected override void OnAttached()
    {
        base.OnAttached();
        DragDropManager.AddDragInitializeHandler(AssociatedObject, OnDragInitialized);
        DragDropManager.AddGiveFeedbackHandler(AssociatedObject, OnGiveFeedback);
        DragDropManager.AddDragDropCompletedHandler(this.AssociatedObject, OnDropComleted);
    }
    
    private void OnDragInitialized(object sender, DragInitializeEventArgs e)
    {
        if (e.OriginalSource is not FrameworkElement source) return;

        IVariableBase variable;
        IProtocolVariable? protocolVariable = null;
        if (source.DataContext is ProtocolVariableWrapper protocolVariableWrapper)
        {
            protocolVariable = protocolVariableWrapper.ProtocolVariable;
            variable = protocolVariable.Variable;
        }
        else if (source.DataContext is VariableSeWrapper varWrapper)
        {
            variable = varWrapper.Variable;
        }
        else
        {
            return;
        }

        if (sender is not RadTreeView treeView) return;
        if (treeView.DataContext is not SolutionExplorerViewModel vm) return;

        if (protocolVariable != null && IsNonLiveSourceProtocolVariable(vm.ProjectModules, protocolVariable))
        {
            protocolVariable = null;
        }

        protocolVariable ??= FindProtocolVariable(vm.ProjectModules, variable);
        if (protocolVariable == null)
        {
            return;
        }
        
        var dragVisualControl = new ContentControl();
        var payload = DragDropPayloadManager.GeneratePayload(null);

        payload.SetData("DraggedVariable", variable);
        payload.SetData("DraggedProtocolVariable", protocolVariable);
        payload.SetData("DraggedVariableDragVisual", dragVisualControl);
        
        e.Data = payload;

        var bgColor = ShellWindow.MainAppSettings.Design.AppTheme == ApplicationTheme.Dark
            ? ShellWindow.MainAppSettings.Design.DarkThemeControlBackgroundColor
            : ShellWindow.MainAppSettings.Design.LightThemeControlBackgroundColor;
        
        var fgColor = ShellWindow.MainAppSettings.Design.AppTheme == ApplicationTheme.Dark
            ? ShellWindow.MainAppSettings.Design.DarkThemeTextColor
            : ShellWindow.MainAppSettings.Design.LightThemeTextColor;

        var content = new TextBlock
        {
            FontSize = 14,
            Text = $"{variable.Label} ({variable.Id})",
            Margin = new Thickness(10),
            Background = new SolidColorBrush(bgColor),
            Foreground = new SolidColorBrush(fgColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        dragVisualControl.Content = content;
        
        e.DragVisual = dragVisualControl;
        e.DragVisualOffset = new Point(e.RelativeStartPoint.X, e.RelativeStartPoint.Y);
        e.Handled = true;
    }
    
    private void OnGiveFeedback(object sender, Telerik.Windows.DragDrop.GiveFeedbackEventArgs e)
    {
        e.SetCursor(IsOverValidTarget ? Cursors.Hand : Cursors.No);
        e.Handled = true;
    }
    
    private void OnDropComleted(object sender, DragDropCompletedEventArgs e)
    {
        try
        {
            var protVariable = (IProtocolVariable)DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedProtocolVariable");
            if (DragDropPayloadManager.GetDataFromObject(e.Data, "ChosenControl") is not IControlBase chosenControl)
            {
                return;
            }

            // Data notification
            protVariable?.SubscribeAsyncValueChanged(async _ =>
            {
                if (protVariable.Variable is ScalarVariable sv)
                {
                    //Console.WriteLine($"x- Variable {sv.Label} changed to {sv.Values}");
                    
//#error chosenControl can be null here, need to investigate
                    await chosenControl.UpdateVariableValueAsync(sv);
                }
            });

            e.Handled = true;
        }
        catch (NullReferenceException)
        {
            // ignore
        }
        
    }

    private static IProtocolVariable? FindProtocolVariable(IEnumerable<IViewableItem> items, IVariableBase variable)
    {
        return FindProtocolVariable(items, variable, false);
    }

    private static IProtocolVariable? FindProtocolVariable(
        IEnumerable<IViewableItem> items,
        IVariableBase variable,
        bool isNonLiveSourceDriverBranch)
    {
        foreach (var item in items)
        {
            var nextIsNonLiveSourceDriverBranch = isNonLiveSourceDriverBranch
                                                  || item is DriverSeWrapper { Driver: IProtocolVariableSinkDriver or IReplayDriver };

            if (!nextIsNonLiveSourceDriverBranch
                && item is ProtocolVariableWrapper protocolVariableWrapper
                && IsSameVariable(protocolVariableWrapper.ProtocolVariable.Variable, variable))
            {
                return protocolVariableWrapper.ProtocolVariable;
            }

            var protocolVariable = FindProtocolVariable(item.Children, variable, nextIsNonLiveSourceDriverBranch);
            if (protocolVariable != null)
            {
                return protocolVariable;
            }
        }

        return null;
    }

    private static bool IsNonLiveSourceProtocolVariable(IEnumerable<IViewableItem> items, IProtocolVariable protocolVariable)
    {
        return IsNonLiveSourceProtocolVariable(items, protocolVariable, false);
    }

    private static bool IsNonLiveSourceProtocolVariable(
        IEnumerable<IViewableItem> items,
        IProtocolVariable protocolVariable,
        bool isNonLiveSourceDriverBranch)
    {
        foreach (var item in items)
        {
            var nextIsNonLiveSourceDriverBranch = isNonLiveSourceDriverBranch
                                                  || item is DriverSeWrapper { Driver: IProtocolVariableSinkDriver or IReplayDriver };

            if (item is ProtocolVariableWrapper protocolVariableWrapper
                && ReferenceEquals(protocolVariableWrapper.ProtocolVariable, protocolVariable))
            {
                return nextIsNonLiveSourceDriverBranch;
            }

            if (IsNonLiveSourceProtocolVariable(item.Children, protocolVariable, nextIsNonLiveSourceDriverBranch))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameVariable(IVariableBase left, IVariableBase right)
    {
        return ReferenceEquals(left, right)
               || ControlBase.IsVariableReferenceMatch(ControlBase.GetVariableReference(left), right);
    }
    
}
