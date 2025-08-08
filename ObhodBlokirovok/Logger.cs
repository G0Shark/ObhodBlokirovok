using System.Drawing;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace ObhodBlokirovok;

public static class Logger
{
    public static void SendMessage(RichTextBox rtb, string type, string text, Brush typeColor)
    {
        string currentText = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;
        int lineCount = currentText.Count(c => c == '\n');

        if (lineCount > 250)
        {
            rtb.Document.Blocks.Clear(); // Очистка
        }
        
        AppendColoredText(rtb, "[", Brushes.Gray);
        AppendColoredText(rtb, type, typeColor);
        AppendColoredText(rtb, "]", Brushes.Gray);
        AppendColoredText(rtb, $": {text}\n", Brushes.White);

        string rb = rtb.Name;
        if (rb == "ConsoleOutput") rb = "Console";
        if (rb == "AWGProxyOutput") rb = "AWGProxy";
        if (rb == "ClashOutput") rb = "Clash";

        SaveToLogFile($"logs\\{rb}.txt", rb, type, text);
    }
    static void AppendColoredText(RichTextBox rtb, string text, Brush color)
    {
        Run run = new Run(text);
        run.Foreground = color;
        Paragraph paragraph;

        if (rtb.Document.Blocks.LastBlock is Paragraph p)
            paragraph = p;
        else
        {
            paragraph = new Paragraph();
            rtb.Document.Blocks.Add(paragraph);
        }
        
        paragraph.Inlines.Add(run);
    }
    
    private static void SaveToLogFile(string logFilePath, string prog, string type, string text)
    {
        try
        {
            Directory.CreateDirectory("logs");
            File.AppendAllText(logFilePath, $"[{DateTime.Now}] [{type}]: {text}\n");
        }
        catch
        {
            Console.WriteLine("Ошибка при сохранении Логов");
        }
    }
}