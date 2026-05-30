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
        if (AssociatedObject == null)
        {
            return;
        }

        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        AssociatedObject.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            AssociatedObject.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
        }

        base.OnDetaching();
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var editor = AssociatedObject;
        if (editor?.Document == null || editor.DataContext is not PythonInterpreterViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await viewModel.SubmitInputAsync();
            if (editor.Document != null)
            {
                editor.CaretOffset = editor.Document.TextLength;
                editor.ScrollToEnd();
            }

            return;
        }

        if (e.Key == Key.Up)
        {
            e.Handled = true;
            viewModel.ShowPreviousHistoryInput();
            editor.CaretOffset = editor.Document.TextLength;
            return;
        }

        if (e.Key == Key.Down)
        {
            e.Handled = true;
            viewModel.ShowNextHistoryInput();
            editor.CaretOffset = editor.Document.TextLength;
            return;
        }

        if ((e.Key == Key.Back || e.Key == Key.Left) && editor.CaretOffset <= viewModel.InputStartOffset)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Home)
        {
            e.Handled = true;
            editor.CaretOffset = viewModel.InputStartOffset;
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        var editor = AssociatedObject;
        if (editor?.Document == null || editor.DataContext is not PythonInterpreterViewModel viewModel)
        {
            return;
        }

        var inputStartOffset = Math.Min(viewModel.InputStartOffset, editor.Document.TextLength);
        if (editor.CaretOffset < inputStartOffset)
        {
            editor.CaretOffset = inputStartOffset;
        }
    }
}
