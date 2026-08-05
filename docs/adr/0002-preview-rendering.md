# ADR-0002: プレビューは WPF FlowDocument で描画する

- ステータス: 承認済み
- 決定日: 2026-08-05
- 決定者: 中村勝

## 背景

Markdown プレビューの描画方式として、(a) WebView2 で HTML を表示、(b) WPF FlowDocument へ変換して表示、
の二案があった。非機能要件に「メモリ消費は最小」「インターネット未接続でも動作」が挙げられている。

## 決定

**WPF FlowDocument** 方式を採用する。Markdig で AST を生成し、自前レンダラで FlowDocument を構築する。
WebView2 は採用しない。

## 理由

- WebView2 は WebView2 Runtime（別プロセス、数十〜百MB超のメモリ）を要し、NFR-02（待機時120MB以下）と衝突する。
- WebView2 Runtime が未導入のPCではインストーラーでの同梱・オンライン取得が必要になり、
  オフライン要件（NFR-05）と配布サイズの両面で不利。
- 半透明ウィンドウ（AllowsTransparency=True）上では WebView2 等の子ウィンドウが正しく描画されない
  既知の制約があり、本アプリの中核要件（半透明表示）と技術的に相性が悪い。
- FlowDocument はフォント・文字色・背景色の設定反映が WPF のリソース機構でそのまま行え、
  アピアランス要件（FR-WN-05〜07）と親和性が高い。

## 影響

- HTML/CSS ほどの表現力は得られない。数式(LaTeX)・Mermaid 図・HTML 直書きは非対応とする（要件外）。
- シンタックスハイライトは自前で簡易実装するか、対応言語を限定する。
- レンダラは自作となるため、単体テスト（AST→FlowDocument 構造の検証）を必須とする。
- 将来 HTML エクスポートが必要になった場合は Markdig の HTML 出力を別経路で利用する。
