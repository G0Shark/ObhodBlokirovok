using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Toolkit.Uwp.Notifications;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace ObhodBlokirovok;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Process awg;
    private Process clash;
    private int directs = 0;
    private int proxys = 0;
    private NotifyIcon _notifyIcon;
    private bool allIsBad = false;
    private AppSettings _settings;
    
    public MainWindow()
    {
        InitializeComponent();
        Console.SetOut(new RichTextBoxWriter(ConsoleOutput));
        AppendColoredText(ConsoleOutput, "ObhodBlokirovok, ver. 1.0.0.0\nПрограмма сделанна G0Shark.\n", Brushes.White);
        AppendColoredText(AWGProxyOutput, "Тут будут логи от AWGProxy\n", Brushes.White);
        AppendColoredText(ClashOutput, "Тут будут логи от Clash\n", Brushes.White);

        _frameTimer.Tick += UpdateGifFrame;
        
        AWGMenuStarter.Click += (sender, e) =>
        {
            if (awg != null)
            {
                awg.Kill();
                awg = null;
                AWGProxyIndicator.Fill = Brushes.Gray;
                if (clash != null)
                {
                    clash.Kill();
                    clash = null;
                    ClashIndicator.Fill = Brushes.Gray;
                }
                return;
            }
            StartAwg();
        };
        
        if (!ProgramTools.IsRunAsAdmin())
        {
            LoadGifIcon("icons\\non_admin.gif");
            AppendColoredText(ConsoleOutput, "Программа запущена не от Администратора, некоторые функции ограниченны.\n\n", Brushes.Aqua);
            Title += " (НЕ от администратора)";
            HostsChanger.IsEnabled = false;
            ClashIndicator.Fill = Brushes.DarkRed;
            ClashText.ToolTip = "Можно запустить только от администратора.";
            ClashStarter.ToolTip += "Можно запустить только от администратора.";
            HostsChanger.ToolTip += "Можно запустить только от администратора.";
            ClashStarter.IsEnabled = false;
        }
        else
        {
            LoadGifIcon("icons\\normal.gif");
            ClashText.MouseLeftButtonDown += ClashClick;
            ClashStarter.Click += (sender, e) =>
            {
                if (awg == null && clash == null)
                {
                    Logger.SendMessage(ConsoleOutput, "ERROR", $"Необходино запустить AWGProxy, прежде чем запускать Clash", Brushes.Red);
                    return;
                }

                if (clash != null)
                {
                    clash.Kill();
                    clash = null;
                    ClashIndicator.Fill = Brushes.Gray;
                    return;
                }

                StartClash();
            };
        }
        
        _settings = AppSettings.Load();
        
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = new Icon("icon.ico"); // Путь к .ico файлу
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "ObhodBlokirovok";
        
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Открыть", null, (s, e) => Show());
        contextMenu.Items.Add("Скрыть", null, (s, e) => Hide());
        contextMenu.Items.Add("Закрыть", null, (s, e) => CloseWindow());
        
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => Show();
    }

    public async Task autostart()
    {
        StartAwg();
        StartClash();
        Hide();
    }

    private void CloseWindow()
    {
        if (awg != null) awg.Kill();
        if (clash != null) clash.Kill();
        Application.Current.Shutdown();
    }
    private async Task StartClash()
    {
        Logger.SendMessage(ConsoleOutput, "RUN", $"Запуск Clash", Brushes.LightSeaGreen);
        ProcessStartInfo clashsi = new ProcessStartInfo
        {
            FileName = $"{Path.GetFullPath("./clash/clash.exe")}",
            Arguments = $"-d {Path.GetFullPath("./clash")}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        clash = new Process
        {
            StartInfo = clashsi,
            EnableRaisingEvents = true
        };
        
        clash.OutputDataReceived += (sender, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                string logLine = e.Data??"";
                var regex = new Regex(@"level=(\w+)\s+msg=""(.+)""");

                var match = regex.Match(logLine);

                if (match.Success)
                {
                    string level = match.Groups[1].Value;    // info
                    string message = match.Groups[2].Value; // TCP или UDP
                
                    Brush b = Brushes.Gray;
                    if (level == "info") b = Brushes.LightBlue;
                    if (level == "warning") b = Brushes.Orange;
                    if (level == "error") b = Brushes.Red;

                    if (level == "info")
                    {
                        if (ProgramTools.IsDirectConnection(logLine))
                        {
                            directs += 1;
                        }
                        else
                        {
                            proxys += 1;
                        }
                    
                        StatusRightText.Text = $"Пропущено: {directs} Ретранслировано: {proxys}";
                    }
                    
                    if (ProgramTools.CheckForAccessIsDenied(e.Data))
                    {
                        if (_settings.EnableNotifications) new ToastContentBuilder()
                            .AddText("Ошибка Clash")
                            .AddText("Программа запущенна без прав Администратора.")
                            .Show();
                    }
                    
                    Logger.SendMessage(ClashOutput, level.ToUpper(), $"{message}", b);
                    ClashOutput.ScrollToEnd();
                }
            });
        };

        clash.ErrorDataReceived += (sender, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                ClashOutput.AppendText(e.Data + Environment.NewLine);
                ClashOutput.ScrollToEnd();
            });
        };

        clash.Exited += (sender, args) =>
        {
            Dispatcher.Invoke(async () =>
            {
                Logger.SendMessage(ConsoleOutput, "STOP", $"Закрытие Clash", Brushes.Red);
                ClashIndicator.Fill = Brushes.Gray;
                LoadGifIcon("icons\\warning.gif");
                clash = null;
            });
        };
        
        LoadGifIcon("icons\\good.gif");
        clash.Start();
        clash.BeginOutputReadLine();
        clash.BeginErrorReadLine();
        
        ClashIndicator.Fill = Brushes.Green;
        Logger.SendMessage(ConsoleOutput, "SUCCESS", $"Clash Запущен", Brushes.LightGreen);
    }
    private async Task StartAwg()
    {
        Logger.SendMessage(ConsoleOutput, "RUN", $"Запуск AWGProxy", Brushes.LightSeaGreen);
        ProcessStartInfo awgsi = new ProcessStartInfo
        {
            FileName = "awgproxy", 
            Arguments = $"-c {Path.GetFullPath("./cfgs/conf.conf")}",
            UseShellExecute = false, 
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        awg = new Process
        {
            StartInfo = awgsi,
            EnableRaisingEvents = true
        };

        awg.OutputDataReceived += (sender, e) =>
        {
            if (ProgramTools.IsSocketBindError(e.Data))
            {
                if (_settings.EnableNotifications) new ToastContentBuilder()
                    .AddText("Ошибка AWGProxy")
                    .AddText("Программа не может занять порт. Может, у вас включён VPN?")
                    .Show();
            }
        };
        
        awg.ErrorDataReceived += (sender, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                string logLine = e.Data??"";
                var regex = new Regex(@"^(?<level>\w+):\s(?<datetime>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\s(?<message>.+)$");

                var match = regex.Match(logLine);

                if (ProgramTools.IsHandshakeTimeout(logLine, out int x))
                {
                    allIsBad = true;
                    if (x > 5)
                    {
                        StopIndicatorBlink();
                        if (_settings.EnableNotifications) new ToastContentBuilder()
                            .AddText("Ошибка AWGProxy")
                            .AddText("Соединение полностью потерянно, попытка перезапуска...")
                            .Show();
                        
                        awgclose();
                        StartAwg();
                    }

                    if (x == 2)
                    {
                        LoadGifIcon("icons\\error.gif");
                        StartIndicatorBlink();
                        if (_settings.EnableNotifications) new ToastContentBuilder()
                            .AddText("Ошибка AWGProxy")
                            .AddText("Программа потеряла соединение...")
                            .Show();
                    }
                    
                    return;
                }

                if (allIsBad)
                {
                    allIsBad = false;
                    StopIndicatorBlink();
                    ClashIndicator.Fill = Brushes.Green;
                    if (_settings.EnableNotifications) new ToastContentBuilder()
                        .AddText("AWGProxy")
                        .AddText("Соединение восстановленно.")
                        .Show();
                    
                    if (clash==null) LoadGifIcon("icons\\good.gif");
                    else LoadGifIcon("icons\\warning.gif");
                }
                
                if (match.Success)
                {
                    string level = match.Groups["level"].Value;
                    string message = match.Groups["message"].Value;

                    Brush b = Brushes.Gray;
                    
                    if (level == "DEBUG") b = Brushes.LightBlue;
                    if (level == "WARNING") b = Brushes.Orange;
                    if (level == "ERROR") b = Brushes.Red;
                    
                    Logger.SendMessage(AWGProxyOutput, level.ToUpper(), $"{message}", b);
                    AWGProxyOutput.ScrollToEnd();
                }
                else
                {
                    if (ProgramTools.IsSocketBindError(e.Data))
                    {
                        if (_settings.EnableNotifications) new ToastContentBuilder()
                            .AddText("Ошибка AWGProxy")
                            .AddText("Программа не может занять порт. Может, у вас включён VPN?")
                            .Show();
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        Logger.SendMessage(AWGProxyOutput, "ERROR", e.Data, Brushes.Red);
                    }
                }
            });
        };

        awg.Exited += (sender, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                Logger.SendMessage(ConsoleOutput, "STOP", $"Закрытие AWGProxy", Brushes.Red);
                AWGProxyIndicator.Fill = Brushes.Gray;
                
                if (clash != null)
                {
                    clash.Kill();
                    ClashIndicator.Fill = Brushes.Gray;
                    
                    if (ProgramTools.IsRunAsAdmin())
                    {
                        LoadGifIcon("icons\\normal.gif");
                    }
                    else
                    {
                        LoadGifIcon("icons\\non_admin.gif");
                    }
                }
                
                awg = null;
            });
        };
        
        LoadGifIcon("icons\\warning.gif");
        awg.Start();
        awg.BeginErrorReadLine();
        awg.BeginOutputReadLine();
        
        LoadGifIcon("good.gif");
        AWGProxyIndicator.Fill = Brushes.Green;
        Logger.SendMessage(ConsoleOutput, "SUCCESS", $"AWGProxy Запущен", Brushes.LightGreen);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_settings.EnableNoClose) {e.Cancel = true;
            Hide(); return;
        }
        CloseWindow();
    }
    
    void AppendColoredText(RichTextBox rtb, string text, Brush color)
    {
        text = "\n" + text;
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

    private void AWGproxyClick(object sender, MouseButtonEventArgs e)
    {
        if (awg != null)
        {
            if (ProgramTools.IsRunAsAdmin())
            {
                LoadGifIcon("icons\\normal.gif");
            }
            else
            {
                LoadGifIcon("icons\\non_admin.gif");
            }
            awg.Kill();
            awg = null;
            AWGProxyIndicator.Fill = Brushes.Gray;
            if (clash != null)
            {
                clash.Kill();
                clash = null;
                ClashIndicator.Fill = Brushes.Gray;
            }
            return;
        }
        StartAwg();
    }

    void awgclose()
    {
        if (awg != null)
        {
            if (ProgramTools.IsRunAsAdmin())
            {
                LoadGifIcon("icons\\normal.gif");
            }
            else
            {
                LoadGifIcon("icons\\non_admin.gif");
            }
            awg.Kill();
            awg = null;
            AWGProxyIndicator.Fill = Brushes.Gray;
            if (clash != null)
            {
                clash.Kill();
                clash = null;
                ClashIndicator.Fill = Brushes.Gray;
            }
        }
    }

    private void ClashClick(object sender, MouseButtonEventArgs e)
    {
        if (awg == null && clash == null)
        {
            Logger.SendMessage(ConsoleOutput, "ERROR", $"Необходино запустить AWGProxy, прежде чем запускать Clash", Brushes.Red);
            return;
        }

        if (clash != null)
        {
            clash.Kill();
            clash = null;
            ClashIndicator.Fill = Brushes.Gray;
            if (awg != null)
            {
                LoadGifIcon("icons\\warning.gif");
            }
            else
            {
                if (ProgramTools.IsRunAsAdmin())
                {
                    LoadGifIcon("icons\\normal.gif");
                }
                else
                {
                    LoadGifIcon("icons\\non_admin.gif");
                }
            }
            return;
        }

        StartClash();
    }

    private void AWGProxyCfgOpen(object sender, RoutedEventArgs e)
    {
        ConfigEditor configEditor = new ConfigEditor(Path.GetFullPath("./cfgs/conf.conf"));
        configEditor.ShowDialog();

        if (configEditor.changed && awg != null)
        {
            awg.Kill();
            AWGProxyIndicator.Fill = Brushes.Gray;
            StartAwg();
            if (clash != null)
            {
                clash.Kill();
                ClashIndicator.Fill = Brushes.Green;
                StartClash();
            }
        }
    }

    private void ClashCfgOpen(object sender, RoutedEventArgs e)
    {
        ConfigEditor configEditor = new ConfigEditor(Path.GetFullPath("./clash/config.yaml"));
        configEditor.ShowDialog();
        
        if (configEditor.changed && clash != null)
        {
            clash.Kill();
            ClashIndicator.Fill = Brushes.Gray;
            StartClash();
        }
    }

    private void WindowClose(object sender, RoutedEventArgs e)
    {
        CloseWindow();
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        Settings s = new Settings();
        s.ShowDialog();
        _settings = AppSettings.Load();
    }

    private void ChangeHosts(object sender, RoutedEventArgs e)
    {
        ProgramTools.UpdateHostsFileFromFile();
    }
    
    private DispatcherTimer _blinkTimer;
    private bool _isGreen = false;

    /// <summary>
    /// Запустить мигание индикатора
    /// </summary>
    public void StartIndicatorBlink()
    {
        if (_blinkTimer == null)
        {
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Interval = TimeSpan.FromMilliseconds(500);
            _blinkTimer.Tick += BlinkTimer_Tick;
        }

        if (!_blinkTimer.IsEnabled)
        {
            _blinkTimer.Start();
        }
    }

    /// <summary>
    /// Остановить мигание индикатора и вернуть цвет по умолчанию
    /// </summary>
    public void StopIndicatorBlink()
    {
        if (_blinkTimer != null && _blinkTimer.IsEnabled)
        {
            _blinkTimer.Stop();
        }

        ClashIndicator.Fill = Brushes.Gray;
        _isGreen = false;
    }

    /// <summary>
    /// Вспомогательный метод — переключает цвет
    /// </summary>
    private void BlinkTimer_Tick(object sender, EventArgs e)
    {
        ClashIndicator.Fill = _isGreen ? Brushes.Gray : Brushes.Green;
        _isGreen = !_isGreen;
    }

    private void OpenClashConfigurator(object sender, RoutedEventArgs e)
    {
        ClashConfigurator c = new ClashConfigurator();
        c.ShowDialog();
    }
    
    private GifBitmapDecoder _gifDecoder;
    private int _currentFrame = 0;
    private DispatcherTimer _frameTimer= new DispatcherTimer();
    private FileStream gifStream = new FileStream("icons\\normal.gif", FileMode.Open, FileAccess.Read, FileShare.Read);
    
    private void LoadGifIcon(string path)
    {
        if (!File.Exists(path))
            return;
        
        this.gifStream.Close();
        _frameTimer.Stop();
        
        gifStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _gifDecoder = new GifBitmapDecoder(gifStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        
        _frameTimer.Interval = TimeSpan.FromMilliseconds(100); // или подстрой под fps gif-а
        _frameTimer.Start();
    }

    private void UpdateGifFrame(object sender, EventArgs e)
    {
        if (_gifDecoder == null || _gifDecoder.Frames.Count == 0)
            return;

        var frame = _gifDecoder.Frames[_currentFrame];

        // Установка иконки окна
        this.Icon = BitmapFrame.Create(frame);

        using (var ms = new MemoryStream())
        {
            var encoder = new BmpBitmapEncoder(); // Лучше Bmp — стабильнее для GDI+
            encoder.Frames.Add(BitmapFrame.Create(frame));
            encoder.Save(ms);

            ms.Seek(0, SeekOrigin.Begin);

            using (var bmp = new Bitmap(ms))
            {
                IntPtr hIcon = bmp.GetHicon();
                using (var originalIcon = System.Drawing.Icon.FromHandle(hIcon))
                {
                    // Клонируем, чтобы оригинал можно было уничтожить
                    _notifyIcon.Icon = (System.Drawing.Icon)originalIcon.Clone();
                }

                DestroyIcon(hIcon); // Безопасно теперь
            }
        }

        _currentFrame = (_currentFrame + 1) % _gifDecoder.Frames.Count;
    }
    
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

}
public class RichTextBoxWriter : TextWriter
{
    private readonly RichTextBox _richTextBox;

    public RichTextBoxWriter(RichTextBox richTextBox)
    {
        _richTextBox = richTextBox;
    }

    public override void Write(char value)
    {
        _richTextBox.Dispatcher.Invoke(() =>
        {
            _richTextBox.Document.Blocks.Add(new Paragraph(new Run(value.ToString())));
            ScrollToEnd();
        });
    }

    public override void Write(string value)
    {
        _richTextBox.Dispatcher.Invoke(() =>
        {
            _richTextBox.Document.Blocks.Add(new Paragraph(new Run(value)));
            ScrollToEnd();
        });
    }

    public override void WriteLine(string value)
    {
        _richTextBox.Dispatcher.Invoke(() =>
        {
            _richTextBox.Document.Blocks.Add(new Paragraph(new Run(value)));
            ScrollToEnd();
        });
    }

    private void ScrollToEnd()
    {
        _richTextBox.ScrollToEnd();
    }

    public override Encoding Encoding => Encoding.UTF8;
}

