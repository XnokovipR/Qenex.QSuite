using System.Text;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Qenex.QInsight.Models;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QInsight.ViewModels;

public class PythonInterpreterViewModel : WorkspaceViewModelBase
{
    private const string PrimaryPrompt = ">>> ";
    private const string ContinuationPrompt = "... ";
    private readonly Func<Task<ScriptingContext?>> scriptingContextProvider;
    private string currentPrompt = PrimaryPrompt;

    public PythonInterpreterViewModel(
        EventAggregator ea,
        Func<Task<ScriptingContext?>> scriptingContextProvider) : base(ea)
    {
        this.scriptingContextProvider = scriptingContextProvider;

        ForegroundColor = ShellWindow.ForegroundColor;
        BackgroundColor = ShellWindow.BackgroundColor;
        FontSize = ShellWindow.MainAppSettings.Design.FontSize + 1;
        PyHighlighting = SyntaxHighlighting.LoadPythonHighlighting(ShellWindow.IsDarkTheme);
        Document = new TextDocument();

        AppendSystemLine("QInsight Python");
        AppendPrompt();
    }

    public IHighlightingDefinition PyHighlighting { get; }
    public Color ForegroundColor { get; }
    public Color BackgroundColor { get; }
    public int FontSize { get; }
    public TextDocument Document { get; }

    public int InputStartOffset
    {
        get => field;
        private set { field = value; OnPropertyChanged(); }
    }

    public bool IsExecuting
    {
        get => field;
        private set { field = value; OnPropertyChanged(); }
    }

    public override string Header { get; set; } = "";
    public override string Name { get; set; } = $"WorkspaceViewModel_PythonInterpreter_{Guid.NewGuid().ToString().Replace("-", "_")}";
    public override string WinTitle { get => field; set { field = value; OnPropertyChanged(); } } = "Python";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;

    public async Task SubmitInputAsync()
    {
        if (IsExecuting)
        {
            return;
        }

        var input = GetCurrentInput();
        AppendText(Environment.NewLine);

        var scriptingContext = await scriptingContextProvider();
        if (scriptingContext?.SharedScope == null)
        {
            AppendSystemLine("Python shared scope is not initialized.");
            currentPrompt = PrimaryPrompt;
            AppendPrompt();
            return;
        }

        try
        {
            IsExecuting = true;
            var result = await scriptingContext.ExecuteInteractiveAsync(input);
            AppendResult(result);
            currentPrompt = result.IsIncomplete ? ContinuationPrompt : PrimaryPrompt;
        }
        catch (Exception e)
        {
            AppendSystemLine(e.Message);
            currentPrompt = PrimaryPrompt;
        }
        finally
        {
            IsExecuting = false;
            AppendPrompt();
        }
    }

    private string GetCurrentInput()
    {
        var inputLength = Document.TextLength - InputStartOffset;
        return inputLength <= 0
            ? string.Empty
            : Document.GetText(InputStartOffset, inputLength);
    }

    private void AppendResult(InteractivePythonExecutionResult result)
    {
        AppendPythonText(result.Output);
        AppendPythonText(result.Error);
    }

    private void AppendPythonText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        AppendText(NormalizeLineEndings(text));
        if (!Document.Text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            AppendText(Environment.NewLine);
        }
    }

    private void AppendSystemLine(string text)
    {
        AppendText($"{text}{Environment.NewLine}");
    }

    private void AppendPrompt()
    {
        AppendText(currentPrompt);
        InputStartOffset = Document.TextLength;
    }

    private void AppendText(string text)
    {
        Document.Insert(Document.TextLength, text);
    }

    private static string NormalizeLineEndings(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                continue;
            }

            if (text[i] == '\n')
            {
                builder.Append(Environment.NewLine);
                continue;
            }

            builder.Append(text[i]);
        }

        return builder.ToString();
    }
}
