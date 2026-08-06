# ADR-0012: リリース資産から per-user 用 Setup.exe / Portable.zip の生成自体を止める

- ステータス: 承認済み
- 決定日: 2026-08-06
- 決定者: 中村勝（株式会社フェローテック）
- 関連: ADR-0009（Program Files per-machine化）, ADR-0011（更新の自己適用を廃止）, ISS-008, ISS-009

## 背景

ISS-009（v0.3.0以降のリリースで確認）: `vpk pack` は `--msi --instLocation PerMachine`
を指定していても、既定では per-user 方式の `Ferrotec.MDAsisst-win-Setup.exe` と
`Ferrotec.MDAsisst-win-Portable.zip` を同時に生成し、GitHub Releases のアセットとして
添付し続けていた。

利用者や配布担当者が誤ってこれらを実行すると、`%LocalAppData%` へ per-user
インストールされてしまい、ISS-008（EDR誤検知によるアプリ起動不能）が再発する。
これまでは操作説明書に「使用禁止」の注記を置く運用でしのいでいたが、
「そもそもクリック可能な形で存在させない」方が事故を根本的に防げる。

## 決定

当初は `vpk pack` に Velopack CLI 1.2.0 が提供する以下のフラグを追加する案とした。

```
vpk pack ... --msi --instLocation PerMachine --noInst --noPortable
```

- `--noInst`: per-user インストーラー（`Setup.exe`）の生成を止める
- `--noPortable`: ポータブル版（`Portable.zip`）の生成を止める

しかし v0.3.3 の実リリース実行（Windows GitHub Actions ランナー）で検証したところ、
Velopack CLI 1.2.0（現時点の最新安定版）は次のエラーで `vpk pack` 自体が失敗する
ことが判明した。

```
Cannot use '--noPortable' and '--noInst' options together, please choose one.
```

そのため、あらかじめ想定していたフォールバック案（下記「検証方針」参照）を正式な
決定として採用する。`vpk pack` コマンドは ADR-0009 の状態（`--msi --instLocation
PerMachine` のみ）に戻し、代わりに `vpk pack` 実行後・`vpk upload github` 実行前に
ワークフロー内で `Setup.exe` / `Portable.zip` を出力フォルダから削除するステップ
（`Remove per-user distributables (ISS-009, ADR-0012)`）を追加した。
`vpk upload github` は出力フォルダの中身をそのままアップロードする実装のため、
アップロード前に削除すればアセットとして公開されない。

一方で、次のファイルは引き続き生成・アップロードする。

- `.msi`（配布・インストールに使う唯一の資産）
- full/delta の `.nupkg`、`RELEASES`、`releases.win.json`

理由: ADR-0011 でアプリの自己適用（ダウンロード＋上書き）は廃止したが、
「新バージョンの有無を確認する」（`VelopackUpdateService.CheckAsync`、
Velopackの `UpdateManager.CheckForUpdatesAsync`）は維持しており、これは
GitHub Releases 上の nupkg／マニフェストを読みに行く実装のため、
これらのファイルを消すと更新通知機能自体が壊れる。

## 検証結果（v0.3.3）

- 1回目の実行: `vpk pack ... --noInst --noPortable` は Pack ステップで失敗
  （上記エラー）。`.msi` が生成される前の段階で停止するため、`--msi` への
  影響有無自体を確認できなかった。
- 2回目の実行: `vpk pack` を素の `--msi --instLocation PerMachine` に戻し、
  Pack成功後に `Setup.exe` / `Portable.zip` を削除するステップを追加した
  構成で成功。GitHub Releases に `.msi` / `.nupkg`（full・delta）/
  `RELEASES` 系ファイルのみが公開されることを確認した。

## 影響

- リリースアセットが `.msi` / `.nupkg`（full・delta）/ `RELEASES` 系ファイルのみになり、
  配布担当者が迷わず `.msi` だけを配布できるようになる。
- 操作説明書の「使用禁止アセット」の注記は、アセットが存在しなくなるため
  リリースノート上の説明として簡略化する。
