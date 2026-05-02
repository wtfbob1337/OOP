using System.ComponentModel;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Lab07;

internal sealed class EditorDocument : Form
{
    private readonly RichTextBox editor = new();
    private readonly System.Windows.Forms.Timer highlightTimer = new();
    private bool internalUpdate;
    private int documentNumber;
    private int printIndex;
    private string printText = string.Empty;
    private UiLanguage language;

    public event EventHandler? DocumentStateChanged;

    public EditorDocument(int number, UiLanguage currentLanguage)
    {
        documentNumber = number;
        language = currentLanguage;
        InitializeEditor();
        ApplyLanguage(currentLanguage);
    }

    public RichTextBox Editor => editor;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? FilePath { get; private set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsModified { get; private set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SyntaxEnabled { get; set; } = true;

    public void ApplyLanguage(UiLanguage currentLanguage)
    {
        language = currentLanguage;
        UpdateTitle();
    }

    public void NewDocument()
    {
        FilePath = null;
        editor.Clear();
        editor.Modified = false;
        IsModified = false;
        UpdateTitle();
    }

    public void LoadDocument(string path)
    {
        internalUpdate = true;
        try
        {
            if (IsRtf(path))
            {
                editor.LoadFile(path, RichTextBoxStreamType.RichText);
            }
            else
            {
                editor.Text = File.ReadAllText(path, Encoding.UTF8);
            }
        }
        finally
        {
            internalUpdate = false;
        }

        FilePath = path;
        editor.Modified = false;
        IsModified = false;
        UpdateTitle();
        ApplySyntaxHighlighting();
        IsModified = false;
        editor.Modified = false;
        DocumentStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        if (FilePath is null)
        {
            throw new InvalidOperationException();
        }

        SaveAs(FilePath);
    }

    public void SaveAs(string path)
    {
        if (IsRtf(path))
        {
            editor.SaveFile(path, RichTextBoxStreamType.RichText);
        }
        else
        {
            File.WriteAllText(path, editor.Text, Encoding.UTF8);
        }

        FilePath = path;
        editor.Modified = false;
        IsModified = false;
        UpdateTitle();
        DocumentStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool ConfirmClose()
    {
        if (!IsModified)
        {
            return true;
        }

        var name = FilePath is null ? TextCatalog.T(language, "untitled") : Path.GetFileName(FilePath);
        var result = MessageBox.Show(
            string.Format(TextCatalog.T(language, "confirmSave"), name),
            TextCatalog.T(language, "confirmSaveTitle"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Cancel)
        {
            return false;
        }

        if (result == DialogResult.No)
        {
            return true;
        }

        try
        {
            if (FilePath is null)
            {
                using var dialog = new SaveFileDialog
                {
                    Filter = TextCatalog.T(language, "saveFilter"),
                    FilterIndex = 1,
                    DefaultExt = "rtf",
                    AddExtension = true
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                SaveAs(dialog.FileName);
            }
            else
            {
                Save();
            }
        }
        catch
        {
            MessageBox.Show(TextCatalog.T(language, "saveError"), TextCatalog.T(language, "errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    public void InsertImage(string path)
    {
        IDataObject? previousData = null;

        try
        {
            if (Clipboard.ContainsData(DataFormats.Bitmap) || Clipboard.ContainsText() || Clipboard.ContainsImage())
            {
                previousData = Clipboard.GetDataObject();
            }

            using var source = Image.FromFile(path);
            using var copy = new Bitmap(source);
            Clipboard.SetImage(copy);
            editor.Paste();

            if (previousData is not null)
            {
                Clipboard.SetDataObject(previousData);
            }
        }
        catch (ExternalException)
        {
            if (previousData is not null)
            {
                Clipboard.SetDataObject(previousData);
            }

            throw;
        }
    }

    public bool FindText(string text, bool matchCase, bool wholeWord)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var options = RichTextBoxFinds.None;

        if (matchCase)
        {
            options |= RichTextBoxFinds.MatchCase;
        }

        if (wholeWord)
        {
            options |= RichTextBoxFinds.WholeWord;
        }

        var start = editor.SelectionStart + editor.SelectionLength;
        var index = editor.Find(text, start, options);

        if (index < 0 && start > 0)
        {
            index = editor.Find(text, 0, options);
        }

        if (index < 0)
        {
            return false;
        }

        editor.Focus();
        return true;
    }

    public bool ReplaceCurrent(string findText, string replaceText, bool matchCase, bool wholeWord)
    {
        if (string.IsNullOrEmpty(findText))
        {
            return false;
        }

        var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        var replaced = false;

        if (string.Equals(editor.SelectedText, findText, comparison))
        {
            if (!wholeWord || IsWholeSelection())
            {
                editor.SelectedText = replaceText;
                replaced = true;
            }
        }

        return FindText(findText, matchCase, wholeWord) || replaced;
    }

    public int ReplaceAll(string findText, string replaceText, bool matchCase, bool wholeWord)
    {
        if (string.IsNullOrEmpty(findText))
        {
            return 0;
        }

        var options = RichTextBoxFinds.None;

        if (matchCase)
        {
            options |= RichTextBoxFinds.MatchCase;
        }

        if (wholeWord)
        {
            options |= RichTextBoxFinds.WholeWord;
        }

        var count = 0;
        var start = 0;
        internalUpdate = true;

        try
        {
            while (start <= editor.TextLength)
            {
                var index = editor.Find(findText, start, options);

                if (index < 0)
                {
                    break;
                }

                editor.SelectedText = replaceText;
                count++;
                start = index + replaceText.Length;
            }
        }
        finally
        {
            internalUpdate = false;
        }

        if (count > 0)
        {
            IsModified = true;
            editor.Modified = true;
            UpdateTitle();
            ScheduleHighlighting();
        }

        return count;
    }

    public void ApplyFontToSelection()
    {
        using var dialog = new FontDialog
        {
            ShowColor = false,
            Font = editor.SelectionFont ?? editor.Font
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            editor.SelectionFont = dialog.Font;
        }
    }

    public void ApplyColorToSelection()
    {
        using var dialog = new ColorDialog
        {
            Color = editor.SelectionColor
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            editor.SelectionColor = dialog.Color;
        }
    }

    public void ToggleStyle(FontStyle style)
    {
        var baseFont = editor.SelectionFont ?? editor.Font;
        var nextStyle = baseFont.Style.HasFlag(style) ? baseFont.Style & ~style : baseFont.Style | style;
        editor.SelectionFont = new Font(baseFont, nextStyle);
    }

    public void SetAlignment(HorizontalAlignment alignment)
    {
        editor.SelectionAlignment = alignment;
    }

    public void ToggleBullets()
    {
        editor.SelectionBullet = !editor.SelectionBullet;
    }

    public void ZoomIn()
    {
        editor.ZoomFactor = Math.Min(5f, editor.ZoomFactor + 0.1f);
    }

    public void ZoomOut()
    {
        editor.ZoomFactor = Math.Max(0.2f, editor.ZoomFactor - 0.1f);
    }

    public void ZoomReset()
    {
        editor.ZoomFactor = 1f;
    }

    public (int line, int column) GetCaretPosition()
    {
        var index = editor.SelectionStart;
        var line = editor.GetLineFromCharIndex(index);
        var first = editor.GetFirstCharIndexFromLine(line);
        return (line + 1, index - first + 1);
    }

    public void PrintDocument(bool preview)
    {
        printText = editor.Text;
        printIndex = 0;

        using var document = new PrintDocument();
        document.DocumentName = FilePath is null ? TextCatalog.T(language, "untitled") : Path.GetFileName(FilePath);
        document.PrintPage += PrintPage;

        if (preview)
        {
            using var previewDialog = new PrintPreviewDialog
            {
                Document = document,
                Width = 1000,
                Height = 700
            };
            previewDialog.ShowDialog(this);
        }
        else
        {
            using var dialog = new PrintDialog
            {
                Document = document,
                UseEXDialog = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                document.Print();
            }
        }
    }

    public void ApplySyntaxHighlighting()
    {
        if (!SyntaxEnabled || editor.TextLength == 0 || editor.TextLength > 200000)
        {
            return;
        }

        var selectionStart = editor.SelectionStart;
        var selectionLength = editor.SelectionLength;
        var text = editor.Text;
        var previousModified = IsModified;
        var previousControlModified = editor.Modified;
        internalUpdate = true;

        try
        {
            editor.SuspendLayout();
            editor.SelectAll();
            editor.SelectionColor = Color.Black;
            Colorize(text, KeywordPattern(), Color.FromArgb(35, 80, 190), RegexOptions.None);
            Colorize(text, @"\b\d+(\.\d+)?\b", Color.FromArgb(0, 120, 120), RegexOptions.None);
            Colorize(text, @"^\s*#\s*\w+.*$", Color.FromArgb(120, 70, 140), RegexOptions.Multiline);
            Colorize(text, "\"(?:\\\\.|[^\"\\\\])*\"", Color.FromArgb(160, 80, 20), RegexOptions.None);
            Colorize(text, @"'(?:\\.|[^'\\])+'", Color.FromArgb(160, 80, 20), RegexOptions.None);
            Colorize(text, @"//.*?$", Color.FromArgb(0, 120, 70), RegexOptions.Multiline);
            Colorize(text, @"/\*.*?\*/", Color.FromArgb(0, 120, 70), RegexOptions.Singleline);
            editor.Select(Math.Min(selectionStart, editor.TextLength), Math.Min(selectionLength, Math.Max(0, editor.TextLength - selectionStart)));
        }
        finally
        {
            editor.ResumeLayout();
            internalUpdate = false;
            IsModified = previousModified;
            editor.Modified = previousControlModified;
            DocumentStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmClose())
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    private void InitializeEditor()
    {
        editor.Dock = DockStyle.Fill;
        editor.AcceptsTab = true;
        editor.DetectUrls = true;
        editor.HideSelection = false;
        editor.Font = new Font("Consolas", 11f);
        editor.BorderStyle = BorderStyle.None;
        editor.EnableAutoDragDrop = true;
        editor.TextChanged += EditorTextChanged;
        editor.SelectionChanged += (_, _) => DocumentStateChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(editor);
        Width = 800;
        Height = 600;

        highlightTimer.Interval = 450;
        highlightTimer.Tick += (_, _) =>
        {
            highlightTimer.Stop();
            ApplySyntaxHighlighting();
        };
    }

    private void EditorTextChanged(object? sender, EventArgs e)
    {
        if (internalUpdate)
        {
            return;
        }

        IsModified = true;
        UpdateTitle();
        ScheduleHighlighting();
        DocumentStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ScheduleHighlighting()
    {
        if (!SyntaxEnabled)
        {
            return;
        }

        highlightTimer.Stop();
        highlightTimer.Start();
    }

    private void UpdateTitle()
    {
        var name = FilePath is null ? $"{TextCatalog.T(language, "document")} {documentNumber}" : Path.GetFileName(FilePath);
        Text = IsModified ? $"{name} ({TextCatalog.T(language, "modified")})" : name;
    }

    private bool IsWholeSelection()
    {
        var start = editor.SelectionStart;
        var end = start + editor.SelectionLength;
        var text = editor.Text;
        var left = start == 0 || !char.IsLetterOrDigit(text[start - 1]) && text[start - 1] != '_';
        var right = end >= text.Length || !char.IsLetterOrDigit(text[end]) && text[end] != '_';
        return left && right;
    }

    private void Colorize(string text, string pattern, Color color, RegexOptions options)
    {
        foreach (Match match in Regex.Matches(text, pattern, options | RegexOptions.Compiled))
        {
            if (match.Length == 0)
            {
                continue;
            }

            editor.Select(match.Index, match.Length);
            editor.SelectionColor = color;
        }
    }

    private static string KeywordPattern()
    {
        var keywords = new[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "record", "ref", "return", "sbyte",
            "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "virtual", "void", "volatile", "while", "var", "let", "function",
            "import", "export", "package", "extends", "implements", "final"
        };

        return @"\b(" + string.Join("|", keywords.Select(Regex.Escape)) + @")\b";
    }

    private static bool IsRtf(string path)
    {
        return string.Equals(Path.GetExtension(path), ".rtf", StringComparison.OrdinalIgnoreCase);
    }

    private void PrintPage(object? sender, PrintPageEventArgs e)
    {
        var font = editor.Font;
        var bounds = e.MarginBounds;
        var charsOnPage = 0;
        var linesPerPage = 0;
        var graphics = e.Graphics ?? throw new InvalidOperationException();

        graphics.MeasureString(
            printText.AsSpan(printIndex).ToString(),
            font,
            bounds.Size,
            StringFormat.GenericTypographic,
            out charsOnPage,
            out linesPerPage);

        graphics.DrawString(
            printText.AsSpan(printIndex, charsOnPage).ToString(),
            font,
            Brushes.Black,
            bounds,
            StringFormat.GenericTypographic);

        printIndex += charsOnPage;
        e.HasMorePages = printIndex < printText.Length;
    }
}
