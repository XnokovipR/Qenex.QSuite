using System.Collections.ObjectModel;
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
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;
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
        if (((FrameworkElement)e.OriginalSource).DataContext is not VariableSeWrapper varWrapper) return;
        if (varWrapper.Variable is not IVariableBase variable) return;
        
        if (sender is not RadTreeView treeView) return;
        if (treeView.DataContext is not SolutionExplorerViewModel vm) return;

        var defaultEvent = ((IVariableEventSeWrapper)vm.ProjectModules.First().Children.First(i => i.Label.Contains("Events")).Children.First()).VariableEvent;
        var defaultProtocol = vm.ProjectModules
            .First().Children
            .First(i => i.Label.Contains("Communicated Drivers")).Children
            .First(i => i.Label.Contains("Simulation Data Driver")).Children
            .First(i => i.Label.Contains("Communicated Protocols")).Children
            .First();
        var defaultDriverProtocolVariables = defaultProtocol.Children
            .First(i => i.Label.Contains("Communicated Variables")).Children; 
        
        var dragVisualControl = new ContentControl();
        var payload = DragDropPayloadManager.GeneratePayload(null);

        payload.SetData("DraggedVariable", variable);
        payload.SetData("DraggedVariableDragVisual", dragVisualControl);
        payload.SetData("DefaultEvent", defaultEvent);
        payload.SetData("DefaultProtocol", defaultProtocol);
        payload.SetData("DefaultDriverProtocolVariables", defaultDriverProtocolVariables);
        
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
            var draggedVariable = (IVariableBase)DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedVariable");
            var defaultEvent = (IVarEvent)DragDropPayloadManager.GetDataFromObject(e.Data, "DefaultEvent");
            var defaultProtocol = (ProtocolSeWrapper)DragDropPayloadManager.GetDataFromObject(e.Data, "DefaultProtocol");
            var defaultDriverProtocolVariables = (ObservableCollection<IViewableItem>)DragDropPayloadManager.GetDataFromObject(e.Data, "DefaultDriverProtocolVariables"); 
            if (DragDropPayloadManager.GetDataFromObject(e.Data, "ChosenControl") is not IControlBase chosenControl)
            {
                return;
            }
            
            // Add variable to Communicated Drivers
            IProtocolVariable? protVariable = null;
            var alreadyAdded = defaultProtocol.Protocol.Variables.Any(v => v.Variable.Name == draggedVariable.Name);
            if (!alreadyAdded)
            {
                protVariable = defaultProtocol.Protocol.CreateProtocolVariable(draggedVariable, defaultEvent, draggedVariable.Name);
                defaultProtocol.Protocol.AddVariable(protVariable);
                var protVarWrapper = new ProtocolVariableWrapper(protVariable);
                defaultDriverProtocolVariables.Add(protVarWrapper);
            }
            else
            {
                var protVarWrapper = defaultDriverProtocolVariables.Cast<ProtocolVariableWrapper>().First(v => v.ProtocolVariable.Variable.Label.Equals(draggedVariable.Label));
                if (protVarWrapper is ProtocolVariableWrapper existingProtVarWrapper)
                {
                    protVariable = existingProtVarWrapper.ProtocolVariable;
                }
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
        catch (NullReferenceException exception)
        {
            // ignore
        }
        
    }
    
}
