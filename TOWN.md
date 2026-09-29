# はじまりの街

64×44タイルの街を既存Meadowシーンに追加します。自宅、回復所、民家3軒、広場、池、街路樹、案内人と門番があり、5棟すべてに室内があります。
元の草原は維持し、街の西側と草原の東側の道で行き来できます。

## 反映

Unityの再生を停止し、コンパイルを待ちます。Meadowシーンが開いていれば初回に自動配置します。
反映されないときは Meadow Quest > Build > Town を実行してください。Ctrl+Sでシーンを保存します。
重複配置しないようTownWorldが存在する場合は処理を止めます。
建物・家具はHierarchyの Town - はじまりの街、地面はWorldのTilemapで編集できます。
建物はTownBuildingの通行範囲と入口アンカーを持つPrefabとして整理します。建物を移動すると入口・帰還地点・通行範囲も追従します。建物は整数座標へスナップして移動し、回転やスケール変更は行わないでください。
家具・住民の位置はまだ固定セルの通行判定と対応させているため、それらの移動時はGridMapのOccupied CellsとTownWorldのResidentsも合わせて更新します。

## 遊び方

- スタート位置は中央通り。カメラはプレイヤーを追従します。
- 北西が自宅、北が回復所。屋根の下の玄関手前へ歩くと入れます。
- 室内の下中央のマットが出口です。出入り直後の移動停止は1フレームだけで、キーを離す必要はありません。
- 人に隣接してE/Enterで会話、もう一度押すと閉じます。
- 自宅の家族、回復所の係員に話すと仲間のHP/PPを回復し、個体データを保存します。
- 民家では育成と街の案内を聞けます。自宅・回復所以外の家具に個別操作はありません。
- メニューのセーブで現在地を保存します。保存位置が未設定または通行不可の場合は街の中央から始まります。

## 構成

シーン遷移ではなく、同じシーンの離れた位置に室内を配置し、出入口でプレイヤーを移動します。
ランタイムに建物や地形を生成しません。TownBuilderが編集時に配置し、保存したシーンをゲームで使用します。
既存の草原会話Controllerは無効にし、同じ会話Canvasを街・草原共通のTownWorldで扱います。
生成時には、各出入口の通行可否とスタートからの到達性を検証します。
外観と家具は既存のGuardian MonstersアトラスからSpriteとして切り出しています。完成した美術ではなく、素材の組み合わせや描画順は実プレイで調整する余地があります。

既存の街の修復：再生停止中に Meadow Quest > Repair > Town Visuals And Doors を実行し、Ctrl+Sで保存します。
修復は建物・家具の位置を保持し、屋根の描画範囲と描画順を更新します。室内に色分けと出口マットを追加します。
プレイヤーと街のSpriteは1000−Y×2の描画順に統一し、遠くに配置された室内でも床より前に表示します。

## 構成の点検・整理

Meadow Quest > Setup > Town Authoring：5棟の外観をPrefab化し、室内をRoomオブジェクトへ整理します。
初回は自動実行を試みます。既存シーンの保存済み状態をBackups/Meadow-before-authoring-review.unityに一度だけコピーします。
PrefabはAssets/Game/Prefabs/Town/Building_*.prefabへ保存します。既存の同名Prefabは上書きしません。
Entrance/Return Pointは建物の子、Arrival/Exitは室内の子です。roomIdで対応づけます。
建物にシーン上の室内への直接参照を持たせないので、Prefabを使い回せます。複製時はroomIdを一意に設定して対応する室内を用意してください。
新規建物の追加後はTownWorldのBuildings/RoomsとGridMapのBuildingsにも登録してください。
室内の地面Tilemapは共通Worldに残っています。Roomを動かす場合はTilemap、住民、通行セルも一緒に編集する必要があります。

Meadow Quest > Validate > Town：入口重複、通行可否、開始位置からの到達性、各移動先からの帰還、住民への隣接、Prefab接続、Sprite参照を確認します。
入口セルに到達したら必ず移動する挙動を再現し、「入口を通り抜ければ帰れる」という誤った判定を避けています。
結果はプロジェクト直下のTownAudit.txtへ記録します。これは実シーンを使う編集時検証であり、画面操作のプレイテストとは別です。
整理後はCtrl+Sでシーンを保存してください。現在は一つのシーンのまま、責務とHierarchyを整理しています。

素材: Guardian Monsters Artwork by Georg Eckert / lucidtanooki, CC BY 4.0.
Source: https://github.com/limbusdev/guardian_monsters_artwork
使用: tilesets/limbusdev_world2.png。Unity Sprite切り出し・拡縮・配置。ライセンスはAssets/Game/Art/Town/SOURCE-LICENSE.md。

