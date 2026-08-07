# ADR-0013: Program Files 配下ファイルのアクセス拒否（削除不可）対策とファイル数削減

## Status
Accepted（2026-08-07 内容改訂）

## Context
v0.3.2 / v0.3.3 において、`--instLocation PerMachine`（Program Files配下）でインストールされたアプリケーションファイルの `current\MDAsisst.dll` が、管理者権限（Administrators／フルコントロール権限付与済み）であっても手動削除時に `UnauthorizedAccessException`（アクセス拒否・「Administratorsからアクセス許可を得る必要があります」ダイアログ）となる現象（ISS-017）が発生した。

### 調査結果（訂正）
当初、原因をWiX（MSI）のファイルパーミッション定義（PermissionEx等によるACLロックダウン）と推定していたが、Velopack 1.2.0が生成するWiXテンプレート（`MsiTemplate.hbs`）を実ソースで確認した結果、**ファイルコンポーネントに対する明示的なACL/PermissionEx定義は存在しない**ことを確認した。この点は前バージョンのADR記述の誤りであり、本改訂で訂正する。

真因として最も妥当性が高いのは、本アプリの仕様（ADR-0010: AutoVisibilityStateMachineによるトレイ常駐）である。ウィンドウを閉じてもプロセスが常駐し続けるため、`current\MDAsisst.dll` を含むアセンブリのファイルハンドルが保持され続ける。この状態でMSIのアンインストール／アップグレード処理（`RemoveFiles`/`InstallFiles`）や、ユーザーによる手動削除が行われると、ロックされたファイルに対してWindowsが「Administratorsからの許可が必要」という、実際のロック要因を正しく表さない拒否ダイアログを表示する。

## Decision
1. **常駐プロセスの確実な終了（根本対策）**
   `VelopackApp.Build().OnBeforeUninstallFastCallback(...) / .OnBeforeUpdateFastCallback(...)` フックを実装し、MSIのアンインストール・アップグレード実行直前に、常駐中の他の `MDAsisst` プロセスを検出して終了させる（`MsiTemplate.hbs` の `UninstallHookDeferred` カスタムアクションが `RemoveFiles` 前に対象exeを `--veloapp-uninstall` 引数付きで起動する仕組みを利用）。
2. **管理対象ファイル数の削減（ユーザー要望への対応）**
   `MDAsisst.App.csproj` に `PublishSingleFile` を有効化する条件付き `PropertyGroup`（Release構成かつRuntimeIdentifier指定時のみ）を追加し、メインアセンブリ `MDAsisst.dll` を `MDAsisst.exe` に統合する。これにより `current` フォルダの管理対象ファイルそのものを削減し、仮にロック等の問題が起きても影響ファイル数を最小化する。あわせて `DebugType=none` を設定しPDB生成を抑止する。

## Consequences
- アンインストール・アップグレード時に旧プロセスが確実に終了するため、ファイルロック起因のアクセス拒否が解消される。
- `MDAsisst.dll` が存在しなくなり（`MDAsisst.exe` に統合）、手動削除・EDRスキャン対象ファイルが削減される。
- 自己完結型シングルファイル化により配布物サイズは増加するが、Program Filesへの配置ファイル数削減とロック影響範囲の縮小を優先する。
- Velopackのdelta（差分）更新は、単一の大きな実行ファイル全体を対象とするためバイナリ差分効率がやや低下する可能性があるが、機能上の問題はない。
