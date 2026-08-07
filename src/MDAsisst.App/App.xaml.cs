using System;
using System.Diagnostics;
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
        // ISS-017: 本アプリはトレイ常駐仕様(ADR-0010 AutoVisibilityStateMachine)のため、
        // ウィンドウを閉じてもプロセスは終了せず current\MDAsisst.dll 等をロードし続ける。
        // MSIのアンインストール/アップグレード(Major Upgrade)がRemoveFiles/InstallFilesを
        // 実行する時点で常駐プロセスが残っていると、対象ファイルがロックされたまま処理が
        // 進み、「Administratorsからのアクセス許可が必要です」という誤解を招く拒否ダイアログが
        // 発生する（実体はACL問題ではなくハンドル保持によるロック）。
        // OnBeforeUninstall / OnBeforeUpdate フックで既存の常駐インスタンスを確実に停止させる。
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => TerminateOtherRunningInstances())
            .OnBeforeUpdateFastCallback(_ => TerminateOtherRunningInstances())
            .Run();

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

    /// <summary>
    /// ISS-017: MSIのアンインストール/アップグレード直前に、トレイ常駐中のものを含む
    /// 既存の MDAsisst プロセスを確実に終了させる。current フォルダ配下の実行ファイル・
    /// DLL のハンドルを解放し、RemoveFiles/InstallFiles でのアクセス拒否を防ぐ。
    /// このメソッドは VelopackApp のフック経由で「一時起動されたインストーラー補助プロセス」
    /// から呼ばれるため、Environment.ProcessId は常駐プロセスとは別物になる。
    /// </summary>
    private static void TerminateOtherRunningInstances()
    {
        var currentProcessId = Environment.ProcessId;
        var processName = Process.GetCurrentProcess().ProcessName;

        foreach (var process in Process.GetProcessesByName(processName))
        {
            if (process.Id == currentProcessId) continue;

            try
            {
                // 通常終了を試みてから、応答がなければ強制終了する。
                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }
            catch
            {
                // 既に終了している／アクセス権がない等は無視して継続する。
                // ここで例外を伝播させるとアンインストール／更新自体が失敗するため。
            }
        }

        // OSがファイルハンドルを解放するまでの猶予。
        Thread.Sleep(500);
    }
}
