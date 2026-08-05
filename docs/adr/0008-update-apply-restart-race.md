# ADR-0008: 更新適用と再起動のタイミングを Velopack 管理下に一本化する

- ステータス: 承認済み
- 決定日: 2026-08-05
- 決定者: 中村勝
- 関連: ADR-0003（Velopack採用）, ISS-001（未署名）, ISS-007（本ADRで新規記録）

## 背景

v0.2.0 配布後、以下の障害が実機（Windows, admin ユーザー）で発生した。

```
System.IO.FileLoadException: Could not load file or assembly
'...\Ferrotec.MDAsisst\current\MDAsisst.dll'. アクセスが拒否されました。
```

WER（Windows エラー報告）ログから、Windows Defender 等による隔離ではないことを確認済み。
本例外は CLR がエントリアセンブリ（MDAsisst.dll）をロードする最中に発生しており、
アプリのコード（`DispatcherUnhandledException` 等）が一切介入できない段階の失敗である。

原因は次の実装にある：

```csharp
_manager.WaitExitThenApplyUpdates(info.TargetFullRelease, silent: true, restart: false);
```

`restart: false` は「アプリ終了後、裏で `current` フォルダへ新バージョンを展開するが、
再起動はしない（ユーザーの次回手動起動を待つ）」という指定である。
このため、展開処理（ファイルコピー）が完了しきる前にユーザーがショートカット等から
MDAsisst を再度起動すると、`current\MDAsisst.dll` が書き込み中の状態でロードされ、
「アクセスが拒否されました」で異常終了する。これは再現条件が「更新直後に素早く再起動した場合」
に限られるため、通常のテストでは顕在化しにくい（ISS-007として記録）。

## 決定

`ApplyOnExit` の実装を `restart: true` に変更し、更新適用〜再起動のライフサイクル全体を
Velopack に一任する。

```csharp
_manager.WaitExitThenApplyUpdates(info.TargetFullRelease, silent: true, restart: true);
```

これにより、新バージョンの展開が完全に終わった後にのみ Velopack 自身がアプリを再起動する。
ユーザーが手動でショートカットを叩いて再起動する余地（＝展開途中のファイルを掴みに行く余地）
を構造的に排除する。

## 検討した代替案

| 案 | 却下理由 |
| --- | --- |
| `ApplyUpdatesAndRestart` をダウンロード直後に即時実行 | 「更新はアプリ終了時に適用し、業務を中断しない」(FR-ST-08想定の運用) に反し、作業中に強制再起動してしまう |
| アプリ起動時に更新中フラグを見て待機 | 例外が CLR のアセンブリロード時点で発生するため、アプリ内コードでは検知・待機ができない |
| 静音インストーラーへの単純な移行（MSI等） | ADR-0003の結論を覆すほどの理由がなく過剰 |

## 影響

- ユーザー体験: 更新後の自動再起動は「アプリを閉じた後」にのみ発生し、作業中に割り込まない
  （Velopackが展開完了を待ってから起動するため、閉じてから数秒〜十数秒後に新バージョンが立ち上がる）。
- `docs/operation-manual.md` に「更新直後は自動的に新バージョンが起動すること」を追記する。
- ISS-001（コード署名未手配）は本件の直接原因ではないと判明したため、優先度は据え置き
  （SmartScreen警告の運用回避は引き続き必要）。
- 既知の課題に ISS-007 として本事象と対策を記録する。
