using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.ViewModels;
using Qenex.QInsight.ViewModels.SolutionExplorerWrappers;
using Qenex.QInsight.Views;
using Qenex.QSuite.Controls.Control;
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
        if (((FrameworkElement)e.OriginalSource).DataContext is not VariableWrapper varWrapper) return;
        if (varWrapper.Variable is not IVariableBase variable) return;
        
        if (sender is not RadTreeView treeView) return;
        if (treeView.DataContext is not SolutionExplorerViewModel vm) return;

        var defaultEvent = vm.ProjectModules.First().Children.First(i => i.Label.Contains("Events")).Children.First();
        var defaultDriverProtocolVariables = vm.ProjectModules
            .First().Children
            .First(i => i.Label.Contains("Communicated Drivers")).Children
            .First(i => i.Label.Contains("Simulation Data Driver")).Children
            .First(i => i.Label.Contains("Communicated Protocols")).Children
            .First().Children
            .First(i => i.Label.Contains("Communicated Variables")).Children;
        
        var dragVisualControl = new ContentControl();
        var payload = DragDropPayloadManager.GeneratePayload(null);

        payload.SetData("DraggedVariable", variable);
        payload.SetData("DraggedVariableDragVisual", dragVisualControl);
        payload.SetData("DefaultEvent", defaultEvent);
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
            var draggedVariable = DragDropPayloadManager.GetDataFromObject(e.Data, "DraggedVariable");
            var defaultEvent = DragDropPayloadManager.GetDataFromObject(e.Data, "DefaultEvent");
            var defaultDriverProtocolVariables = DragDropPayloadManager.GetDataFromObject(e.Data, "DefaultDriverProtocolVariables"); 
            
            // Add variable to Communicated Drivers + Data notification
            
            e.Handled = true;
        }
        catch (NullReferenceException exception)
        {
            // ignore
        }
        
    }
    
}