# ADR-0004: 半透明表示は AllowsTransparency ではなく WS_EX_LAYERED で実現する

- ステータス: 承認済み
- 決定日: 2026-08-05
- 起票: 実装フェーズの技術調査で判明した制約による設計変更

## 背景

当初の実装骨子は WPF の `AllowsTransparency="True"` + `WindowStyle="None"` を使っていた。
実装前調査で、この方式には本アプリの要件と衝突する制約があることが分かった。

| 制約 | 本アプリへの影響 |
| --- | --- |
| ClearType が無効化され、テキストがグレースケール AA になる | **テキストエディタとして致命的**（可読性低下） |
| 子 HWND（WebView2 等）が描画されない | 将来 WebView2 を併用する余地が消える |
| GPU→システムメモリのコピーが発生し、再描画が重くなる | 入力ごとに再描画するエディタで CPU 負荷増（NFR-02/04） |
| RDP・低ティア環境でソフトウェアレンダリングへ降格 | 社内リモート環境で描画品質・速度が落ちる |

## 決定

`AllowsTransparency="False"` のまま、以下の組み合わせで半透明ウィンドウを実現する。

1. `WindowStyle="None"` + `WindowChrome`（`CaptionHeight=30`, `ResizeBorderThickness=8`）
   → ドラッグ移動・四辺リサイズ・Aero スナップを OS に任せる（FR-WN-02/03）。
2. `WS_EX_LAYERED` + `SetLayeredWindowAttributes(LWA_ALPHA)`
   → ウィンドウ全体の不透明度をユーザー設定値で可変にする（FR-WN-04）。
3. `WS_EX_TOOLWINDOW` を付与し Alt+Tab の一覧に出さない（常駐ツールとしての作法）。

実装は `src/MDAsisst.App/Interop/WindowEffects.cs`。

## 影響

- ClearType が維持され、日本語テキストの可読性が確保される。
- 「角丸＋背景が透ける」ような per-pixel 表現はできない。均一な透過のみ対応する。
- `ShowInTaskbar` を切り替えると WPF が HWND を作り直し、設定した拡張スタイルとアルファ値が失われる。
  → **`ShowInTaskbar` は起動時に決めて以後変更しない**。トレイ格納は `Hide()` で行う。
- Topmost は他アプリのフルスクリーンや UAC 後に外れることがある。UAT で挙動を確認し、
  必要なら `SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE)` の定期再適用を追加する（課題 ISS-005）。
