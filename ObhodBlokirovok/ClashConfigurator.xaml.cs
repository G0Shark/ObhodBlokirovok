using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace ObhodBlokirovok;

public partial class ClashConfigurator : Window
{
    private ObservableCollection<RuleEntry> _rules = new();
    private string clashConfigPath = @"clash\config.yaml";

    public ClashConfigurator()
    {
        InitializeComponent();
        RulesListBox.ItemsSource = _rules;
        LoadYaml();
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        var type = (RuleTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var value = RuleTextBox.Text.Trim();
        var name = DisplayNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
        {
            ErrorText.Text = "Выберите тип и введите значение.";
            return;
        }

        var entry = new RuleEntry
        {
            Type = type,
            Value = value,
            DisplayName = string.IsNullOrWhiteSpace(name) ? null : name
        };

        _rules.Add(entry);

        RuleTextBox.Clear();
        DisplayNameTextBox.Clear();
        RuleTypeComboBox.SelectedIndex = -1;
    }

    private void DeleteRuleInline_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is RuleEntry rule)
        {
            _rules.Remove(rule);
        }
    }

    private void SaveYaml_Click(object sender, RoutedEventArgs e)
    {
        var staticHeader = new[]
        {
            "mixed-port: 7890",
            "allow-lan: true",
            "mode: rule",
            "log-level: info",
            "",
            "tun:",
            "  enable: true",
            "  stack: system",
            "  auto-route: true",
            "  auto-detect-interface: true",
            "  dns-hijack:",
            "    - 198.18.0.2:53",
            "",
            "dns:",
            "  enable: true",
            "  listen: 0.0.0.0:53",
            "  ipv6: false",
            "  enhanced-mode: fake-ip",
            "  nameserver:",
            "    - 1.1.1.1",
            "    - 8.8.8.8",
            "  fallback:",
            "    - tls://1.1.1.1",
            "    - tls://dns.google",
            "  fallback-filter:",
            "    geoip: true",
            "    geoip-code: US",
            "",
            "proxies:",
            "  - name: \"socks5-proxy\"",
            "    type: socks5",
            "    server: 127.0.0.1",
            "    port: 1080",
            "    udp: true",
            "",
            "proxy-groups:",
            "  - name: \"ObhodBlokirovok\"",
            "    type: select",
            "    proxies:",
            "      - \"socks5-proxy\"",
            "",
            "rules:"
        };

        var rules = _rules.SelectMany(r =>
            string.IsNullOrWhiteSpace(r.DisplayName)
                ? new[] { $"  - {r}" }
                : new[] { $"  # {r.DisplayName}", $"  - {r}" });

        var fullYaml = staticHeader.Concat(rules).Append("  - MATCH,DIRECT");

        Directory.CreateDirectory(Path.GetDirectoryName(clashConfigPath)!);
        File.WriteAllLines(clashConfigPath, fullYaml);

        ErrorText.Text = "Сохранено в " + clashConfigPath;
    }

    private void LoadYaml()
    {
        if (!File.Exists(clashConfigPath))
            return;

        var loadedRules = new ObservableCollection<RuleEntry>();
        string? lastComment = null;

        foreach (var line in File.ReadLines(clashConfigPath))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("#"))
            {
                lastComment = trimmed.Substring(1).Trim();
            }
            else if (trimmed.StartsWith("- "))
            {
                var raw = trimmed.Substring(2);
                var parts = raw.Split(',');

                if (parts.Length == 3)
                {
                    loadedRules.Add(new RuleEntry
                    {
                        Type = parts[0].Trim(),
                        Value = parts[1].Trim(),
                        ProxyGroup = parts[2].Trim(),
                        DisplayName = lastComment
                    });
                }

                lastComment = null;
            }
        }

        _rules.Clear();
        foreach (var rule in loadedRules)
            _rules.Add(rule);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

public class RuleEntry
{
    public string Type { get; set; }
    public string Value { get; set; }
    public string ProxyGroup { get; set; } = "ObhodBlokirovok";
    public string? DisplayName { get; set; }

    public override string ToString() => $"{Type},{Value},{ProxyGroup}";
}

public class RuleTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "DOMAIN-SUFFIX" => "🌐",
            "DOMAIN-KEYWORD" => "🔍",
            "PROCESS-NAME" => "⚙️",
            _ => "❓"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
