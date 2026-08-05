using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MDAsisst.App.Services;
using MDAsisst.Core.Logging;
using MDAsisst.Core.Settings;
using MDAsisst.Core.Snippets;

namespace MDAsisst.App.ViewModels;

/// <summary>メイン画面の状態。WPF 依存の描画処理は View 側に置く。</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly DocumentService _documents;
    private readonly ILogSink _log;
    private DocumentFormat _format = DocumentFormat.Default;

    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string _statusMessage = "準備完了";

    public AppSettings Settings { get; }

    /// <summary>
    /// Issue #5: 検索前提をやめ、全カテゴリ・全項目を常時アイコンボタンとして表示するため、
    /// 絞り込み用の FilteredSnippets は廃止し Categories のみを View にバインドする。
    /// </summary>
    public ObservableCollection<CheatSheetCategory> Categories { get; } = new();

    public MainViewModel(AppSettings settings, ICheatSheetProvider cheatSheet, DocumentService documents, ILogSink log)
    {
        Settings = settings;
        _documents = documents;
        _log = log;

        foreach (var c in cheatSheet.GetCategories()) Categories.Add(c);
    }

    public string Title => (IsDirty ? "* " : string.Empty) +
                           (FilePath is null ? "無題.md" : Path.GetFileName(FilePath)) +
                           " - MDAsisst";

    /// <summary>プレビューの画像相対パス解決に使う基準ディレクトリ。</summary>
    public string? BaseDirectory => FilePath is null ? null : Path.GetDirectoryName(FilePath);

    partial void OnTextChanged(string value) => IsDirty = true;

    partial void OnFilePathChanged(string? value) => OnPropertyChanged(nameof(Title));

    partial void OnIsDirtyChanged(bool value) => OnPropertyChanged(nameof(Title));

    public void NewDocument()
    {
        Text = string.Empty;
        FilePath = null;
        _format = DocumentFormat.Default;
        IsDirty = false;
        StatusMessage = "新規ドキュメント";
    }

    public bool Open(string path)
    {
        try
        {
            var (text, format) = _documents.Load(path);
            Text = text;
            _format = format;
            FilePath = path;
            IsDirty = false;
            Settings.AddRecentFile(path);
            StatusMessage = $"開きました: {Path.GetFileName(path)}";
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"ファイルを開けませんでした: {Path.GetFileName(path)}", ex);
            StatusMessage = "ファイルを開けませんでした";
            return false;
        }
    }

    public bool Save(string? path = null)
    {
        var target = path ?? FilePath;
        if (string.IsNullOrEmpty(target)) return false;

        if (!_documents.Save(target, Text, _format))
        {
            StatusMessage = "保存に失敗しました";
            return false;
        }

        FilePath = target;
        IsDirty = false;
        Settings.AddRecentFile(target);
        StatusMessage = $"保存しました: {Path.GetFileName(target)} ({DateTime.Now:HH:mm:ss})";
        return true;
    }
}
