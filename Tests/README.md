# 戦闘・成長ルールのチェック

ゲーム本体の `BattleSession.cs`、`BattleMath.cs`、`Progression.cs` を直接参照するコンソールテストです。Unityのシーンや画像を読み込まずにルールを確認します。Unity Test Runner用ではありません。

## 実行

.NET 8 SDKを用意し、リポジトリのルートで実行します。

```sh
dotnet run --project Tests/MeadowQuest.Checks.csproj
```

外部NuGetパッケージは不要です。失敗時は例外と終了コードで通知します。成功時はチェック数を表示します。

確認対象は、ターン順序、二重入力、勝敗後の操作拒否、交代、捕獲、ダメージ補正、レベル境界、鍛錬値の上限です。UI表示、Unityの参照設定、セーブのファイル入出力、実行ファイルの動作は対象外です。
