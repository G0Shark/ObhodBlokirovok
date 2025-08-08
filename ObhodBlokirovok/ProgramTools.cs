using System.IO;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32.TaskScheduler;
using Task = System.Threading.Tasks.Task;

namespace ObhodBlokirovok;

public static class ProgramTools
{
    public static bool IsRunAsAdmin()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
    
    public static void UpdateHostsFileFromFile()
        {
            string hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
            string prefsFileName = "hostsPrefs.txt";
            string prefsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, prefsFileName);

            if (!File.Exists(prefsPath))
            {
                Console.WriteLine($"Файл {prefsFileName} не найден рядом с exe.");
                return;
            }

            string[] newEntries;
            try
            {
                newEntries = File.ReadAllLines(prefsPath)
                                 .Select(line => line.Trim())
                                 .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                                 .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при чтении hostsPrefs.txt: " + ex.Message);
                return;
            }

            try
            {
                if (!File.Exists(hostsPath))
                {
                    Console.WriteLine("Файл hosts не найден.");
                    return;
                }

                var existingLines = File.ReadAllLines(hostsPath)
                                        .Select(line => line.Trim())
                                        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                List<string> linesToAdd = newEntries
                    .Where(e => !existingLines.Contains(e))
                    .ToList();

                if (linesToAdd.Count == 0)
                {
                    Console.WriteLine("Все записи из файла уже присутствуют в hosts.");
                }
                else
                {
                    File.AppendAllLines(hostsPath, new[] { "\n# Custom entries added on " + DateTime.Now + " by ObhodBlokirovok" }.Concat(linesToAdd));
                    Console.WriteLine($"Добавлено новых записей: {linesToAdd.Count}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Нет прав на запись. Запустите от имени администратора.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при обновлении hosts: " + ex.Message); 
            }
        }
    
    public static bool CheckForAccessIsDenied(string s)
    {
        var regex = new Regex(@"Access is denied\.", RegexOptions.IgnoreCase);

        if (regex.IsMatch(s)) return true;
        return false;
    }
    
    public static bool IsSocketBindError(string logLine)
    {
        if (string.IsNullOrWhiteSpace(logLine))
            return false;

        // Приводим строку к нижнему регистру для надёжного поиска
        string lower = logLine.ToLowerInvariant();

        // Проверяем наличие ключевых фрагментов
        return lower.Contains("bind: only one usage of each socket address");
    }

    public static bool IsDirectConnection(string logLine)
    {
        if (string.IsNullOrWhiteSpace(logLine))
            return false;

        // Приводим к нижнему регистру для надёжности
        string lower = logLine.ToLowerInvariant();

        // Проверяем, что строка содержит "using direct"
        // и начинается с ожидаемого формата (TCP или UDP)
        return (lower.Contains("using direct") &&
                (lower.Contains("[tcp]") || lower.Contains("[udp]")));
    }

    public static bool IsHandshakeTimeout(string logLine, out int attempt)
    {
        var match = Regex.Match(logLine, @"try\s+(\d+)", RegexOptions.IgnoreCase);

        if (logLine.Contains("Handshake did not complete after") && match.Success)
        {
            attempt = int.Parse(match.Groups[1].Value);
            return true;
        }

        attempt = 0;
        return false;
    }

    private static string TaskName = "ObhodBlokirovok";
    
    public static void RegisterWithTaskScheduler()
    {
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

        using (TaskService ts = new TaskService())
        {
            TaskDefinition td = ts.NewTask();
            td.RegistrationInfo.Description = "ObhodBlokirovok, автозапуск с правами Администратора (необходимо для Clash), отключить возможно в настройках программы.";

            td.Principal.RunLevel = TaskRunLevel.Highest; // 🟢 Запуск от имени администратора
            td.Principal.LogonType = TaskLogonType.InteractiveToken;

            td.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromSeconds(5) }); // Запуск при входе

            td.Actions.Add(new ExecAction(Path.GetFullPath("ObhodBlokirovok.exe"), "--autostart", AppDomain.CurrentDomain.BaseDirectory));

            ts.RootFolder.RegisterTaskDefinition(TaskName, td);
        }
    }
    
    public static void UnregisterFromTaskScheduler()
    {
        using (TaskService ts = new TaskService())
        {
            var task = ts.FindTask(TaskName, true); // ← альтернативный способ
            if (task != null)
            {
                ts.RootFolder.DeleteTask(TaskName);
            }
        }
    }
}