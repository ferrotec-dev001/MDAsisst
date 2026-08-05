# MDAsisst テスト実施報告（実装フェーズ）

- 版数: 1.0
- 実施日: 2026-08-05
- 実施環境: .NET SDK 8.0.423 / Linux コンテナ（クロスビルド `EnableWindowsTargeting=true`）

## 1. ビルド結果

| 対象 | 結果 |
| --- | --- |
| MDAsisst.Core (net8.0) | 成功 / 警告 0 |
| MDAsisst.Rendering (net8.0-windows, WPF) | 成功 / 警告 0 |
| MDAsisst.Updating (net8.0-windows, Velopack 1.2.0) | 成功 / 警告 0 |
| MDAsisst.App (net8.0-windows, WPF, XAML) | 成功 / 警告 0 |
| ソリューション全体 | **成功（エラー 0・警告 0）** |

## 2. 単体テスト結果

| プロジェクト | 件数 | 結果 | 備考 |
| --- | --- | --- | --- |
| MDAsisst.Core.Tests | 60 | **60 件成功 / 失敗 0** | 設定・編集支援・状態機械・チートシート |
| MDAsisst.Rendering.Tests | 12 | **未実行（Windows 必須）** | WPF ランタイムが Linux に無いため。CI(windows-latest) で実行 |

### 2.1 カバーした観点（NFR-07: 正常系・境界値・異常値）

| 対象 | 正常系 | 境界値 | 異常値 |
| --- | --- | --- | --- |
| 設定の丸め処理 | 0.85 → そのまま | 0.2 / 1.0 ちょうど | NaN・範囲外・不正色文字列 |
| 設定の永続化 | 保存→再読込で一致 | 上限10件の履歴 | 破損JSON→退避して既定値起動 |
| リスト自動継続 | `- `/`1. `/`- [x] ` | インデント付き | 空項目→解除、非リスト行→非介入 |
| 装飾トグル | 選択を囲む | 選択なし（マーカーのみ挿入） | 範囲外インデックス |
| 自動表示/最小化 | 30秒で最小化 | 遅延ちょうど / 0秒=無効 | 負値、トレイ中の操作通知 |
| チートシート | 検索・補完候補 | 空キーワード=全件 | 該当なし=空 |

## 3. 実装中に判明した重要事項（設計へ反映済み）

| No | 事象 | 対応 |
| --- | --- | --- |
| 1 | `AllowsTransparency=True` は ClearType 無効化・再描画コスト増を招く | **ADR-0004** を起票し、`WS_EX_LAYERED` 方式へ設計変更 |
| 2 | Velopack は自動生成 `Main` と競合する | `App.xaml` を `Page` 化し `App.Main` を StartupObject に指定（csproj に明記） |
| 3 | 設定をアプリフォルダに置くと更新時に消える | 保存先を `%APPDATA%\MDAsisst\` に固定（`AppPaths`） |
| 4 | `SystemParameters.WorkArea` はプライマリモニタのみ | `MonitorFromWindow` + `GetDpiForWindow` による自前計算に変更（FR-WN-16） |
| 5 | GitHub API の未認証レート制限は 60 req/h・IP 単位（社内 NAT で共有） | 自動更新チェックを **24時間に1回** へ間引き、`LastCheckedUtc` を設定に保存 |
| 6 | WPF 依存の単体テストは Linux で実行不可 | テストを Core（net8.0）と Rendering（net8.0-windows）に分離し、後者は CI(Windows) で実行 |

## 4. 未実施（UAT および Windows 実機で確認が必要な項目）

- 半透明表示・ドラッグ移動・リサイズの実動作（FR-WN-01〜03）
- 最小アイコン化とマルチモニタ／DPI 200% 環境での位置（FR-WN-12/16）
- タスクトレイ格納・復帰、Windows 起動時の常駐（FR-WN-08/10）
- IME 変換中の補完抑止（FR-IA-05）
- インストーラー作成と自動更新の実地確認（NFR-01/10）
- メモリ実測（NFR-02）・起動時間（NFR-03）
