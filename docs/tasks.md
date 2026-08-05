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
- [ ] オフライン動作確認（NFR-05）
- [ ] 操作説明書（`docs/operation-manual.md`）の作成
- [ ] UAT シナリオ作成と実施 → 指摘対応

## 既知の課題

| ID | 内容 | 状態 |
| --- | --- | --- |
| ISS-001 | コード署名証明書が未手配。SmartScreen 警告が出る | 運用で回避（手順書に記載予定） |
| ISS-002 | 半透明方式の描画品質 | ADR-0004 で解決済み（要実機確認） |
| ISS-003 | 巨大文書でのプレビュー遅延 | デバウンス実装済み。実測で再評価 |
| ISS-004 | IME 変換中の補完誤爆 | `Key.ImeProcessed` で抑止。実機確認が必要 |
| ISS-005 | Topmost が他アプリのフルスクリーン後に外れる場合がある | UAT で確認し、必要なら定期再適用 |
