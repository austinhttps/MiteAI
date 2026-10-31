using Microsoft.UI.Xaml;

namespace MiteAI.App;

public partial class App : Application
{
    private Window? _window;

    public App()
{
    this.UnhandledException += App_UnhandledException;
    
    try
    {
        this.InitializeComponent();
    }
    catch (System.Exception ex)
    {
        WriteCrashLog("App_Initialize", ex.ToString());
        throw;
    }
}

private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
{
    WriteCrashLog("Global_Unhandled", e.Exception.ToString());
}

private void WriteCrashLog(string source, string details)
{
    string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
    string filePath = System.IO.Path.Combine(desktopPath, "MiteAI_CrashLog.txt");
    System.IO.File.WriteAllText(filePath, $"[{source}]\n{details}");
}

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
