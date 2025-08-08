using System.IO;
using System.Windows;

namespace ObhodBlokirovok;

public partial class ConfigEditor : Window
{
    private string path = "";
    public bool changed = false;
    public ConfigEditor(string Path)
    {
        path = Path;
        InitializeComponent();

        editorTextBox.Text = File.ReadAllText(path);
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        File.WriteAllText(path, editorTextBox.Text);
        changed = true;
        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        editorTextBox.Copy();
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        editorTextBox.Paste();
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        editorTextBox.Cut();
    }
    
    private void IncreaseFont_Click(object sender, RoutedEventArgs e)
    {
        editorTextBox.FontSize += 2;
    }

    private void DecreaseFont_Click(object sender, RoutedEventArgs e)
    {
        if (editorTextBox.FontSize > 6)
            editorTextBox.FontSize -= 2;
    }
}