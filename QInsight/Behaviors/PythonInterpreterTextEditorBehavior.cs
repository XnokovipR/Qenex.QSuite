using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using Qenex.QInsight.ViewModels;

namespace Qenex.QInsight.Behaviors;

public class PythonInterpreterTextEditorBehavior : Behavior<TextEditor>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        AssociatedObject.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        AssociatedObject.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
        base.OnDetaching();
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (AssociatedObject.DataContext is not PythonInterpreterViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await viewModel.SubmitInputAsync();
            AssociatedObject.CaretOffset = AssociatedObject.Document.TextLength;
            AssociatedObject.ScrollToEnd();
            return;
        }

        if (e.Key == Key.Up)
        {
            e.Handled = true;
            viewModel.ShowPreviousHistoryInput();
            AssociatedObject.CaretOffset = AssociatedObject.Document.TextLength;
            return;
        }

        if (e.Key == Key.Down)
        {
            e.Handled = true;
            viewModel.ShowNextHistoryInput();
            AssociatedObject.CaretOffset = AssociatedObject.Document.TextLength;
            return;
        }

        if ((e.Key == Key.Back || e.Key == Key.Left) && AssociatedObject.CaretOffset <= viewModel.InputStartOffset)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Home)
        {
            e.Handled = true;
            AssociatedObject.CaretOffset = viewModel.InputStartOffset;
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (AssociatedObject.DataContext is not PythonInterpreterViewModel viewModel)
        {
            return;
        }

        if (AssociatedObject.CaretOffset < viewModel.InputStartOffset)
        {
            AssociatedObject.CaretOffset = viewModel.InputStartOffset;
        }
    }
}
