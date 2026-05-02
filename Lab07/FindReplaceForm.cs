namespace Lab07;

internal sealed class FindReplaceForm : Form
{
    private readonly MainForm ownerForm;
    private readonly Label findLabel = new();
    private readonly Label replaceLabel = new();
    private readonly TextBox findBox = new();
    private readonly TextBox replaceBox = new();
    private readonly CheckBox matchCaseBox = new();
    private readonly CheckBox wholeWordBox = new();
    private readonly Button findButton = new();
    private readonly Button replaceButton = new();
    private readonly Button replaceAllButton = new();
    private readonly Button closeButton = new();
    private UiLanguage language;

    public FindReplaceForm(MainForm owner, UiLanguage currentLanguage)
    {
        ownerForm = owner;
        language = currentLanguage;
        InitializeForm();
        ApplyLanguage(currentLanguage);
    }

    public void ApplyLanguage(UiLanguage currentLanguage)
    {
        language = currentLanguage;
        Text = TextCatalog.T(language, "findTitle");
        findLabel.Text = TextCatalog.T(language, "findText");
        replaceLabel.Text = TextCatalog.T(language, "replaceText");
        matchCaseBox.Text = TextCatalog.T(language, "matchCase");
        wholeWordBox.Text = TextCatalog.T(language, "wholeWord");
        findButton.Text = TextCatalog.T(language, "findNext");
        replaceButton.Text = TextCatalog.T(language, "replaceOne");
        replaceAllButton.Text = TextCatalog.T(language, "replaceAll");
        closeButton.Text = TextCatalog.T(language, "closeDialog");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    private void InitializeForm()
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(470, 195);

        findLabel.SetBounds(16, 18, 120, 24);
        findBox.SetBounds(140, 16, 300, 24);
        replaceLabel.SetBounds(16, 52, 120, 24);
        replaceBox.SetBounds(140, 50, 300, 24);
        matchCaseBox.SetBounds(140, 82, 180, 24);
        wholeWordBox.SetBounds(320, 82, 140, 24);
        findButton.SetBounds(16, 124, 105, 32);
        replaceButton.SetBounds(129, 124, 105, 32);
        replaceAllButton.SetBounds(242, 124, 105, 32);
        closeButton.SetBounds(355, 124, 85, 32);

        findButton.Click += (_, _) => FindNext();
        replaceButton.Click += (_, _) => ReplaceOne();
        replaceAllButton.Click += (_, _) => ReplaceAll();
        closeButton.Click += (_, _) => Hide();
        AcceptButton = findButton;
        CancelButton = closeButton;

        Controls.AddRange(new Control[]
        {
            findLabel,
            findBox,
            replaceLabel,
            replaceBox,
            matchCaseBox,
            wholeWordBox,
            findButton,
            replaceButton,
            replaceAllButton,
            closeButton
        });
    }

    private EditorDocument? CurrentDocument()
    {
        return ownerForm.ActiveDocument;
    }

    private bool ValidateFindText()
    {
        if (!string.IsNullOrEmpty(findBox.Text))
        {
            return true;
        }

        MessageBox.Show(this, TextCatalog.T(language, "emptyFind"), TextCatalog.T(language, "findTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        findBox.Focus();
        return false;
    }

    private void FindNext()
    {
        if (!ValidateFindText())
        {
            return;
        }

        var document = CurrentDocument();

        if (document is null || !document.FindText(findBox.Text, matchCaseBox.Checked, wholeWordBox.Checked))
        {
            MessageBox.Show(this, TextCatalog.T(language, "notFound"), TextCatalog.T(language, "findTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ReplaceOne()
    {
        if (!ValidateFindText())
        {
            return;
        }

        var document = CurrentDocument();

        if (document is null || !document.ReplaceCurrent(findBox.Text, replaceBox.Text, matchCaseBox.Checked, wholeWordBox.Checked))
        {
            MessageBox.Show(this, TextCatalog.T(language, "notFound"), TextCatalog.T(language, "findTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ReplaceAll()
    {
        if (!ValidateFindText())
        {
            return;
        }

        var document = CurrentDocument();
        var count = document?.ReplaceAll(findBox.Text, replaceBox.Text, matchCaseBox.Checked, wholeWordBox.Checked) ?? 0;
        MessageBox.Show(this, string.Format(TextCatalog.T(language, "replaceCount"), count), TextCatalog.T(language, "findTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
