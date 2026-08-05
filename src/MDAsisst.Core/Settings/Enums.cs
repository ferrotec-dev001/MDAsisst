namespace MDAsisst.Core.Settings;

/// <summary>プレビューとエディタの表示レイアウト。</summary>
public enum LayoutMode { EditorOnly, PreviewOnly, Split }

/// <summary>最小アイコン化したときの待機位置。</summary>
public enum ScreenCorner { TopLeft, TopRight, BottomLeft, BottomRight }

/// <summary>アップデートの動作モード（FR-ST-04）。</summary>
public enum UpdateMode { Auto, Manual, Disabled }

/// <summary>アピアランスのプリセット。</summary>
public enum ThemePreset { Dark, Light, HighContrast, Custom }
