using System.IO;
using System.Text;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Models;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QInsight.ViewModels;

public class PythonInterpreterViewModel : WorkspaceViewModelBase
{
    private const string PrimaryPrompt = ">>> ";
    private const string ContinuationPrompt = "... ";
    private const int MaxHistoryCount = 50;
    private static readonly string HistoryFilePath = AppDataPaths.PythonHistoryFile;
    private readonly Func<Task<ScriptingContext?>> scriptingContextProvider;
    private readonly List<string> inputHistory = [];
    private string currentPrompt = PrimaryPrompt;
    private int historyIndex = -1;

    public PythonInterpreterViewModel(
        EventAggregator ea,
        Func<Task<ScriptingContext?>> scriptingContextProvider) : base(ea)
    {
        this.scriptingContextProvider = scriptingContextProvider;

        ForegroundColor = ShellWindow.ForegroundColor;
        BackgroundColor = ShellWindow.TextEditorBackgroubndColor;
        FontSize = ShellWindow.MainAppSettings.Design.FontSize + 1;
        PyHighlighting = SyntaxHighlighting.LoadPythonHighlighting(ShellWindow.IsDarkTheme);
        Document = new TextDocument();
        LoadInputHistory();

        ClearWindowCommand = new RelayCommand<object>(_ => ClearWindow());
        ResetWindow();
    }

    public IHighlightingDefinition PyHighlighting { get; }
    public Color ForegroundColor { get; }
    public Color BackgroundColor { get; }
    public int FontSize { get; }
    public TextDocument Document { get; }
    public RelayCommand<object> ClearWindowCommand { get; }

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
    public override string WinTitle { get => field; set { field = value; OnPropertyChanged(); } } = "Python Interpreter";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;

    public async Task SubmitInputAsync()
    {
        if (IsExecuting)
        {
            return;
        }

        var input = GetCurrentInput();
        AddToHistory(input);
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

    public void ShowPreviousHistoryInput()
    {
        if (inputHistory.Count == 0)
        {
            return;
        }

        historyIndex = historyIndex < 0
            ? inputHistory.Count - 1
            : Math.Max(0, historyIndex - 1);
        ReplaceCurrentInput(inputHistory[historyIndex]);
    }

    public void ShowNextHistoryInput()
    {
        if (inputHistory.Count == 0 || historyIndex < 0)
        {
            return;
        }

        historyIndex++;
        if (historyIndex >= inputHistory.Count)
        {
            historyIndex = -1;
            ReplaceCurrentInput(string.Empty);
            return;
        }

        ReplaceCurrentInput(inputHistory[historyIndex]);
    }

    private void AddToHistory(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            historyIndex = -1;
            return;
        }

        if (inputHistory.Count == 0 || inputHistory[^1] != input)
        {
            inputHistory.Add(input);
            TrimHistory();
            SaveInputHistory();
        }

        historyIndex = -1;
    }

    private void ReplaceCurrentInput(string input)
    {
        var inputLength = Document.TextLength - InputStartOffset;
        if (inputLength > 0)
        {
            Document.Remove(InputStartOffset, inputLength);
        }

        Document.Insert(InputStartOffset, input);
    }

    private void LoadInputHistory()
    {
        if (!File.Exists(HistoryFilePath))
        {
            return;
        }

        try
        {
            foreach (var line in File.ReadLines(HistoryFilePath))
            {
                var input = UnescapeHistoryLine(line);
                if (!string.IsNullOrWhiteSpace(input))
                {
                    inputHistory.Add(input);
                }
            }

            TrimHistory();
        }
        catch
        {
            inputHistory.Clear();
        }
    }

    private void SaveInputHistory()
    {
        try
        {
            File.WriteAllLines(HistoryFilePath, inputHistory.Select(EscapeHistoryLine));
        }
        catch
        {
            // History is a convenience feature; command execution must not depend on it.
        }
    }

    private void TrimHistory()
    {
        if (inputHistory.Count <= MaxHistoryCount)
        {
            return;
        }

        inputHistory.RemoveRange(0, inputHistory.Count - MaxHistoryCount);
    }

    private static string EscapeHistoryLine(string input)
    {
        return input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string UnescapeHistoryLine(string input)
    {
        var builder = new StringBuilder(input.Length);
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] != '\\' || i == input.Length - 1)
            {
                builder.Append(input[i]);
                continue;
            }

            i++;
            builder.Append(input[i] switch
            {
                'r' => '\r',
                'n' => '\n',
                '\\' => '\\',
                _ => input[i]
            });
        }

        return builder.ToString();
    }

    private void AppendResult(InteractivePythonExecutionResult result)
    {
        AppendPythonText(result.Output);
        AppendPythonText(result.Error);
    }

    public void ClearWindow()
    {
        currentPrompt = PrimaryPrompt;
        ResetWindow();
    }

    private void ResetWindow()
    {
        InputStartOffset = 0;
        Document.Remove(0, Document.TextLength);
        AppendSystemLine("QInsight Python");
        AppendPrompt();
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
