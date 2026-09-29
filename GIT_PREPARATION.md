# Git用コピーの準備状況

人物と基本地形を出典・条件の明確な素材へ差し替えました。
Unityで再インポート後の表示確認とシーン保存を済ませてから、公開用コピーを確定してください。

## コピーするもの

- Assets 全体（ファイル・フォルダの.metaも含む）
- Packages（manifest.json、packages-lock.json）
- ProjectSettings
- Tests
- ルートの説明書 .md
- .gitignore

Library、Temp、Logs、UserSettings、Backups、ビルド成果物、検証出力.txtは不要です。
Unity上で再生を停止し、シーンとアセットを保存してからコピーします。
コピー先をUnity Hubに追加し、再インポートと起動・保存・戦闘・クリアまで検証してください。
OSのコピーで.metaを保持します。UnityのExport Packageだけではプロジェクト設定を移せません。

## コピー後

コピー先でGitを初期化し、追加予定ファイルを確認します。
その後はコピー先を開発元にすると、二つのフォルダの変更が分岐する事故を避けられます。
Gitへの登録とGitHub等へのアップロードは別の操作です。
.gitignoreは既に追跡されたファイルを自動では除外しません。
