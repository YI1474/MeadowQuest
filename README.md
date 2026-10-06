# Meadow Quest

Unity / C#で制作した、モンスターを仲間にして育てる短編2D RPGです。
はじまりの街から草原・洞窟を抜け、岩峰の街の試練場で師範に挑みます。

## 主な機能

- タイル単位の探索、会話、建物への出入り、エリア移動
- 技選択・タイプ相性・交代・捕獲を含むターン制バトル
- 基本種10種類、最大6匹の手持ち、成長・並び替え
- 道具の使用・売買、所持金、回復所
- 対戦NPCとの連戦・再戦、師範撃破後のクリア表示とクレジット
- JSONによる手動セーブ・ロード（自動セーブなし）

## 制作範囲と使用ツール

個人制作として、ゲーム仕様の選択・調整、Unity上でのプレイ確認、不具合の報告と改善方針の判断を行っています。
コードとエディターツールの作成・修正にはCodex、モンスターの背面画像制作には画像生成AIを使用しています。人物・地形・モンスター等には外部素材を使用しています。

## 設計上の工夫

- **CSVからゲーム用データへ変換**：種族・技・道具を表形式で管理し、エディターツールでScriptableObjectへ取り込みます。パラメータ調整とゲーム処理を分けています。
- **共通定義と個体の状態を分離**：種族の共通データと、各個体のHP・経験値・習得技などを別に管理します。
- **計算と画面表示を分離**：戦闘の状態遷移、ダメージ、成長計算をUnityの画面に依存しないクラスにまとめ、コンソールテストで確認できるようにしています。
- **セーブ読み込み時の検証**：形式のバージョン、手持ち数、所持金などを検証してからプレイ状態へ反映します。
- **シーン制作の補助**：マップ構築・修復・データ検証をエディターメニューから実行できます。生成したシーン・Prefab・タイルはAssets以下に保存します。

現在は小規模作品として、街や洞窟を同じシーン内の別座標へ配置しています。戦闘以外の進行管理もBattleControllerに集まっているため、今後の拡張時には責務の分離が改善点になります。

## 起動方法

| 項目 | バージョン |
|---|---|
| Unity | 6000.6.2f1 |
| Input System | 1.20.0 |
| Unity UI | 2.6.0 |

1. リポジトリをクローン、またはZIPで取得・展開します。
2. Unity Hubから、`Assets`・`Packages`・`ProjectSettings`が入っているフォルダを追加します。
3. パッケージの復元とインポート完了後、`Assets/Game/Scenes/Meadow.unity`を開きます。
4. 再生し、Gameビューをクリックして操作します。

通常の起動ではBuild/Setupメニューの再実行は不要です。

## 操作

| 操作 | 入力 |
|---|---|
| 移動 | WASD / 矢印キー |
| 話す・会話を進める | E / Enter |
| メニュー | Tab / M / Esc |
| 戻る・閉じる | Esc / 画面のボタン |
| 戦闘コマンド・道具・仲間の選択 | 画面のボタン |
| 戦闘の導入文を進める | クリック / Enter / Space |
| 保存 | メニューの「セーブ」 |

## コードの入口

| 主なクラス | 役割 |
|---|---|
| [KeyboardMoveInput](Assets/Game/Scripts/KeyboardMoveInput.cs) / [GridMover](Assets/Game/Scripts/GridMover.cs) / [GridMap](Assets/Game/Scripts/GridMap.cs) | 入力・移動・通行判定 |
| [BattleController](Assets/Game/Scripts/BattleController.cs) / [BattleView](Assets/Game/Scripts/BattleView.cs) | 戦闘進行とUI |
| [BattleSession](Assets/Game/Scripts/BattleSession.cs) / [BattleMath](Assets/Game/Scripts/BattleMath.cs) / [Progression](Assets/Game/Scripts/Progression.cs) | 状態遷移・戦闘・成長計算 |
| [MonsterIndividual](Assets/Game/Scripts/MonsterIndividual.cs) / [MonsterParty](Assets/Game/Scripts/MonsterParty.cs) | 個体と手持ち |
| [Inventory](Assets/Game/Scripts/Inventory.cs) / [ShopRules](Assets/Game/Scripts/ShopRules.cs) | 所持品と売買 |
| [ProgressSave](Assets/Game/Scripts/ProgressSave.cs) | 保存・読み込み・検証 |
| [Editor](Assets/Game/Editor) | シーン作成・修復・CSV取り込み |

詳しい編集手順は[開発用ガイド](DEVELOPMENT.md)、計算・状態遷移のテストは[テスト手順](Tests/README.md)を参照してください。

## 動作確認と既知の課題

開発PCのUnity Editorで起動・操作を確認しています。別PCでの初回インポートと、配布用実行ファイルのビルド・動作は未確認です。

2026-10-06：C#ソースの整形後にコンパイルを確認し、同梱のコンソールテスト34項目が成功しました。今回の変更後のUnity実プレイは別途確認が必要です。

- 複数体との戦闘で経験値獲得メッセージが見えないという報告があり、表示の確認が残っています。
- コンソールテストは計算と状態遷移を対象とし、UnityのUIやシーン参照の動作は対象外です。

## 素材とライセンス

素材の出典・ライセンス・改変内容は[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)に記載しています。
本リポジトリ全体への一律のオープンソースライセンスは設定していません。
