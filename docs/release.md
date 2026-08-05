# MDAsisst リリース手順

## 1. 前提

- Windows 上で作業する（WPF のため）
- .NET 8 SDK、`vpk` CLI（Velopack 1.2.0）
- `--packId` は **`Ferrotec.MDAsisst`** で固定。変更すると別アプリ扱いになり自動更新が切れる。

```powershell
dotnet tool install -g vpk --version 1.2.0
```

## 2. 手動リリース

```powershell
dotnet publish src/MDAsisst.App/MDAsisst.App.csproj -c Release -r win-x64 --self-contained true -o publish
vpk download github --repoUrl https://github.com/ferrotec-dev001/MDAsisst
vpk pack -u Ferrotec.MDAsisst -v 1.0.0 -p publish -e MDAsisst.exe --packTitle "MDAsisst" --packAuthors "Ferrotec Corporation" --channel win
vpk upload github --repoUrl https://github.com/ferrotec-dev001/MDAsisst --publish --releaseName "MDAsisst 1.0.0" --tag v1.0.0 --token $env:GH_TOKEN
```

## 3. 自動リリース（推奨）

`v1.0.0` 形式のタグを push すると `.github/workflows/release.yml` が実行され、
インストーラー生成と GitHub Releases への公開まで自動で行われる。

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## 4. 注意事項

| 項目 | 内容 |
| --- | --- |
| 差分更新 | `vpk download github` を必ず先に実行する。省くとフルパッケージ配布になる |
| バージョン | タグと `--packVersion` を一致させる（SemVer 準拠） |
| 再アップロード | 公開済みバージョンの上書きは禁止。修正時はパッチ版を上げる（チェックサム不整合の原因） |
| 未公開リリース | ドラフトのままだとクライアントから見えない。`--publish` を付ける |
| コード署名 | 未手配（ISS-001）。初回インストール時に SmartScreen 警告が出る前提で周知する |
