using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;

namespace ObhodBlokirovok;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        MainWindow window = new MainWindow();
        window.Show();

        if (e.Args.Length > 0 && e.Args[0] == "--autostart")
        {
            window.autostart();
        }

        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Необработанная ошибка (UI): {e.Exception.Message}", "Ошибка");
        e.Handled = true;

        Directory.CreateDirectory("logs");
        File.WriteAllText("logs\\error.txt", e.Exception.ToString());
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        MessageBox.Show($"Необработанная ошибка (домен): {ex?.Message}", "Критическая ошибка");

        Directory.CreateDirectory("logs");
        File.WriteAllText("logs\\error.txt", ex.ToString());
    }
}