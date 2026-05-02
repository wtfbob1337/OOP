namespace Lab07;

internal sealed class MainForm : Form
{
    private readonly MenuStrip menuStrip = new();
    private readonly ToolStrip toolStrip = new();
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new();
    private readonly ToolStripStatusLabel positionLabel = new();
    private readonly ToolStripStatusLabel infoLabel = new();
    private readonly Dictionary<ToolStripItem, string> itemKeys = new();
    private readonly Dictionary<ToolStripButton, string> buttonKeys = new();
    private readonly ToolStripMenuItem languageMenu = new();
    private readonly ToolStripMenuItem ukrainianItem = new();
    private readonly ToolStripMenuItem englishItem = new();
    private readonly ToolStripMenuItem polishItem = new();
    private readonly ToolStripMenuItem syntaxItem = new();
    private readonly ToolStripMenuItem wordWrapItem = new();
    private FindReplaceForm? findForm;
    private UiLanguage language = UiLanguage.Ukrainian;
    private int documentCounter;

    public MainForm()
    {
        IsMdiContainer = true;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(980, 640);
        InitializeMenu();
        InitializeToolbar();
        InitializeStatus();
        ApplyLanguage();
        MdiChildActivate += (_, _) => UpdateState();
    }

    public EditorDocument? ActiveDocument => ActiveMdiChild as EditorDocument;

    private string T(string key)
    {
        return TextCatalog.T(language, key);
    }

    private void InitializeMenu()
    {
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);

        var fileMenu = Menu("file");
        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            Item("new", (_, _) => CreateDocument(), Keys.Control | Keys.N),
            Item("open", (_, _) => OpenDocument(), Keys.Control | Keys.O),
            new ToolStripSeparator(),
            Item("save", (_, _) => SaveActiveDocument(), Keys.Control | Keys.S),
            Item("saveAs", (_, _) => SaveActiveDocumentAs(), Keys.Control | Keys.Shift | Keys.S),
            Item("saveAll", (_, _) => SaveAllDocuments()),
            new ToolStripSeparator(),
            Item("print", (_, _) => PrintActiveDocument(false), Keys.Control | Keys.P),
            Item("printPreview", (_, _) => PrintActiveDocument(true)),
            new ToolStripSeparator(),
            Item("close", (_, _) => ActiveDocument?.Close(), Keys.Control | Keys.W),
            Item("exit", (_, _) => Close(), Keys.Alt | Keys.F4)
        });

        var editMenu = Menu("edit");
        editMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            Item("undo", (_, _) => ActiveDocument?.Editor.Undo(), Keys.Control | Keys.Z),
            Item("redo", (_, _) => ActiveDocument?.Editor.Redo(), Keys.Control | Keys.Y),
            new ToolStripSeparator(),
            Item("cut", (_, _) => ActiveDocument?.Editor.Cut(), Keys.Control | Keys.X),
            Item("copy", (_, _) => ActiveDocument?.Editor.Copy(), Keys.Control | Keys.C),
            Item("paste", (_, _) => ActiveDocument?.Editor.Paste(), Keys.Control | Keys.V),
            Item("selectAll", (_, _) => ActiveDocument?.Editor.SelectAll(), Keys.Control | Keys.A),
            new ToolStripSeparator(),
            Item("find", (_, _) => ShowFindDialog(), Keys.Control | Keys.F),
            Item("replace", (_, _) => ShowFindDialog(), Keys.Control | Keys.H)
        });

        var alignMenu = Menu("align");
        alignMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            Item("left", (_, _) => ActiveDocument?.SetAlignment(HorizontalAlignment.Left), Keys.Control | Keys.L),
            Item("center", (_, _) => ActiveDocument?.SetAlignment(HorizontalAlignment.Center), Keys.Control | Keys.E),
            Item("right", (_, _) => ActiveDocument?.SetAlignment(HorizontalAlignment.Right), Keys.Control | Keys.R)
        });

        var formatMenu = Menu("format");
        wordWrapItem.CheckOnClick = true;
        wordWrapItem.Checked = true;
        wordWrapItem.Click += (_, _) => SetWordWrap(wordWrapItem.Checked);
        Register(wordWrapItem, "wordWrap");
        formatMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            Item("font", (_, _) => ActiveDocument?.ApplyFontToSelection(), Keys.Control | Keys.Shift | Keys.F),
            Item("color", (_, _) => ActiveDocument?.ApplyColorToSelection()),
            new ToolStripSeparator(),
            Item("bold", (_, _) => ActiveDocument?.ToggleStyle(FontStyle.Bold), Keys.Control | Keys.B),
            Item("italic", (_, _) => ActiveDocument?.ToggleStyle(FontStyle.Italic), Keys.Control | Keys.I),
            Item("underline", (_, _) => ActiveDocument?.ToggleStyle(FontStyle.Underline), Keys.Control | Keys.U),
            new ToolStripSeparator(),
            alignMenu,
            Item("bullets", (_, _) => ActiveDocument?.ToggleBullets()),
            wordWrapItem
        });

        var insertMenu = Menu("insert");
        insertMenu.DropDownItems.Add(Item("image", (_, _) => InsertImage(), Keys.Control | Keys.Shift | Keys.I));

        var toolsMenu = Menu("tools");
        syntaxItem.CheckOnClick = true;
        syntaxItem.Checked = true;
        syntaxItem.Click += (_, _) => ToggleSyntax();
        Register(syntaxItem, "syntax");
        toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            syntaxItem,
            new ToolStripSeparator(),
            Item("zoomIn", (_, _) => ActiveDocument?.ZoomIn(), Keys.Control | Keys.Oemplus),
            Item("zoomOut", (_, _) => ActiveDocument?.ZoomOut(), Keys.Control | Keys.OemMinus),
            Item("zoomReset", (_, _) => ActiveDocument?.ZoomReset(), Keys.Control | Keys.D0)
        });

        Register(languageMenu, "language");
        ukrainianItem.CheckOnClick = true;
        englishItem.CheckOnClick = true;
        polishItem.CheckOnClick = true;
        ukrainianItem.Click += (_, _) => ChangeLanguage(UiLanguage.Ukrainian);
        englishItem.Click += (_, _) => ChangeLanguage(UiLanguage.English);
        polishItem.Click += (_, _) => ChangeLanguage(UiLanguage.Polish);
        Register(ukrainianItem, "ukrainian");
        Register(englishItem, "english");
        Register(polishItem, "polish");
        languageMenu.DropDownItems.AddRange(new ToolStripItem[] { ukrainianItem, englishItem, polishItem });

        var windowMenu = Menu("window");
        windowMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            Item("cascade", (_, _) => LayoutMdi(MdiLayout.Cascade)),
            Item("tileHorizontal", (_, _) => LayoutMdi(MdiLayout.TileHorizontal)),
            Item("tileVertical", (_, _) => LayoutMdi(MdiLayout.TileVertical)),
            Item("arrangeIcons", (_, _) => LayoutMdi(MdiLayout.ArrangeIcons))
        });

        menuStrip.MdiWindowListItem = windowMenu;
        menuStrip.Items.AddRange(new ToolStripItem[]
        {
            fileMenu,
            editMenu,
            formatMenu,
            insertMenu,
            toolsMenu,
            languageMenu,
            windowMenu
        });
    }

    private void InitializeToolbar()
    {
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Dock = DockStyle.Top;
        Controls.Add(toolStrip);
        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            Button("new", (_, _) => CreateDocument()),
            Button("open", (_, _) => OpenDocument()),
            Button("save", (_, _) => SaveActiveDocument()),
            new ToolStripSeparator(),
            Button("cut", (_, _) => ActiveDocument?.Editor.Cut()),
            Button("copy", (_, _) => ActiveDocument?.Editor.Copy()),
            Button("paste", (_, _) => ActiveDocument?.Editor.Paste()),
            new ToolStripSeparator(),
            Button("bold", (_, _) => ActiveDocument?.ToggleStyle(FontStyle.Bold)),
            Button("italic", (_, _) => ActiveDocument?.ToggleStyle(FontStyle.Italic)),
            Button("underline", (_, _) => ActiveDocument?.ToggleStyle(FontStyle.Underline)),
            new ToolStripSeparator(),
            Button("left", (_, _) => ActiveDocument?.SetAlignment(HorizontalAlignment.Left)),
            Button("center", (_, _) => ActiveDocument?.SetAlignment(HorizontalAlignment.Center)),
            Button("right", (_, _) => ActiveDocument?.SetAlignment(HorizontalAlignment.Right)),
            new ToolStripSeparator(),
            Button("image", (_, _) => InsertImage()),
            Button("find", (_, _) => ShowFindDialog())
        });
    }

    private void InitializeStatus()
    {
        statusStrip.Dock = DockStyle.Bottom;
        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, positionLabel, infoLabel });
        Controls.Add(statusStrip);
    }

    private ToolStripMenuItem Menu(string key)
    {
        var item = new ToolStripMenuItem();
        Register(item, key);
        return item;
    }

    private ToolStripMenuItem Item(string key, EventHandler handler, Keys shortcut = Keys.None)
    {
        var item = new ToolStripMenuItem
        {
            ShortcutKeys = shortcut
        };
        item.Click += handler;
        Register(item, key);
        return item;
    }

    private ToolStripButton Button(string key, EventHandler handler)
    {
        var button = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        button.Click += handler;
        buttonKeys[button] = key;
        return button;
    }

    private void Register(ToolStripItem item, string key)
    {
        itemKeys[item] = key;
    }

    private void ApplyLanguage()
    {
        Text = T("appTitle");

        foreach (var pair in itemKeys)
        {
            pair.Key.Text = T(pair.Value);
        }

        foreach (var pair in buttonKeys)
        {
            pair.Key.Text = T(pair.Value);
            pair.Key.ToolTipText = T(pair.Value);
        }

        ukrainianItem.Checked = language == UiLanguage.Ukrainian;
        englishItem.Checked = language == UiLanguage.English;
        polishItem.Checked = language == UiLanguage.Polish;

        foreach (var document in MdiChildren.OfType<EditorDocument>())
        {
            document.ApplyLanguage(language);
        }

        findForm?.ApplyLanguage(language);
        UpdateState();
    }

    private void CreateDocument()
    {
        documentCounter++;
        var document = new EditorDocument(documentCounter, language)
        {
            MdiParent = this
        };
        document.DocumentStateChanged += (_, _) => UpdateState();
        document.NewDocument();
        document.Editor.WordWrap = wordWrapItem.Checked;
        document.SyntaxEnabled = syntaxItem.Checked;
        document.Show();
        document.Editor.Focus();
        UpdateState();
    }

    private void OpenDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = T("openFilter"),
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        foreach (var path in dialog.FileNames)
        {
            try
            {
                documentCounter++;
                var document = new EditorDocument(documentCounter, language)
                {
                    MdiParent = this
                };
                document.DocumentStateChanged += (_, _) => UpdateState();
                document.Editor.WordWrap = wordWrapItem.Checked;
                document.SyntaxEnabled = syntaxItem.Checked;
                document.LoadDocument(path);
                document.Show();
            }
            catch
            {
                MessageBox.Show(this, T("openError"), T("errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        UpdateState();
    }

    private bool SaveActiveDocument()
    {
        var document = ActiveDocument;

        if (document is null)
        {
            return false;
        }

        if (document.FilePath is null)
        {
            return SaveActiveDocumentAs();
        }

        try
        {
            document.Save();
            return true;
        }
        catch
        {
            MessageBox.Show(this, T("saveError"), T("errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool SaveActiveDocumentAs()
    {
        var document = ActiveDocument;

        if (document is null)
        {
            return false;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = T("saveFilter"),
            FilterIndex = 1,
            DefaultExt = "rtf",
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        try
        {
            document.SaveAs(dialog.FileName);
            return true;
        }
        catch
        {
            MessageBox.Show(this, T("saveError"), T("errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void SaveAllDocuments()
    {
        foreach (var document in MdiChildren.OfType<EditorDocument>())
        {
            document.Activate();

            if (!SaveActiveDocument())
            {
                break;
            }
        }
    }

    private void InsertImage()
    {
        var document = ActiveDocument;

        if (document is null)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = T("imageFilter")
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            document.InsertImage(dialog.FileName);
        }
        catch
        {
            MessageBox.Show(this, T("imageError"), T("errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintActiveDocument(bool preview)
    {
        var document = ActiveDocument;

        if (document is null)
        {
            return;
        }

        try
        {
            document.PrintDocument(preview);
        }
        catch
        {
            MessageBox.Show(this, T("printError"), T("errorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowFindDialog()
    {
        findForm ??= new FindReplaceForm(this, language);
        findForm.ApplyLanguage(language);
        findForm.Show(this);
        findForm.Activate();
    }

    private void ToggleSyntax()
    {
        foreach (var document in MdiChildren.OfType<EditorDocument>())
        {
            document.SyntaxEnabled = syntaxItem.Checked;

            if (syntaxItem.Checked)
            {
                document.ApplySyntaxHighlighting();
            }
        }
    }

    private void SetWordWrap(bool enabled)
    {
        foreach (var document in MdiChildren.OfType<EditorDocument>())
        {
            document.Editor.WordWrap = enabled;
        }
    }

    private void ChangeLanguage(UiLanguage nextLanguage)
    {
        language = nextLanguage;
        ApplyLanguage();
    }

    private void UpdateState()
    {
        var document = ActiveDocument;
        var hasDocument = document is not null;

        foreach (var pair in itemKeys)
        {
            pair.Key.Enabled = hasDocument || CanUseWithoutDocument(pair.Value);
        }

        foreach (var button in buttonKeys.Keys)
        {
            var key = buttonKeys[button];
            button.Enabled = key is "new" or "open" || hasDocument;
        }

        if (!hasDocument)
        {
            statusLabel.Text = T("statusNoDocument");
            positionLabel.Text = string.Empty;
            infoLabel.Text = string.Empty;
            return;
        }

        var position = document!.GetCaretPosition();
        statusLabel.Text = document.FilePath ?? T("untitled");
        positionLabel.Text = string.Format(T("lineColumn"), position.line, position.column);
        infoLabel.Text = string.Format(T("formatInfo"), document.Editor.TextLength);
    }

    private static bool CanUseWithoutDocument(string key)
    {
        return key is "file"
            or "new"
            or "open"
            or "exit"
            or "format"
            or "wordWrap"
            or "tools"
            or "syntax"
            or "language"
            or "ukrainian"
            or "english"
            or "polish"
            or "window";
    }

}
