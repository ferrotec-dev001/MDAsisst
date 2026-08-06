# ADR-0010: Issue #11〜#16 対応方針（不具合修正・Fluentデザイン化）

- ステータス: 承認済み・実装済み
- 決定日: 2026-08-06
- 起票: GitHub Issue #11〜#16

## 背景

v0.3.0 リリース後の実機・レビューにより、GitHub Issue に6件の改善要望・不具合が
追加登録された。本ADRは各Issueへの対応方針と実装箇所を記録する。

## 対応一覧

| Issue | 内容 | 原因 | 対応 |
| --- | --- | --- | --- |
| #11 | タイトルバーの太字/斜体/リンク/表アイコンが不要 | Issue #6 でツールバーを統合した際、使用頻度の低い装飾コマンドまでタイトルバーに残っていた | 太字/斜体/リンクは既存の `Ctrl+B` / `Ctrl+I` / `Ctrl+K` ショートカットとチートシートから利用可能なため、タイトルバーのボタン4つを削除 |
| #12 | Setting内のフォント色・ウィンドウ色がエディタ／プレビュー領域にしか反映されない | タイトルバー・チートシート領域・設定画面の各パネルが固定色（`#252526` 等）で塗られており、`ApplyAppearance()` の `Window.Background` / `Foreground` を透過させていなかった | タイトルバー(`TitleBarGrid`)・チートシート(`CheatSheetPanel`)・設定画面の各パネルの `Background` を `Transparent` に変更し、`Window.Background` がそのまま透過するようにした。`ApplyAppearance()` からタイトル文字色等の子要素にも `Foreground` を明示伝播 |
| #13 | MD記法一覧アイコンに入力方法のポップアップコメントがない | `SnippetItem` に `Example`/`Description` は保持していたが、UIバインドしていなかった | チートシートボタンの `ToolTip` に `Title`（記法名）・`Example`（入力例）・`Description`（説明）を表示するテンプレートを追加 |
| #14 | 自動小型化後の復帰時にウィンドウサイズが変化する | 最小化中に Windows が返す座標が `-32000` 付近の疑似値になることがあり、これを検知せず `RestoreBounds`/`Left`/`Top`/`Width`/`Height` として保存・復元していた | `SettingsValidator` に座標の有効範囲チェック（`MinCoordinate`〜`MaxCoordinate`）を追加し、範囲外は既定値へ丸める。`MainWindow` の配置保存も `RestoreBounds` を優先して異常値の混入を防止 |
| #15 | 透明度設定を変更してもメインウィンドウ・設定ウィンドウの透明度が変わらない | `SettingsWindow` 自身には `WindowEffects.SetOpacity` が一度も呼ばれておらず、メインウィンドウ側も一部イベントでしか再適用していなかった | `SettingsWindow` にライブ反映用の `ApplyLiveAppearance()` を追加し、`Loaded` 時・スライダー操作時・色変更時に `WindowEffects.SetOpacity` を呼ぶよう統一 |
| #16 | メインウィンドウ・設定ウィンドウのデザインを WPF Fluent にしてほしい | 標準の濃色フラットスタイルのみで、ボタン等に角丸・アクセントカラー・ホバー/プレス遷移がなかった | ADR-0004 の半透明化方式（`WS_EX_LAYERED` + `WindowChrome`、`AllowsTransparency=False`）は変更せず、その上に見た目だけ Fluent 化。詳細は下記 |

## Issue #16: Fluentデザイン化の実装方針

### 変更しないもの（既存アーキテクチャの維持）

- **ADR-0004 の半透明化方式**: `AllowsTransparency=True` へは変更しない。引き続き
  `WS_EX_LAYERED` + `SetLayeredWindowAttributes` で透過度を制御し、ClearType と
  入力性能を維持する。
- **カスタムタイトルバー構成**（`WindowChrome` + 独自ボタン配置）。

### 追加したもの

1. **共通テーマ辞書 `Themes/FluentTheme.xaml`**（`App.xaml` からマージ）
   - カラートークン: アクセントカラー `#0078D4`（Fluent標準）とそのホバー/プレス濃淡、
     サーフェス色、ボーダー色を `SolidColorBrush` リソースとして定義。
   - 角丸トークン: コントロール用 `CornerRadius=4`、カード用 `CornerRadius=8`。
   - `TitleIconButton` / `FluentButton` / `FluentAccentButton` / `FluentCheatButton` /
     `FluentTextBox` / `FluentComboBox` / `FluentCheckBox` / `FluentRadioButton` /
     `FluentTabControl` の各スタイルを定義し、`ControlTemplate` で角丸＋ホバー/プレス時の
     背景遷移を実装。
   - 従来 `MainWindow.xaml` と `SettingsWindow.xaml` に重複定義されていた
     `TitleIconButton` は本辞書へ一本化（Issue #10 時点の重複を解消）。
2. **ウィンドウ角丸**（Windows 11 のみ）
   - `WindowEffects.ApplyRoundedCorners()` を追加し、`DwmSetWindowAttribute` の
     `DWMWA_WINDOW_CORNER_PREFERENCE=DWMWCP_ROUND` を `OnLoaded` 時に適用。
   - Windows 10 等 API 非対応環境では呼び出しが単に無効となり、既存の角丸なし表示に
     留まる（例外にならず安全）。
3. **設定画面のコントロールに Fluent スタイルを適用**
   - OK ボタンはアクセントカラーで強調（`FluentAccentButton`）、その他ボタンは
     ニュートラルな `FluentButton`。
   - テキストボックス・コンボボックス・チェックボックス・ラジオボタン・タブに
     それぞれ対応スタイルを適用し、フォーカス時はアクセントカラーの枠線を表示。
   - チートシートの記法ボタンも角丸＋ホバー時にアクセントカラーの枠線を表示する
     `FluentCheatButton` に変更。

### 影響範囲・非影響確認

- 透過度・トレイ最小化・自動展開/最小化・ライブプレビュー等のコア挙動は変更していない
  （スタイルの `Template` 差し替えのみで、`Click`/`TextChanged` 等のイベント配線は無変更）。
- `dotnet build` / `dotnet test`（`MDAsisst.Core.Tests`）は全件成功を確認済み。
