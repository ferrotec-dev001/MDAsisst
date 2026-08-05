# MDAsisst 開発タスク・進捗（MD駆動設計）

最終更新: 2026-08-05

## フェーズ 1: 要件・設計 — 完了

- [x] 要件定義（`docs/requirements.md` v2.0、機能要件 40 項目 / 非機能 10 項目）
- [x] 重要決定の記録（ADR-0001〜0004）
- [x] アーキテクチャ設計（`docs/architecture.md` v2.0）
- [x] 技術調査（Velopack / Markdig→FlowDocument / WPF 半透明・トレイ）

## フェーズ 2: 実装 — 完了（コアおよびUI骨格）

- [x] `MDAsisst.Core`: 設定モデル・検証・JSON永続化・破損時退避
- [x] `MDAsisst.Core`: チートシート（7カテゴリ・18項目、内蔵JSON）
- [x] `MDAsisst.Core`: 入力支援（リスト継続・装飾トグル・表雛形・トリガー検出）
- [x] `MDAsisst.Core`: 自動表示/最小化 状態機械（IClock 注入でテスト可能）
- [x] `MDAsisst.Core`: ファイルロガー（7日ローテーション・本文非記録）
- [x] `MDAsisst.Rendering`: Markdig AST → FlowDocument レンダラ、テーマ適用
- [x] `MDAsisst.Updating`: IUpdateService / Velopack 実装 / Null 実装（不要モード）
- [x] `MDAsisst.App`: 半透明ウィンドウ（WindowChrome + WS_EX_LAYERED）
- [x] `MDAsisst.App`: エディタ・プレビュー・チートシートの3ペインUI、レイアウト切替
- [x] `MDAsisst.App`: トレイ常駐、最小アイコン化、設定画面、自動起動登録
- [x] CI / リリースワークフロー（GitHub Actions）

## フェーズ 3: 検証 — 一部完了

- [x] ソリューション全体のビルド（エラー0・警告0）
- [x] Core 単体テスト 60 件すべて成功
- [ ] Rendering 単体テスト 12 件（**Windows 実機 / CI で実行が必要**）
- [ ] Windows 実機での動作確認（半透明・トレイ・DPI・IME）

## フェーズ 4: UAT 準備 — 未着手（次の作業）

- [ ] Windows 実機ビルドとインストーラー生成（`vpk pack`）
- [ ] メモリ実測（NFR-02）・起動時間計測（NFR-03）
- [ ] **プレビュー応答性の実機計測（NFR-04, ADR-0006）**: 対象PCで5,000行文書を編集し500ms以内か確認
- [ ] オフライン動作確認（NFR-05）
- [x] 操作説明書（`docs/operation-manual.md`）の作成（v1.0、2026-08-05）
- [ ] UAT シナリオ作成と実施 → 指摘対応

## Issue #1〜#10 対応（2026-08-05, v0.2.0）

UAT前レビューで登録された10件のGitHub Issueすべてに対応した。詳細は **ADR-0007** を参照。

| Issue | 内容 | 状態 |
| --- | --- | --- |
| #1 | 透過度がすぐに反映されない | 解決（ライブプレビュー機構を追加） |
| #2 | 色指定がカラーコードで不便 | 解決（色選択ダイアログ＋スウォッチ追加） |
| #3 | 終了時に不要な保存確認が出る | 解決（ファイル読込時のIsDirty誤検知を修正） |
| #4 | アプリアイコン未設定 | 解決（Assets/icon.ico 新規作成、トレイにも設定） |
| #5 | チートシートが検索前提で分かりにくい | 解決（全項目を常時アイコンボタン表示に変更） |
| #6 | メニューの配置 | 解決（タイトルバーへアイコン統合、専用ツールバー行を廃止） |
| #7 | ステータスバー不要 | 解決（行を削除、バージョンは設定画面のみに一本化） |
| #8 | エディタ/プレビューの配置 | 解決（左側を上下分割に変更） |
| #9 | 縮小化アイコンが大きい | 解決（44×44のアイコンのみに縮小） |
| #10 | 設定ウィンドウのタイトルバー不統一 | 解決（メイン画面と同一のカスタムチロームに統一） |

## インストーラーに関する状況（2026-08-05 時点）

- サンドボックス環境（Linux）で `dotnet publish -r win-x64 --self-contained` は成功することを確認した。
- しかし **`vpk pack` は実行ホストのOSに応じたパッケージしか生成できず、Linux上ではLinux用
  AppImageしか作れない**（Windows向けインストーラーはビルドできない）ことを確認した。
- Windows用インストーラー（`MDAsisst-win-Setup.exe`）の生成には、以下のいずれかが必要：
  1. 実機Windows環境での `vpk pack` 実行
  2. `.github/workflows/release.yml`（`windows-latest` ランナー）によるタグpush起動のCIビルド
- **本タスクは未着手**。リリース判断（バージョンタグ付与）はユーザーの承認後に実施する。

> **更新（2026-08-05, ADR-0009）**: 上記の `MDAsisst-win-Setup.exe`（per-user, `%LocalAppData%`）は
> 配布物として**廃止**する。以後の配布物は `vpk pack --msi --instLocation PerMachine` が生成する
> **`.msi`**（Program Files への per-machine インストール）とする。

## v0.3.0: Program Files インストール化・更新の同意必須化（2026-08-05）

フェローテック社内端末（SN11, EDR: Cybereason）で v0.2.1 実機起動不能が再発。原因調査の結果、
Velopackのper-userインストール（`%LocalAppData%`＋未署名＋自己書き換え）がEDRの誤検知条件に
該当することが判明した（ISS-008）。ADR-0009に基づき以下を実施した。

- `.github/workflows/release.yml`: `vpk pack` に `--msi --instLocation PerMachine` を追加し、
  Program Files への per-machine インストール用MSIを生成するよう変更
- `IUpdateService.ApplyOnExit`（無人バックグラウンド自動適用）を削除
- 「自動」更新モードの意味を「バックグラウンドでの確認のみ」に再定義し、適用前に必ず
  同意ダイアログ（`MessageBox` Yes/No）を表示するよう `MainWindow.RunUpdateFlowAsync` を変更
  （設定画面の手動確認フローと同一の同意パターンに統一）
- `docs/requirements.md`（NFR-01, FR-ST-05, FR-ST-10）、`docs/architecture.md`、
  `docs/operation-manual.md` をADR-0009に合わせて更新

## 既知の課題

| ID | 内容 | 状態 |
| --- | --- | --- |
| ISS-001 | コード署名証明書が未手配。インストーラー実行時に SmartScreen 警告が出る | **未解決（優先度: 中）**。SmartScreen 対策としてのみ有効。EDR の行動検知は署名の有無で判定しないため、本課題は ISS-008 の解決策ではない（ADR-0009 参照、2026-08-05 中村勝 判断）。運用では手順書の案内で回避 |
| ISS-002 | 半透明方式の描画品質 | ADR-0004 で解決済み（要実機確認） |
| ISS-003 | 巨大文書でのプレビュー遅延 | デバウンス実装済み。実測で再評価 |
| ISS-004 | IME 変換中の補完誤爆 | `Key.ImeProcessed` で抑止。実機確認が必要 |
| ISS-005 | Topmost が他アプリのフルスクリーン後に外れる場合がある | UAT で確認し、必要なら定期再適用 |
| ISS-006 | 10,000行文書の再描画が約2.4秒（CI実測）かかりNFR-04未達 | **解決済み**: ADR-0005の暫定策（5,000行超で自動プレビュー一時停止）を恒久仕様として確定。想定文書規模は数百行のためADR-0005以上の対策は不要（2026-08-05確定） |
| ISS-007 | v0.2.0実機で、更新直後に再起動すると `FileLoadException`（アクセス拒否）で起動不能になる場合がある | ADR-0008で暫定対策（`restart:true`）を実施したが、v0.2.1実機（SN11, EDR: Cybereason導入）で再発。真因はEDRによる誤検知と判明（ISS-008参照）。**根本対策としてADR-0009へ移行、解決済み**（2026-08-05） |
| ISS-008 | EDR（Cybereason）が、`%LocalAppData%`配下の未署名・自己書き換え実行ファイル（Velopackのper-userインストール）を高リスクと判定し、通知なしにDLLロードをブロック（`ACCESS_DENIED`）する | **解決済み**: ADR-0009でインストール先をProgram Files（per-machine, MSI）に変更し、更新の適用も無人自動ではなくユーザー同意＋UAC確認を必須化（2026-08-05） |

## v0.3.0 リリース前チェックリスト（未実施）

ADR-0009 の変更は配布方式そのものを変えるため、リリース時に以下の実機確認が必要。

- [ ] Release ワークフローが `.msi` を生成し、GitHub Releases のアセットとして
      アップロードされることを確認（`vpk pack --msi` の出力が `vpk upload github` の
      対象に含まれるかは実行して確認する）
- [ ] 旧 v0.2.x（per-user）をアンインストールしてから `.msi` でインストールできること
      （操作説明書 3.0 節の移行手順どおり）
- [ ] SN11（EDR: Cybereason 導入端末）で正常に起動すること = ISS-008 の解消確認
- [ ] 設定（`%APPDATA%\MDAsisst\settings.json`）が移行後も引き継がれること
- [ ] v0.3.0 → v0.3.1 の更新で、同意ダイアログ → UAC → 再起動 の流れが成立すること
