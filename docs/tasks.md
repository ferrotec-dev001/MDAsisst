# MDAsisst 開発タスク・進捗（MD駆動設計）

最終更新: 2026-08-06

## Issue #11〜#16 対応（2026-08-06, v0.3.1）

UAT後レビューで登録された6件のGitHub Issueすべてに対応した。詳細は **ADR-0010** を参照。

| Issue | 内容 | 状態 |
| --- | --- | --- |
| #11 | タイトルバーの太字/斜体/リンク/表アイコンが不要 | 解決（削除。Ctrl+B/I/K・チートシートで代替） |
| #12 | フォント色・ウィンドウ色がエディタ/プレビュー以外に反映されない | 解決（タイトルバー・チートシート・設定画面の固定背景色を透過化） |
| #13 | MD記法一覧アイコンのポップアップコメントがない | 解決（記法名・入力例・説明のツールチップを追加） |
| #14 | 自動小型化後の復帰でウィンドウ大きさが変化する | 解決（`SettingsValidator`に座標異常値[-32000付近]のガードを追加） |
| #15 | 透明度設定が無効 | 解決（`SettingsWindow`にもライブ透過度反映処理を追加） |
| #16 | メイン/設定ウィンドウをWPF Fluentデザインに | 解決（`Themes/FluentTheme.xaml`新設。ADR-0004の半透明化方式は維持） |

- 対応コミット: `0eedf52`
- リリース: v0.3.1（本ドキュメント更新と同時にタグ付け・公開）

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

## Issue #11〜#16 対応（2026-08-06）

v0.3.0 リリース後のレビューで登録された6件のGitHub Issueすべてに対応した。詳細は **ADR-0010** を参照。

| Issue | 内容 | 状態 |
| --- | --- | --- |
| #11 | タイトルバーの太字/斜体/リンク/表アイコンが不要 | 解決（4ボタンを削除。既存の Ctrl+B/I/K ショートカットとチートシートで代替） |
| #12 | Setting内のフォント色・ウィンドウ色がエディタ/プレビューにしか反映されない | 解決（タイトルバー・チートシート・設定画面パネルの固定背景色を透過に変更） |
| #13 | MD記法一覧アイコンのポップアップコメント | 解決（記法名・入力例・説明を表示するツールチップを追加） |
| #14 | 自動小型化後の復帰でウィンドウ大きさが変化する | 解決（`-32000`付近の異常座標を`SettingsValidator`で検知し既定値へ丸める） |
| #15 | 透明度設定が無効（メイン/設定ウィンドウとも） | 解決（`SettingsWindow`にもライブ反映用の透過度適用処理を追加） |
| #16 | メイン/設定ウィンドウのデザインをWPF Fluentに | 解決（`Themes/FluentTheme.xaml`を新設し、配色トークン・角丸・アクセントカラー・ホバー/プレス遷移を適用。ADR-0004の半透明化方式は維持） |

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
| ISS-009 | v0.3.0 のリリース資産に、per-user 方式の `Ferrotec.MDAsisst-win-Setup.exe` と `Ferrotec.MDAsisst-win-Portable.zip` が Velopack により自動生成・添付されている。利用者が誤ってこれらを実行すると `%LocalAppData%` にインストールされ、ISS-008（EDRブロック）が再発する | **対応中**: ADR-0012に基づき`release.yml`の`vpk pack`に`--noInst --noPortable`を追加し、生成自体を止めるコード変更を実施（2026-08-06）。`--msi`との組み合わせがWindowsランナー上で問題なく動作するかは次回リリース実行時に確認する |
| ISS-008 | EDR（Cybereason）が、`%LocalAppData%`配下の未署名・自己書き換え実行ファイル（Velopackのper-userインストール）を高リスクと判定し、通知なしにDLLロードをブロック（`ACCESS_DENIED`）する | ADR-0009でインストール先をProgram Files（per-machine, MSI）に変更したが、**ISS-010のとおり更新の自己適用局面で類似事象が再発**したため「解決済み」の判定を撤回。真因（未署名アプリの自己書き換えという挙動）への対策はADR-0011に統合 |
| ISS-010 | v0.3.1実機で、オンライン自動更新（アプリ内の確認→同意→ダウンロード→`ApplyAndRestart`）実行直後に`FileLoadException`（アクセス拒否）で起動不能になる。同一端末でMSIをクリーンインストールした場合は正常起動することを確認済み | **解決済み**: ADR-0011で、アプリ自身によるダウンロード・自己適用の経路（`DownloadAsync`/`ApplyAndRestart`）を完全に廃止。更新は新バージョン通知＋リリースページ誘導のみとし、実際の適用はMSIの手動再インストールに一本化（2026-08-06） |

## v0.3.0 リリース前チェックリスト（未実施）

ADR-0009 の変更は配布方式そのものを変えるため、リリース時に以下の実機確認が必要。

- [x] Release ワークフローが `.msi` を生成し、GitHub Releases のアセットとして
      アップロードされることを確認 → **確認済み（v0.3.0, 2026-08-05）**。
      実際のアセット名は `Ferrotec.MDAsisst-win.msi`（62.0MB）。
      ただし per-user 用の `Ferrotec.MDAsisst-win-Setup.exe` と
      `Ferrotec.MDAsisst-win-Portable.zip` も同時に生成・添付される（ISS-009 参照）
- [ ] 旧 v0.2.x（per-user）をアンインストールしてから `.msi` でインストールできること
      （操作説明書 3.0 節の移行手順どおり）
- [ ] SN11（EDR: Cybereason 導入端末）で正常に起動すること = ISS-008 の解消確認
- [ ] 設定（`%APPDATA%\MDAsisst\settings.json`）が移行後も引き継がれること
- [ ] v0.3.0 → v0.3.1 の更新で、同意ダイアログ → UAC → 再起動 の流れが成立すること

## v0.3.3: per-user資産の生成停止（ISS-009, ADR-0012）

- 1回目の実行（`vpk pack` に `--noInst --noPortable` を追加）は **Pack ステップで失敗**。
  Velopack CLI 1.2.0は `--noInst` と `--noPortable` の同時指定に対応しておらず、
  `Cannot use '--noPortable' and '--noInst' options together` で停止することが判明した。
- 対策として `vpk pack` は ADR-0009 時点の構成（`--msi --instLocation PerMachine` のみ）に戻し、
  代わりに Pack 成功後・Upload 実行前に `Setup.exe` / `Portable.zip` を出力フォルダから
  削除するステップ（`Remove per-user distributables`）を追加。2回目の実行で成功し、
  GitHub Releases のアセットが `.msi` / `.nupkg`（full・delta）/ `RELEASES` 系ファイルのみに
  なることを確認した。詳細は **ADR-0012**（検証結果を追記済み）を参照。
- 残作業: `docs/operation-manual.md` 3.1節の「使用禁止アセット」に関する警告文を
  「そもそも存在しない」旨に簡略化する。

## 追加タスク・不具合修正 (ISS-017)

- [x] **ISS-017: Program Files 配下のファイル（MDAsisst.dll等）が管理者権限でも削除できない問題の解消** (2026-08-07, v0.3.4)
  - 原因（Velopack 1.2.0のWiXテンプレートを実ソース調査の上で特定）: MSI自体は明示的なACL/パーミッション定義を行っておらず、
    トレイ常駐仕様（ADR-0010）により旧プロセスがファイルハンドルを保持し続けていたことがロック起因のアクセス拒否の真因。
  - 対策1: `VelopackApp` の `OnBeforeUninstallFastCallback` / `OnBeforeUpdateFastCallback` フックで、
    アンインストール・アップグレード直前に常駐中の他プロセスを確実に終了。
  - 対策2: `PublishSingleFile` を有効化し `MDAsisst.dll` を `MDAsisst.exe` に統合、管理対象ファイル数を削減。
  - 詳細は **ADR-0013**（内容改訂版）を参照。
