# ADR-0009: インストール先を Program Files（per-machine）に変更し、更新は同意必須とする

- ステータス: 承認済み
- 決定日: 2026-08-05
- 決定者: 中村勝（株式会社フェローテック）
- 関連: ADR-0003（配布・更新に Velopack を採用）、ADR-0008（更新適用の再起動レース対策）、ISS-007

## 背景

v0.2.0 / v0.2.1 において、フェローテック社内の管理端末（SN11）で以下の障害が発生した。

- インストール後（クリーンインストール後も含む）に `MDAsisst.exe` が
  `System.IO.FileLoadException: ... MDAsisst.dll ... アクセスが拒否されました。` で起動不能になる。
- Smart App Control はオフ、Windows Defender の保護履歴にも隔離記録なし、`Unblock-File`
  （MOTW 解除）でも解消しない。
- 端末には EDR **Cybereason** が導入されている。

ADR-0003 採用の Velopack は「`%LocalAppData%` への per-user インストール・管理者権限不要」を
最大の利点としていたが、この設計そのものが今回の障害の温床になっていた。

- `%LocalAppData%` 配下という**ユーザー書き込み可能領域**に、
- **コード署名のない**実行ファイルが配置され、
- アプリ自身がその場所の DLL を**自己書き換え**する（Velopack の更新機構）。

この3条件の組み合わせは、正規の自動更新アプリと自己複製・永続化を試みるマルウェアの双方に
共通する挙動パターンであり、EDR の行動検知が高リスクと判定し、通知なしに DLL ロードを
ブロックする（`ACCESS_DENIED`）ケースがあることを確認した。加えて、更新の適用（旧
`ApplyOnExit`、ADR-0008 で対策済みのレースコンディションを含む）が**ユーザーの目に見えない
バックグラウンドで**行われる設計自体も、社内セキュリティ運用上望ましくないと判断した。

## 決定

1. **インストール先を Program Files（per-machine）に変更する。**
   `vpk pack` に `--msi --instLocation PerMachine` を付与し、MSI インストーラーで
   `Program Files\Ferrotec Corporation\MDAsisst` へ配置する。per-machine インストールは
   HKLM レジストリを使用するため、**インストール自体に管理者権限（UAC）が必須**になる。
   配布・案内には `.msi` を用いる（`Setup.exe` は Velopack の仕様上 per-user 固定であり、
   本決定の対象外とする）。
2. **更新の適用は必ずユーザーの同意を得てから行う。無人の自動適用は行わない。**
   - `IUpdateService.ApplyOnExit`（アプリ終了時に裏で自動適用する経路）を削除する。
   - 「自動」モードは「バックグラウンドでの更新**確認**」のみを意味するよう再定義する。
     確認の結果、更新が見つかった場合は必ず確認ダイアログ（Yes/No）を表示し、
     同意した場合のみダウンロード・`ApplyAndRestart` を実行する。
   - 「手動」モードは従来どおり、設定画面の「更新を確認」ボタンで同じ同意フローに入る。
   - 適用（`ApplyAndRestart`）は Program Files への書き込みを伴うため、実行時に Windows の
     UAC 確認が表示される。アプリ内の同意ダイアログと UAC の二重の同意ゲートになる。

## 理由

- Program Files への per-machine インストールは HKLM 管理・署名検証の慣行に沿い、
  EDR／SmartScreen が「ユーザー領域での未署名の自己書き換え」として警戒するパターンから外れる。
  ISS-001（コード署名未手配）の恒久解決までの緩和策として有効。
- インストール・更新のいずれも管理者権限の確認（UAC）を経ることになり、「無人で気づかぬうちに
  実行ファイルが差し替わる」という状態が構造的になくなる。
- 更新のダウンロード・適用前にアプリ内の同意ダイアログを必須にすることで、UAC 表示前にも
  ユーザーが「何のための管理者権限確認か」を理解できる。

## 影響（既存要件・実装との差分）

- **NFR-01 を変更する**: 「管理者権限なしでインストールできる」は撤回し、「管理者権限
  （per-machine インストール）を要求し、Program Files に配置する」に置き換える
  （`docs/requirements.md` NFR-01 参照）。
- **FR-ST-05 を変更する**: 「自動」時に「適用して次回起動時に反映する」（無人適用）としていた
  記述を、「確認のみ自動で行い、適用はユーザーの同意後」に修正する。
- **FR-ST-10 を新設**: インストール先が Program Files（per-machine, 管理者権限必須）であることを
  明文化する。
- ADR-0008 の対策（`WaitExitThenApplyUpdates(restart: true)`）は、`ApplyOnExit` 自体を廃止した
  ことで**適用対象がなくなり事実上凍結**する。ADR-0008 は歴史的記録として残すが、現行実装は
  本 ADR に従う。
- `docs/operation-manual.md` のインストール手順・更新手順を、`.msi` の利用と UAC 確認の説明に
  更新する。

## 検証済み事項（Velopack 1.2.0 ソース確認、2026-08-05）

本決定は「Program Files に置くと Velopack の更新が動かなくなるのではないか」という懸念を伴うため、
採用バージョン（`vpk` / Velopack **1.2.0**）の実装を確認し、以下を確定した。

| 確認項目 | 結果 | 根拠 |
| --- | --- | --- |
| `--msi` / `--instLocation` が 1.2.0 で利用可能か | **可能** | `src/vpk/Velopack.Vpk/Commands/Packaging/WindowsPackCommand.cs` に両オプションの定義あり |
| `PerMachine` の指定値が存在するか | **存在する** | `InstallLocation` enum は `None / PerUser / PerMachine / Either` |
| `PerMachine` が Program Files に入るか | **入る** | `MsiBuilder` が `InstallForAllUsers = InstLocation.HasFlag(PerMachine)` を WiX テンプレートへ渡す |
| Program Files への更新適用が権限不足で失敗しないか | **失敗しない（自動昇格する）** | `src/bins/src/commands/apply_windows_impl.rs`: ルートディレクトリが書き込み不可の場合、`run_process_as_admin` で自分自身を昇格起動し、完了を最大10分待機する実装になっている |

すなわち per-machine インストールでも更新経路は成立し、その際に **UAC 昇格ダイアログが出る**。
これは本 ADR の狙い（更新を必ずユーザーの同意のもとで行う）と一致するため、仕様として許容する。

## 代替案と却下理由

- **現状維持（per-user + 無人自動適用）**: EDR 誤検知の再発リスクが残ったまま。却下。
- **コード署名証明書を先に取得して per-user のまま継続**: 恒久対策として引き続き有効
  （ISS-001 は本 ADR 後も残タスクとする）が、証明書の手配・予算確保には時間を要し、
  実機で起動できない状態を放置できないため、即応可能な本決定を先行させる。
- **MSIX 化**: 署名がほぼ必須で ISS-001 と同じ制約に当たるため、ADR-0003 と同様の理由で見送り。
