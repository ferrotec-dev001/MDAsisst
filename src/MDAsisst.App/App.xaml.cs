using System.Threading;
using System.Windows;
using MDAsisst.App.Services;
using MDAsisst.App.Views;
using MDAsisst.Core;
using MDAsisst.Core.Logging;
using MDAsisst.Core.Settings;
using MDAsisst.Core.Snippets;
using MDAsisst.Updating;
using Velopack;

namespace MDAsisst.App;

/// <summary>
/// アプリケーションのエントリポイント兼コンポジションルート。
/// Velopack の要求により自動生成 Main を使わず、ここで最初に VelopackApp.Run() を呼ぶ。
/// </summary>
public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;

    public ILogSink Log { get; private set; } = NullLogSink.Instance;
    public ISettingsService Settings { get; private set; } = default!;
    public ICheatSheetProvider CheatSheet { get; private set; } = default!;
    public IUpdateService UpdateService { get; private set; } = default!;
    public DocumentService Documents { get; private set; } = default!;

    public static new App Current => (App)Application.Current;

    [STAThread]
    public static void Main(string[] args)
    {
        // 何よりも先に実行する。インストール/更新フックはこの中で完結して終了する。
        VelopackApp.Build().Run();

        // 常駐アプリのため多重起動を防ぐ。
        _singleInstanceMutex = new Mutex(true, @"Local\MDAsisst.SingleInstance", out var isNew);
        if (!isNew) return;

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log = new FileLogSink(AppPaths.LogDirectory);
        Settings = JsonSettingsService.CreateDefault(Log);
        var settings = Settings.Load();

        CheatSheet = new EmbeddedCheatSheetProvider(AppPaths.UserSnippetsPath, Log);
        Documents = new DocumentService(Log);
        UpdateService = UpdateServiceFactory.Create(settings.Update.Mode, Log);

        // 未処理例外でアプリを落とさない（NFR-05: オフライン時の更新失敗等を含む）。
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error("未処理の UI 例外を捕捉しました。", args.Exception);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Warn("未観測のタスク例外を捕捉しました。", args.Exception);
            args.SetObserved();
        };

        var startInTray = e.Args.Contains("--tray", StringComparer.OrdinalIgnoreCase);
        var window = new MainWindow();
        MainWindow = window;
        if (!startInTray) window.Show();
        Log.Info($"MDAsisst を起動しました（tray={startInTray}, updateMode={settings.Update.Mode}）。");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Info("MDAsisst を終了しました。");
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
