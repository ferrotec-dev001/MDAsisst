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

`vpk pack` に Velopack CLI 1.2.0 が提供する以下のフラグを追加し、
per-user 用アセットの生成自体を止める。

```
vpk pack ... --msi --instLocation PerMachine --noInst --noPortable
```

- `--noInst`: per-user インストーラー（`Setup.exe`）の生成を止める
- `--noPortable`: ポータブル版（`Portable.zip`）の生成を止める

一方で、次のファイルは引き続き生成・アップロードする。

- `.msi`（配布・インストールに使う唯一の資産）
- full/delta の `.nupkg`、`RELEASES`、`releases.win.json`

理由: ADR-0011 でアプリの自己適用（ダウンロード＋上書き）は廃止したが、
「新バージョンの有無を確認する」（`VelopackUpdateService.CheckAsync`、
Velopackの `UpdateManager.CheckForUpdatesAsync`）は維持しており、これは
GitHub Releases 上の nupkg／マニフェストを読みに行く実装のため、
これらのファイルを消すと更新通知機能自体が壊れる。

## 検証方針

`--noInst` が `--msi` のビルド過程に影響しないことは、Velopackの内部実装上は
MSIがWiX 5テンプレートから独立して生成される設計だが、リリースパイプラインは
Windows専用GitHub Actionsランナーでしか実行できないため、次回リリース
（v0.3.3想定）の実行結果で最終確認する。

- 万一 `.msi` の生成に悪影響が出た場合のフォールバック: `--noInst --noPortable`
  を外し、代わりに `vpk pack` 実行後・`vpk upload github` 実行前に
  ワークフロー内で `Setup.exe` / `Portable.zip` を明示的に削除するステップを追加する。

## 影響

- リリースアセットが `.msi` / `.nupkg`（full・delta）/ `RELEASES` 系ファイルのみになり、
  配布担当者が迷わず `.msi` だけを配布できるようになる。
- 操作説明書の「使用禁止アセット」の注記は、アセットが存在しなくなるため
  リリースノート上の説明として簡略化する。
