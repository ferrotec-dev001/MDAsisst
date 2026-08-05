# MDAsisst

Markdown の書き方を半透明ウィンドウで表示してくれるサポートソフト（Windows / C# / WPF）。

## 特徴

- 画面の隅に常駐する**半透明ウィンドウ**（透過度・色・フォントを自由に設定）
- **チートシート**と「記載後の効果」表示、クリックでスニペット挿入
- 入力に追従する**リアルタイムプレビュー**（WPF FlowDocument、完全オフライン）
- **入力サポート**（リスト自動継続、Ctrl+B/I/K の装飾トグル、表の雛形）
- 編集時に自動展開／非編集時に画面隅へ自動最小化（切替時間も設定可）
- タスクトレイ常駐、Windows 起動時の自動起動
- インストーラー配布と GitHub Releases 連携の自動更新（自動／手動／不要）

## ドキュメント（MD駆動設計）

本プロジェクトは設計・要件をすべて Markdown で管理し、AI・開発者の双方が参照できるようにしている。

| ファイル | 内容 |
| --- | --- |
| [`Agent.MD`](Agent.MD) | AI・開発者が守る制約（機密保持ほか） |
| [`docs/requirements.md`](docs/requirements.md) | 要件定義書 |
| [`docs/architecture.md`](docs/architecture.md) | アーキテクチャ設計書 |
| [`docs/adr/`](docs/adr) | 重要な設計判断の記録（ADR） |
| [`docs/test-report.md`](docs/test-report.md) | テスト実施報告 |
| [`docs/release.md`](docs/release.md) | リリース手順 |
| [`docs/tasks.md`](docs/tasks.md) | 開発タスクと進捗 |

## ビルド

```powershell
dotnet build MDAsisst.sln
dotnet test  MDAsisst.sln
dotnet run --project src/MDAsisst.App/MDAsisst.App.csproj
```

必要環境: Windows 10/11 + .NET 8 SDK
