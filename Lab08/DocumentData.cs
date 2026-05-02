using System.IO;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;
using WpfTabItem = System.Windows.Controls.TabItem;

namespace Lab08;

internal sealed class DocumentData
{
    public DocumentData(int number, WpfRichTextBox editor, WpfTabItem tab)
    {
        Number = number;
        Editor = editor;
        Tab = tab;
    }

    public int Number { get; }
    public WpfRichTextBox Editor { get; }
    public WpfTabItem Tab { get; }
    public string? FilePath { get; set; }
    public string Format { get; set; } = System.Windows.DataFormats.XamlPackage;
    public bool IsModified { get; set; }

    public string DisplayName(string untitled, string modified)
    {
        var name = FilePath is null ? $"{untitled} {Number}" : Path.GetFileName(FilePath);
        return IsModified ? $"{name} ({modified})" : name;
    }
}
