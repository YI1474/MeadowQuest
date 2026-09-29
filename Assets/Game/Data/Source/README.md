# マスターデータの編集

monsters.csv と moves.csv が編集元です。Excelでは CSV UTF-8 形式で保存してください。
Unityの再生を停止して Meadow Quest > Data > Validate CSV で検証し、Import CSV で反映します。
生成済みScriptableObjectの値を直接変えても次の取り込みで上書きされます。

- IDは変更しない識別子。小文字英数字とアンダースコアを使用します。
- monstersのmovesは技IDを | で区切ります（1〜4件）。
- front/backはSpriteとして読み込めるAssetsからのパスです。
- タイプは現在Normal/Grass/Fire/Water。相性は BattleMath.cs で定義（炎→草→水→炎が2倍、逆方向と同タイプは0.5倍、Normalは等倍）。
- baseHp/baseAttack/baseDefense/baseSpecialAttack/baseSpecialDefense/baseSpeedは1〜255の種族基本値です。levelは1〜100の初期レベルです。個体ごとの素質・鍛錬値・性格とレベルで実能力を計算します。
- 技のcategoryはPhysicalまたはSpecial。戦闘で最大4つの技から選択できます。技名・タイプ・物理/特殊・威力をボタンに表示します。命中率と威力が戦闘に反映されます。技実行時にPPを消費します。
- CSVから行を消しても既存アセットは削除しません。IDを変えると別アセットになるため、改名にはnameを使います。
- 既存のStarter/Wildアセットを更新するのでシーンの参照は維持されます。
- 入力エラーは書き込み前に検出します。ディスク障害など実行中の障害に対する完全なトランザクションではありません。Gitで変更を管理してください。


## 育成版の追加項目

baseExperienceは倒したときの経験値の基準値、evHp〜evSpeedは倒したときの鍛錬値（各0〜3）です。
育成・戦闘・保存の現行仕様はプロジェクト直下の GROWTH.md を参照してください。


出現地域は Scripts/EncounterRoster.cs に定義しています。Import CSV で草原と洞窟それぞれのリストに反映します。habitat=Custom の遭遇領域は自動更新しません。
