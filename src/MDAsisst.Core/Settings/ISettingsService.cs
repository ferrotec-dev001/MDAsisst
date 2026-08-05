namespace MDAsisst.Core.Settings;

/// <summary>設定の読み書きを担う。</summary>
public interface ISettingsService
{
    /// <summary>現在の設定。Load 前でも既定値を返す。</summary>
    AppSettings Current { get; }

    /// <summary>設定を読み込む。破損・欠損時は既定値を返し、破損ファイルは退避する（FR-ST-02）。</summary>
    AppSettings Load();

    /// <summary>設定を保存する。失敗しても例外を投げず false を返す。</summary>
    bool Save(AppSettings settings);

    /// <summary>全設定を既定値へ戻して保存する（FR-ST-03）。</summary>
    AppSettings ResetToDefaults();
}
