using System.Windows;

namespace ObhodBlokirovok;

public partial class Settings : Window
{
    public AppSettings CurrentSettings { get; set; }

    public Settings()
    {
        InitializeComponent();

        CurrentSettings = AppSettings.Load();
        DataContext = CurrentSettings;

        if (!ProgramTools.IsRunAsAdmin()) {
            AutostartCB.IsEnabled = false;
            AutostartCB.Content = "Можно изменить, если запустить от имени Администратора.";
        }
        else
        {
            AutostartCB.Checked += Checked;
            AutostartCB.Unchecked += ToggleButton_OnUnchecked;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        CurrentSettings.Save();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Checked(object sender, RoutedEventArgs e)
    {
        ProgramTools.RegisterWithTaskScheduler();
        CurrentSettings.Save();
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        ProgramTools.UnregisterFromTaskScheduler();
        CurrentSettings.Save();
    }
}