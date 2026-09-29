# Guardian Monsters 背面スプライト

ダウンロード素材の `sprites/monsters/128x128` 全100点（進化形・卵を含む）を対象にしています。作成状況は manifest.csv を参照してください。

100点を収録（新規生成98点、既存背面1点、前後共通の卵1点）。42_1 は元の形状を大きく変更した、灰色の丸い繭とピンクの模様の代替デザインです。正面画像とは形状が異なります。最終プロンプトは cocoon-redesign-prompt.json に収録しています。redesign-prompts.json の42_1の拒否記録は以前の試行の履歴です。

12_0・14_1・26_1・32_0・37_0・51_0 は、元の配色を参考に体や突起を丸めた簡略デザインで追加しました。正面画像と形状が一部異なります。今回の組み込み画像生成ツールへのプロンプトと結果は `redesign-prompts.json` に収録しています。

- `128x128`: Unity向けPNG。生成原寸から最近傍補間で縮小。
- `generated`: 組み込み画像生成ツールで作成した原寸画像。
- `prompts.json`: 素材番号別の生成プロンプト。
- `1_0`: 元素材に含まれる背面画像を使用。
- `0_0`: 卵は前後共通として元画像を使用。

新規背面はAI生成の派生画像です。元画像に描かれていない背中の模様や構造は推定で、公式の背面デザインではありません。正面とのドット密度・輪郭・向きに差があるため、応募作品の最終素材としては調整の余地があります。

## クレジット

Includes Guardian Monsters Artwork by Georg Eckert / lucidtanooki

Source: https://github.com/limbusdev/guardian_monsters_artwork

Original artwork: Creative Commons Attribution 4.0 (CC BY 4.0), https://creativecommons.org/licenses/by/4.0/

Changes: AI-generated rear-view derivatives and nearest-neighbor resizing. Original license is included as SOURCE-LICENSE.md. Credit the original author and indicate these modifications when distributing.

## MeadowQuest

`Assets/Game/Art/MonsterBacks` にゲーム用画像を配置。MonsterDefinition の Back Portrait が味方用、Portrait が敵用です。アオマルとクサトカゲは `Meadow Quest > Setup > Monster Back Sprites` で割り当てられます。既に設定済みの画像は上書きしません。

