using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using WpfControl = System.Windows.Controls.Control;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;

namespace Lab08;

public partial class MainWindow
{
    private readonly List<DocumentData> documents = new();
    private readonly List<WpfButtonBase> routedCommandControls = new();
    private readonly List<WpfControl> documentControls = new();
    private readonly Brush[] textColors = { Brushes.Black, Brushes.DarkBlue, Brushes.DarkRed, Brushes.SeaGreen, Brushes.Purple };
    private FindReplaceWindow? findWindow;
    private UiLanguage language = UiLanguage.Ukrainian;
    private bool updatingSelection;
    private bool updatingLanguage;
    private bool internalChange;
    private int documentCounter;
    private int textColorIndex;

    public MainWindow()
    {
        InitializeComponent();
        InitializeControls();
        ApplyLanguage();
    }

    private DocumentData? CurrentDocument => DocumentsTab.SelectedItem is TabItem tab ? tab.Tag as DocumentData : null;
    private WpfRichTextBox? CurrentEditor => CurrentDocument?.Editor;

    private void InitializeControls()
    {
        routedCommandControls.AddRange(new WpfButtonBase[]
        {
            NewButton,
            OpenButton,
            SaveButton,
            UndoButton,
            RedoButton,
            BoldButton,
            ItalicButton,
            UnderlineButton
        });

        documentControls.AddRange(new WpfControl[]
        {
            SaveButton,
            SaveAsButton,
            UndoButton,
            RedoButton,
            BoldButton,
            ItalicButton,
            UnderlineButton,
            FontFamilyBox,
            FontSizeBox,
            ColorButton,
            LeftButton,
            CenterButton,
            RightButton,
            JustifyButton,
            ImageButton,
            FindButton
        });

        FontFamilyBox.ItemsSource = Fonts.SystemFontFamilies.OrderBy(font => font.Source);
        FontSizeBox.ItemsSource = new List<double> { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
    }

    private string T(string key)
    {
        return TextCatalog.T(language, key);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (documents.Count == 0)
        {
            CreateNewDocument();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var document in documents.ToList())
        {
            DocumentsTab.SelectedItem = document.Tab;

            if (!ConfirmSave(document))
            {
                e.Cancel = true;
                return;
            }
        }
    }

    private void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            ShowFindWindow();
            e.Handled = true;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.H)
        {
            ShowFindWindow();
            e.Handled = true;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.S)
        {
            SaveCurrentAs();
            e.Handled = true;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.I)
        {
            InsertImage();
            e.Handled = true;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            CloseCurrentDocument();
            e.Handled = true;
        }
    }

    private void New_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        CreateNewDocument();
    }

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        OpenDocuments();
    }

    private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SaveCurrent();
    }

    private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        PrintCurrent();
    }

    private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        CloseCurrentDocument();
    }

    private void NewWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = new MainWindow();
        window.Show();
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentAs();
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var document in documents.ToList())
        {
            DocumentsTab.SelectedItem = document.Tab;

            if (!SaveDocumentWithDialog(document, false))
            {
                break;
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Find_Click(object sender, RoutedEventArgs e)
    {
        ShowFindWindow();
    }

    private void Image_Click(object sender, RoutedEventArgs e)
    {
        InsertImage();
    }

    private void Date_Click(object sender, RoutedEventArgs e)
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return;
        }

        editor.Selection.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
        MarkCurrentModified();
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return;
        }

        var brush = textColors[textColorIndex % textColors.Length];
        textColorIndex++;
        editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
        MarkCurrentModified();
    }

    private void Left_Click(object sender, RoutedEventArgs e)
    {
        ApplyAlignment(TextAlignment.Left);
    }

    private void Center_Click(object sender, RoutedEventArgs e)
    {
        ApplyAlignment(TextAlignment.Center);
    }

    private void Right_Click(object sender, RoutedEventArgs e)
    {
        ApplyAlignment(TextAlignment.Right);
    }

    private void Justify_Click(object sender, RoutedEventArgs e)
    {
        ApplyAlignment(TextAlignment.Justify);
    }

    private void Ukrainian_Click(object sender, RoutedEventArgs e)
    {
        ChangeLanguage(UiLanguage.Ukrainian);
    }

    private void English_Click(object sender, RoutedEventArgs e)
    {
        ChangeLanguage(UiLanguage.English);
    }

    private void Polish_Click(object sender, RoutedEventArgs e)
    {
        ChangeLanguage(UiLanguage.Polish);
    }

    private void PreviousDocument_Click(object sender, RoutedEventArgs e)
    {
        MoveTab(-1);
    }

    private void NextDocument_Click(object sender, RoutedEventArgs e)
    {
        MoveTab(1);
    }

    private void DocumentsTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != DocumentsTab)
        {
            return;
        }

        UpdateCommandTargets();
        UpdateSelectionState();
        UpdateStatus();
    }

    private void FontFamilyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingSelection || FontFamilyBox.SelectedItem is not FontFamily family)
        {
            return;
        }

        CurrentEditor?.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, family);
        MarkCurrentModified();
    }

    private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFontSizeFromBox();
    }

    private void FontSizeBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ApplyFontSizeFromBox();
    }

    private void FontSizeBox_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFontSizeFromBox();
            CurrentEditor?.Focus();
            e.Handled = true;
        }
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingLanguage)
        {
            return;
        }

        ChangeLanguage(LanguageBox.SelectedIndex switch
        {
            1 => UiLanguage.English,
            2 => UiLanguage.Polish,
            _ => UiLanguage.Ukrainian
        });
    }

    private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateSelectionState();
        UpdateStatus();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (internalChange || sender is not WpfRichTextBox editor || editor.Tag is not DocumentData document)
        {
            return;
        }

        document.IsModified = true;
        UpdateTabHeader(document);
        UpdateStatus();
    }

    private void CreateNewDocument()
    {
        var document = CreateDocument();
        DocumentsTab.Items.Add(document.Tab);
        DocumentsTab.SelectedItem = document.Tab;
        UpdateTabHeader(document);
        document.Editor.Focus();
        UpdateState();
    }

    private DocumentData CreateDocument()
    {
        documentCounter++;

        var editor = new WpfRichTextBox
        {
            AcceptsTab = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            Padding = new Thickness(16),
            Background = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        editor.Document = new FlowDocument(new Paragraph());
        SpellCheck.SetIsEnabled(editor, true);

        var tab = new TabItem
        {
            Content = editor
        };

        var document = new DocumentData(documentCounter, editor, tab);
        editor.Tag = document;
        tab.Tag = document;
        documents.Add(document);
        editor.SelectionChanged += Editor_SelectionChanged;
        editor.TextChanged += Editor_TextChanged;
        return document;
    }

    private void OpenDocuments()
    {
        var dialog = new OpenFileDialog
        {
            Filter = T("openFilter"),
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        foreach (var path in dialog.FileNames)
        {
            var document = CreateDocument();
            DocumentsTab.Items.Add(document.Tab);
            DocumentsTab.SelectedItem = document.Tab;

            try
            {
                LoadDocument(document, path);
                document.Editor.Focus();
            }
            catch
            {
                RemoveDocument(document);
                MessageBox.Show(this, T("openError"), T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        UpdateState();
    }

    private void LoadDocument(DocumentData document, string path)
    {
        internalChange = true;

        try
        {
            var format = FormatFromPath(path);
            var range = new TextRange(document.Editor.Document.ContentStart, document.Editor.Document.ContentEnd);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            range.Load(stream, format);
            document.FilePath = path;
            document.Format = format;
            document.IsModified = false;
            UpdateTabHeader(document);
        }
        finally
        {
            internalChange = false;
        }
    }

    private bool SaveCurrent()
    {
        var document = CurrentDocument;

        if (document is null)
        {
            return false;
        }

        if (document.FilePath is null)
        {
            return SaveDocumentWithDialog(document, false);
        }

        try
        {
            SaveDocument(document, document.FilePath);
            return true;
        }
        catch
        {
            MessageBox.Show(this, T("saveError"), T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool SaveCurrentAs()
    {
        return CurrentDocument is not null && SaveDocumentWithDialog(CurrentDocument, true);
    }

    private bool SaveDocumentWithDialog(DocumentData document, bool forceDialog)
    {
        if (!forceDialog && document.FilePath is not null)
        {
            try
            {
                SaveDocument(document, document.FilePath);
                return true;
            }
            catch
            {
                MessageBox.Show(this, T("saveError"), T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        var dialog = new SaveFileDialog
        {
            Filter = T("saveFilter"),
            DefaultExt = "xamlpkg",
            AddExtension = true,
            FilterIndex = document.Format == DataFormats.Rtf ? 2 : document.Format == DataFormats.Text ? 3 : 1
        };

        if (document.FilePath is not null)
        {
            dialog.FileName = Path.GetFileNameWithoutExtension(document.FilePath);
        }

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        try
        {
            SaveDocument(document, dialog.FileName);
            return true;
        }
        catch
        {
            MessageBox.Show(this, T("saveError"), T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void SaveDocument(DocumentData document, string path)
    {
        var format = FormatFromPath(path);
        var range = new TextRange(document.Editor.Document.ContentStart, document.Editor.Document.ContentEnd);

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        range.Save(stream, format);
        document.FilePath = path;
        document.Format = format;
        document.IsModified = false;
        UpdateTabHeader(document);
        UpdateStatus();
    }

    private void PrintCurrent()
    {
        var document = CurrentDocument;

        if (document is null)
        {
            return;
        }

        try
        {
            var dialog = new PrintDialog();

            if (dialog.ShowDialog() == true)
            {
                dialog.PrintDocument(((IDocumentPaginatorSource)document.Editor.Document).DocumentPaginator, document.DisplayName(T("untitled"), T("modified")));
            }
        }
        catch
        {
            MessageBox.Show(this, T("printError"), T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseCurrentDocument()
    {
        var document = CurrentDocument;

        if (document is null || !ConfirmSave(document))
        {
            return;
        }

        RemoveDocument(document);
        UpdateState();
    }

    private void RemoveDocument(DocumentData document)
    {
        documents.Remove(document);
        DocumentsTab.Items.Remove(document.Tab);
    }

    private bool ConfirmSave(DocumentData document)
    {
        if (!document.IsModified)
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            string.Format(T("confirmSave"), document.DisplayName(T("untitled"), T("modified"))),
            T("confirmSaveTitle"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        return result != MessageBoxResult.Yes || SaveDocumentWithDialog(document, false);
    }

    private void ApplyFontSizeFromBox()
    {
        if (updatingSelection || CurrentEditor is null)
        {
            return;
        }

        if (TryParseSize(FontSizeBox.Text, out var size))
        {
            CurrentEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            MarkCurrentModified();
        }
    }

    private static bool TryParseSize(string text, out double size)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out size)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out size);
    }

    private void ApplyAlignment(TextAlignment alignment)
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return;
        }

        editor.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, alignment);
        MarkCurrentModified();
    }

    private void InsertImage()
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = T("imageFilter")
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var image = new Image
            {
                Source = LoadBitmap(dialog.FileName),
                MaxWidth = 620,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2)
            };

            var container = new InlineUIContainer(image, editor.CaretPosition);
            editor.CaretPosition = container.ElementEnd;
            MarkCurrentModified();
        }
        catch
        {
            MessageBox.Show(this, T("imageError"), T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void ShowFindWindow()
    {
        if (CurrentEditor is null)
        {
            return;
        }

        if (findWindow is null)
        {
            findWindow = new FindReplaceWindow(this, language)
            {
                Owner = this
            };
            findWindow.Closed += (_, _) => findWindow = null;
        }

        findWindow.ApplyLanguage(language);
        findWindow.Show();
        findWindow.Activate();
    }

    public bool FindNext(string text, bool matchCase)
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return false;
        }

        var range = FindRange(editor, text, matchCase, editor.Selection.End);
        range ??= FindRange(editor, text, matchCase, editor.Document.ContentStart);

        if (range is null)
        {
            return false;
        }

        editor.Selection.Select(range.Start, range.End);
        editor.Focus();
        return true;
    }

    public bool ReplaceOne(string findText, string replaceText, bool matchCase)
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return false;
        }

        var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

        if (string.Equals(editor.Selection.Text, findText, comparison))
        {
            editor.Selection.Text = replaceText;
            MarkCurrentModified();
            return true;
        }

        if (!FindNext(findText, matchCase))
        {
            return false;
        }

        editor.Selection.Text = replaceText;
        MarkCurrentModified();
        return true;
    }

    public int ReplaceAll(string findText, string replaceText, bool matchCase)
    {
        var editor = CurrentEditor;

        if (editor is null)
        {
            return 0;
        }

        var count = 0;
        var pointer = editor.Document.ContentStart;
        internalChange = true;

        try
        {
            while (true)
            {
                var range = FindRange(editor, findText, matchCase, pointer);

                if (range is null)
                {
                    break;
                }

                range.Text = replaceText;
                pointer = range.End;
                count++;
            }
        }
        finally
        {
            internalChange = false;
        }

        if (count > 0)
        {
            MarkCurrentModified();
        }

        return count;
    }

    private static TextRange? FindRange(WpfRichTextBox editor, string text, bool matchCase, TextPointer start)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        var pointer = start;

        while (pointer is not null && pointer.CompareTo(editor.Document.ContentEnd) < 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var runText = pointer.GetTextInRun(LogicalDirection.Forward);
                var index = runText.IndexOf(text, comparison);

                if (index >= 0)
                {
                    var foundStart = pointer.GetPositionAtOffset(index);
                    var foundEnd = foundStart?.GetPositionAtOffset(text.Length);

                    if (foundStart is not null && foundEnd is not null)
                    {
                        return new TextRange(foundStart, foundEnd);
                    }
                }
            }

            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        }

        return null;
    }

    private void UpdateSelectionState()
    {
        var editor = CurrentEditor;
        updatingSelection = true;

        try
        {
            if (editor is null)
            {
                BoldButton.IsChecked = false;
                ItalicButton.IsChecked = false;
                UnderlineButton.IsChecked = false;
                return;
            }

            var value = editor.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            BoldButton.IsChecked = value != DependencyProperty.UnsetValue && value.Equals(FontWeights.Bold);
            value = editor.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            ItalicButton.IsChecked = value != DependencyProperty.UnsetValue && value.Equals(FontStyles.Italic);
            value = editor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            UnderlineButton.IsChecked = value != DependencyProperty.UnsetValue && value.Equals(TextDecorations.Underline);
            value = editor.Selection.GetPropertyValue(TextElement.FontFamilyProperty);

            if (value != DependencyProperty.UnsetValue)
            {
                FontFamilyBox.SelectedItem = value;
            }

            value = editor.Selection.GetPropertyValue(TextElement.FontSizeProperty);

            if (value != DependencyProperty.UnsetValue)
            {
                FontSizeBox.Text = Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.CurrentCulture);
            }
        }
        finally
        {
            updatingSelection = false;
        }
    }

    private void UpdateCommandTargets()
    {
        var editor = CurrentEditor;

        foreach (var control in routedCommandControls)
        {
            control.CommandTarget = editor;
        }

        SaveMenuItem.CommandTarget = editor;
        PrintMenuItem.CommandTarget = editor;
        CloseDocumentMenuItem.CommandTarget = editor;
        UndoMenuItem.CommandTarget = editor;
        RedoMenuItem.CommandTarget = editor;
        CutMenuItem.CommandTarget = editor;
        CopyMenuItem.CommandTarget = editor;
        PasteMenuItem.CommandTarget = editor;
        SelectAllMenuItem.CommandTarget = editor;
        BoldMenuItem.CommandTarget = editor;
        ItalicMenuItem.CommandTarget = editor;
        UnderlineMenuItem.CommandTarget = editor;
    }

    private void UpdateState()
    {
        UpdateCommandTargets();
        UpdateSelectionState();
        UpdateStatus();
        var hasDocument = CurrentDocument is not null;

        foreach (var control in documentControls)
        {
            control.IsEnabled = hasDocument;
        }

        SaveAsMenuItem.IsEnabled = hasDocument;
        SaveAllMenuItem.IsEnabled = hasDocument;
        CloseDocumentMenuItem.IsEnabled = hasDocument;
        PrintMenuItem.IsEnabled = hasDocument;
        FindMenuItem.IsEnabled = hasDocument;
        ReplaceMenuItem.IsEnabled = hasDocument;
        FormatMenu.IsEnabled = hasDocument;
        InsertMenu.IsEnabled = hasDocument;
        PreviousDocumentMenuItem.IsEnabled = documents.Count > 1;
        NextDocumentMenuItem.IsEnabled = documents.Count > 1;
    }

    private void UpdateStatus()
    {
        var document = CurrentDocument;

        if (document is null)
        {
            StatusText.Text = T("statusNoDocument");
            PositionText.Text = string.Empty;
            InfoText.Text = string.Empty;
            return;
        }

        StatusText.Text = document.FilePath ?? document.DisplayName(T("untitled"), T("modified"));
        var position = GetPosition(document.Editor);
        PositionText.Text = string.Format(T("lineColumn"), position.line, position.column);
        InfoText.Text = string.Format(T("formatInfo"), new TextRange(document.Editor.Document.ContentStart, document.Editor.Document.ContentEnd).Text.Length);
    }

    private static (int line, int column) GetPosition(WpfRichTextBox editor)
    {
        var text = new TextRange(editor.Document.ContentStart, editor.CaretPosition).Text.Replace("\r\n", "\n");
        var line = text.Count(character => character == '\n') + 1;
        var lastLine = text.LastIndexOf('\n');
        var column = lastLine < 0 ? text.Length + 1 : text.Length - lastLine;
        return (line, column);
    }

    private void UpdateTabHeader(DocumentData document)
    {
        document.Tab.Header = document.DisplayName(T("untitled"), T("modified"));
    }

    private void MarkCurrentModified()
    {
        var document = CurrentDocument;

        if (document is null)
        {
            return;
        }

        document.IsModified = true;
        UpdateTabHeader(document);
        UpdateStatus();
    }

    private void ApplyLanguage()
    {
        Title = T("appTitle");
        FileMenu.Header = T("file");
        NewMenuItem.Header = T("new");
        NewWindowMenuItem.Header = T("newWindow");
        OpenMenuItem.Header = T("open");
        SaveMenuItem.Header = T("save");
        SaveAsMenuItem.Header = T("saveAs");
        SaveAllMenuItem.Header = T("saveAll");
        PrintMenuItem.Header = T("print");
        CloseDocumentMenuItem.Header = T("closeDocument");
        ExitMenuItem.Header = T("exit");
        EditMenu.Header = T("edit");
        UndoMenuItem.Header = T("undo");
        RedoMenuItem.Header = T("redo");
        CutMenuItem.Header = T("cut");
        CopyMenuItem.Header = T("copy");
        PasteMenuItem.Header = T("paste");
        SelectAllMenuItem.Header = T("selectAll");
        FindMenuItem.Header = T("find");
        ReplaceMenuItem.Header = T("replace");
        FormatMenu.Header = T("format");
        BoldMenuItem.Header = T("bold");
        ItalicMenuItem.Header = T("italic");
        UnderlineMenuItem.Header = T("underline");
        ColorMenuItem.Header = T("color");
        AlignMenu.Header = T("align");
        LeftMenuItem.Header = T("left");
        CenterMenuItem.Header = T("center");
        RightMenuItem.Header = T("right");
        JustifyMenuItem.Header = T("justify");
        InsertMenu.Header = T("insert");
        ImageMenuItem.Header = T("image");
        DateMenuItem.Header = T("date");
        LanguageMenu.Header = T("language");
        UkrainianMenuItem.Header = T("ukrainian");
        EnglishMenuItem.Header = T("english");
        PolishMenuItem.Header = T("polish");
        WindowMenu.Header = T("window");
        PreviousDocumentMenuItem.Header = T("previousDocument");
        NextDocumentMenuItem.Header = T("nextDocument");
        NewButton.Content = T("new");
        OpenButton.Content = T("open");
        SaveButton.Content = T("save");
        SaveAsButton.Content = T("saveAs");
        UndoButton.Content = T("undo");
        RedoButton.Content = T("redo");
        BoldButton.Content = "B";
        ItalicButton.Content = "I";
        UnderlineButton.Content = "U";
        FontLabel.Text = T("font");
        SizeLabel.Text = T("size");
        ColorButton.Content = T("color");
        LeftButton.Content = T("left");
        CenterButton.Content = T("center");
        RightButton.Content = T("right");
        JustifyButton.Content = T("justify");
        ImageButton.Content = T("image");
        FindButton.Content = T("find");
        UkrainianMenuItem.IsChecked = language == UiLanguage.Ukrainian;
        EnglishMenuItem.IsChecked = language == UiLanguage.English;
        PolishMenuItem.IsChecked = language == UiLanguage.Polish;
        UpdateLanguageBox();

        foreach (var document in documents)
        {
            UpdateTabHeader(document);
        }

        findWindow?.ApplyLanguage(language);
        UpdateState();
    }

    private void UpdateLanguageBox()
    {
        updatingLanguage = true;
        LanguageBox.Items.Clear();
        LanguageBox.Items.Add(T("ukrainian"));
        LanguageBox.Items.Add(T("english"));
        LanguageBox.Items.Add(T("polish"));
        LanguageBox.SelectedIndex = language switch
        {
            UiLanguage.English => 1,
            UiLanguage.Polish => 2,
            _ => 0
        };
        updatingLanguage = false;
    }

    private void ChangeLanguage(UiLanguage nextLanguage)
    {
        language = nextLanguage;
        ApplyLanguage();
    }

    private void MoveTab(int direction)
    {
        if (DocumentsTab.Items.Count == 0)
        {
            return;
        }

        var next = DocumentsTab.SelectedIndex + direction;

        if (next < 0)
        {
            next = DocumentsTab.Items.Count - 1;
        }

        if (next >= DocumentsTab.Items.Count)
        {
            next = 0;
        }

        DocumentsTab.SelectedIndex = next;
    }

    private static string FormatFromPath(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".rtf" => DataFormats.Rtf,
            ".txt" => DataFormats.Text,
            _ => DataFormats.XamlPackage
        };
    }
}
