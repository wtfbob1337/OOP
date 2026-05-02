using System.Windows;

namespace Lab08;

public partial class FindReplaceWindow
{
    private readonly MainWindow ownerWindow;
    private UiLanguage language;

    public FindReplaceWindow(MainWindow owner, UiLanguage currentLanguage)
    {
        ownerWindow = owner;
        language = currentLanguage;
        InitializeComponent();
        ApplyLanguage(currentLanguage);
    }

    public void ApplyLanguage(UiLanguage currentLanguage)
    {
        language = currentLanguage;
        Title = TextCatalog.T(language, "findTitle");
        FindLabel.Text = TextCatalog.T(language, "findText");
        ReplaceLabel.Text = TextCatalog.T(language, "replaceText");
        MatchCaseBox.Content = TextCatalog.T(language, "matchCase");
        FindButton.Content = TextCatalog.T(language, "findNext");
        ReplaceButton.Content = TextCatalog.T(language, "replaceOne");
        ReplaceAllButton.Content = TextCatalog.T(language, "replaceAll");
        CloseButton.Content = TextCatalog.T(language, "close");
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateText())
        {
            return;
        }

        if (!ownerWindow.FindNext(FindBox.Text, MatchCaseBox.IsChecked == true))
        {
            MessageBox.Show(this, TextCatalog.T(language, "notFound"), TextCatalog.T(language, "findTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ReplaceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateText())
        {
            return;
        }

        if (!ownerWindow.ReplaceOne(FindBox.Text, ReplaceBox.Text, MatchCaseBox.IsChecked == true))
        {
            MessageBox.Show(this, TextCatalog.T(language, "notFound"), TextCatalog.T(language, "findTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateText())
        {
            return;
        }

        var count = ownerWindow.ReplaceAll(FindBox.Text, ReplaceBox.Text, MatchCaseBox.IsChecked == true);
        MessageBox.Show(this, string.Format(TextCatalog.T(language, "replaceCount"), count), TextCatalog.T(language, "findTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private bool ValidateText()
    {
        if (!string.IsNullOrEmpty(FindBox.Text))
        {
            return true;
        }

        MessageBox.Show(this, TextCatalog.T(language, "emptyFind"), TextCatalog.T(language, "findTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        FindBox.Focus();
        return false;
    }
}
