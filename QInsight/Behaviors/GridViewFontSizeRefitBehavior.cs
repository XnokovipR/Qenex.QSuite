using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.Behaviors;

/// <summary>
/// Sizes RadGridView columns to a sample text measured with the current theme font, and
/// re-applies the width whenever the theme font size changes. Used instead of the grid's
/// automatic sizing because automatic widths only ever grow — after a font decrease the
/// columns keep the widest layout ever measured and never shrink back.
/// </summary>
public static class GridViewFontSizeRefitBehavior
{
    // Cell chrome around the text (padding + border) that the measurement must add.
    private const double CellChromeWidth = 18;

    private static readonly DependencyPropertyDescriptor FontSizeDescriptor =
        DependencyPropertyDescriptor.FromProperty(Windows11Palette.FontSizeProperty, typeof(Windows11Palette));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(GridViewFontSizeRefitBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>Sample text whose rendered width (plus cell chrome) becomes the column width.</summary>
    public static readonly DependencyProperty WidthSampleTextProperty =
        DependencyProperty.RegisterAttached(
            "WidthSampleText",
            typeof(string),
            typeof(GridViewFontSizeRefitBehavior),
            new PropertyMetadata(null));

    // Keeps the palette subscription per grid so Unloaded can release exactly this handler.
    private static readonly DependencyProperty RefitHandlerProperty =
        DependencyProperty.RegisterAttached(
            "RefitHandler",
            typeof(EventHandler),
            typeof(GridViewFontSizeRefitBehavior),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    public static string? GetWidthSampleText(DependencyObject obj)
    {
        return (string?)obj.GetValue(WidthSampleTextProperty);
    }

    public static void SetWidthSampleText(DependencyObject obj, string? value)
    {
        obj.SetValue(WidthSampleTextProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RadGridView gridView)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            gridView.Loaded += OnGridViewLoaded;
            gridView.Unloaded += OnGridViewUnloaded;
        }
        else
        {
            gridView.Loaded -= OnGridViewLoaded;
            gridView.Unloaded -= OnGridViewUnloaded;
            ReleaseSubscription(gridView);
        }
    }

    private static void OnGridViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not RadGridView gridView)
        {
            return;
        }

        ApplySampleTextWidths(gridView);

        if (gridView.GetValue(RefitHandlerProperty) != null)
        {
            return;
        }

        EventHandler handler = (_, _) => ApplySampleTextWidths(gridView);
        gridView.SetValue(RefitHandlerProperty, handler);
        FontSizeDescriptor.AddValueChanged(Windows11Palette.Palette, handler);
    }

    private static void OnGridViewUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is RadGridView gridView)
        {
            ReleaseSubscription(gridView);
        }
    }

    private static void ReleaseSubscription(RadGridView gridView)
    {
        if (gridView.GetValue(RefitHandlerProperty) is EventHandler handler)
        {
            FontSizeDescriptor.RemoveValueChanged(Windows11Palette.Palette, handler);
            gridView.SetValue(RefitHandlerProperty, null);
        }
    }

    private static void ApplySampleTextWidths(RadGridView gridView)
    {
        var typeface = new Typeface(gridView.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var fontSize = Windows11Palette.Palette.FontSize;
        var pixelsPerDip = VisualTreeHelper.GetDpi(gridView).PixelsPerDip;

        foreach (var column in gridView.Columns)
        {
            var sampleText = GetWidthSampleText(column);
            if (string.IsNullOrEmpty(sampleText))
            {
                continue;
            }

            var formattedText = new FormattedText(
                sampleText,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                pixelsPerDip);

            column.Width = new GridViewLength(formattedText.WidthIncludingTrailingWhitespace + CellChromeWidth);
        }
    }
}
