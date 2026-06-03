using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;

namespace Qenex.QInsight.Behaviors;

public static class ActiveDocumentPaneHighlightBehavior
{
    private const double ActivePaneBorderOpacity = 0.4;
    private const double ActivePaneBorderThickness = 1;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ActiveDocumentPaneHighlightBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is not RadDocking docking)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            docking.Loaded += OnDockingLoaded;
            docking.ActivePaneChanged += OnActivePaneChanged;
            return;
        }

        docking.Loaded -= OnDockingLoaded;
        docking.ActivePaneChanged -= OnActivePaneChanged;
        ClearDocumentGroupHighlight(docking);
    }

    private static void OnDockingLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not RadDocking docking)
        {
            return;
        }

        docking.Dispatcher.BeginInvoke(
            () => UpdateDocumentGroupHighlight(docking, docking.ActivePane),
            DispatcherPriority.Loaded);
    }

    private static void OnActivePaneChanged(object? sender, ActivePangeChangedEventArgs e)
    {
        if (sender is RadDocking docking)
        {
            UpdateDocumentGroupHighlight(docking, e.NewPane);
        }
    }

    private static void UpdateDocumentGroupHighlight(RadDocking docking, RadPane? activePane)
    {
        ClearDocumentGroupHighlight(docking);

        if (activePane is not RadDocumentPane)
        {
            return;
        }

        var activeGroup = FindParent<RadPaneGroup>(activePane);
        if (activeGroup == null)
        {
            return;
        }

        var adornerLayer = AdornerLayer.GetAdornerLayer(activeGroup);
        adornerLayer?.Add(new ActiveDocumentPaneAdorner(activeGroup, CreateActivePaneBorderBrush()));
    }

    private static void ClearDocumentGroupHighlight(RadDocking docking)
    {
        foreach (var group in FindVisualChildren<RadPaneGroup>(docking))
        {
            if (!ContainsDocumentPane(group))
            {
                continue;
            }

            group.ClearValue(RadPaneGroup.BorderBrushProperty);
            group.ClearValue(RadPaneGroup.BorderThicknessProperty);
            RemoveActivePaneAdorners(group);
        }
    }

    private static Brush CreateActivePaneBorderBrush()
    {
        return new SolidColorBrush(Windows11Palette.Palette.AccentColor)
        {
            Opacity = ActivePaneBorderOpacity
        };
    }

    private static void RemoveActivePaneAdorners(UIElement adornedElement)
    {
        var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
        var adorners = adornerLayer?.GetAdorners(adornedElement);
        if (adornerLayer == null || adorners == null)
        {
            return;
        }

        foreach (var adorner in adorners.OfType<ActiveDocumentPaneAdorner>())
        {
            adornerLayer.Remove(adorner);
        }
    }

    private static bool ContainsDocumentPane(RadPaneGroup group)
    {
        return group.Items.OfType<RadDocumentPane>().Any();
    }

    private static T? FindParent<T>(DependencyObject child)
        where T : DependencyObject
    {
        var parent = GetParent(child);
        while (parent != null)
        {
            if (parent is T typedParent)
            {
                return typedParent;
            }

            parent = GetParent(parent);
        }

        return null;
    }

    private static DependencyObject? GetParent(DependencyObject child)
    {
        if (child is Visual or Visual3D)
        {
            var visualParent = VisualTreeHelper.GetParent(child);
            if (visualParent != null)
            {
                return visualParent;
            }
        }

        return LogicalTreeHelper.GetParent(child);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class ActiveDocumentPaneAdorner(UIElement adornedElement, Brush borderBrush) : Adorner(adornedElement)
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            var halfThickness = ActivePaneBorderThickness / 2;
            var rectangle = new Rect(
                halfThickness,
                halfThickness,
                Math.Max(0, ActualWidth - ActivePaneBorderThickness),
                Math.Max(0, ActualHeight - ActivePaneBorderThickness));

            drawingContext.DrawRoundedRectangle(
                null,
                new Pen(borderBrush, ActivePaneBorderThickness),
                rectangle,
                6,
                6);
        }
    }
}
