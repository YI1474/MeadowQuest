using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class TownAuthoringAudit
    {
        static readonly string[] Names={"自宅","回復所","民家・庭師","民家・研究家","民家・旅人"};
        static readonly string[] Ids={"home","clinic","gardener","researcher","traveler"};
        static TownAuthoringAudit()
        {
            EditorApplication.delayCall+=Upgrade;
            EditorSceneManager.sceneOpened+=(scene,mode)=>Upgrade();
        }
        static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(town && town.authoringVersion<1) Apply();
        }
        [MenuItem("Meadow Quest/Setup/Town Authoring")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); if(!town) return;
            if(town.authoringVersion>=1) { Validate(); return; }
            // Preflight: don't partially reorganize a customized or incomplete layout.
            var houses=Names.Select(name=>town.transform.Find(name)).ToArray();
            var interiors=Names.Select(name=>town.transform.Find(name+" - 室内家具")).ToArray();
            if(houses.Any(h=>!h) || interiors.Any(h=>!h)) { Debug.LogError("街の構造が変更されています。建物/室内が5組あるか確認してください。"); return; }
            Directory.CreateDirectory("Backups");
            string backup="Backups/Meadow-before-authoring-review.unity";
            if(!File.Exists(backup) && File.Exists(town.gameObject.scene.path)) File.Copy(town.gameObject.scene.path,backup);
            TownRepair.Repair();
            var map=Object.FindAnyObjectByType<GridMap>();
            Undo.RecordObject(map,"Organize Town"); Undo.RecordObject(town,"Organize Town");
            var ground=map.transform.Find("Ground").GetComponent<Tilemap>();
            var path=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");
            var buildings=new List<TownBuilding>(); var rooms=new List<TownRoom>();
            Directory.CreateDirectory("Assets/Game/Prefabs/Town"); AssetDatabase.Refresh();
            for(int i=0;i<5;i++)
            {
                var house=houses[i]; var old=Vector3Int.FloorToInt(house.position);
                for(int y=0;y<4;y++) for(int x=-3;x<3;x++) map.RemoveOccupiedCell(old+new Vector3Int(x,y,0));
                Undo.RecordObject(house,"Align House"); house.position=new Vector3(old.x,old.y,0);
                var building=house.GetComponent<TownBuilding>() ?? Undo.AddComponent<TownBuilding>(house.gameObject);
                building.roomId=Ids[i];
                building.entrance=Anchor(house,"Entrance",new Vector3(-2,-2f/3,0));
                building.returnPoint=Anchor(house,"Return Point",new Vector3(-2,-2,0));
                buildings.Add(building);
                var entry=map.CellAt(building.entrance.position);
                // Join the shifted visual doorway to the existing path.
                for(int x=entry.x;x<=old.x;x++) for(int y=old.y-2;y<old.y;y++) ground.SetTile(new Vector3Int(x,y,0),path);
                var roomObj=new GameObject("Room - "+Names[i]); Undo.RegisterCreatedObjectUndo(roomObj,"Organize Town");
                roomObj.transform.SetParent(town.transform); roomObj.transform.position=new Vector3(i*20,80,0);
                var room=roomObj.AddComponent<TownRoom>(); room.roomId=Ids[i]; room.displayName=Names[i];
                room.arrival=Anchor(room.transform,"Arrival",new Vector3(6.5f,2.5f,0));
                room.exit=Anchor(room.transform,"Exit",new Vector3(6.5f,1.5f,0));
                Undo.SetTransformParent(interiors[i],room.transform,"Organize Room");
                var bed=interiors[i].Find("ベッド"); var table=interiors[i].Find("テーブル");
                if(bed) { Undo.RecordObject(bed,"Align Furniture"); bed.position=new Vector3(i*20+3,87,0); }
                if(table) { Undo.RecordObject(table,"Align Furniture"); table.position=new Vector3(i*20+9,85,0); }
                rooms.Add(room);
                string prefab="Assets/Game/Prefabs/Town/Building_"+Ids[i]+".prefab";
                if(!File.Exists(prefab))
                {
                    Vector3 position=house.position; house.position=Vector3.zero;
                    try { PrefabUtility.SaveAsPrefabAssetAndConnect(house.gameObject,prefab,InteractionMode.AutomatedAction); }
                    finally { house.position=position; PrefabUtility.RecordPrefabInstancePropertyModifications(house); }
                }
                EditorUtility.SetDirty(building); EditorUtility.SetDirty(room);
            }
            town.map=map; town.buildings=buildings.ToArray(); town.rooms=rooms.ToArray();
            // Building transitions now use their authored child anchors.
            town.passages=town.passages.Where(p=>p.label=="街へ" || p.label=="草原へ").ToArray();
            map.SetBuildings(town.buildings);
            town.authoringVersion=1;
            EditorUtility.SetDirty(town); EditorUtility.SetDirty(map); EditorUtility.SetDirty(ground);
            EditorSceneManager.MarkSceneDirty(town.gameObject.scene); AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("建物5棟をPrefab化し、入口・通行範囲・室内を整理しました。Ctrl+Sでシーンを保存してください。");
        }
        static Transform Anchor(Transform parent,string name,Vector3 local)
        {
            var t=parent.Find(name);
            if(!t) { var obj=new GameObject(name); Undo.RegisterCreatedObjectUndo(obj,"Create Entrance"); t=obj.transform; t.SetParent(parent,false); }
            t.localPosition=local; return t;
        }
        [MenuItem("Meadow Quest/Validate/Town")]
        public static void Validate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); if(!town) return;
            var errors=new List<string>(); var map=town.map;
            if(!map || !town.player || !town.battle || !town.lens || !town.body || !town.hint || !town.panel) { Debug.LogError("街の必須参照がありません。"); return; }
            var links=new Dictionary<Vector3Int,Vector3Int>();
            Action<Vector3Int,Vector3Int,string> link=(from,to,label)=>
            {
                if(!map.IsWalkable(from) || !map.IsWalkable(to)) errors.Add(label+": 通行不可");
                if(links.ContainsKey(from)) errors.Add(label+": 出入口重複"); else links.Add(from,to);
            };
            foreach(var p in town.passages) link(p.cell,p.destination,p.label);
            if(town.buildings.Length!=5 || town.rooms.Length!=5) errors.Add("建物/室内が5組ではありません");
            var ids=new HashSet<string>();
            foreach(var b in town.buildings)
            {
                if(!b || !b.entrance || !b.returnPoint) { errors.Add("建物の参照切れ"); continue; }
                if(!ids.Add(b.roomId)) errors.Add("部屋ID重複: "+b.roomId);
                var matches=town.rooms.Where(r=>r && r.roomId==b.roomId).ToArray();
                if(matches.Length!=1 || !matches[0].arrival || !matches[0].exit) { errors.Add("室内の対応不明: "+b.roomId); continue; }
                var r=matches[0];
                link(map.CellAt(b.entrance.position),map.CellAt(r.arrival.position),b.roomId+" 入室");
                link(map.CellAt(r.exit.position),map.CellAt(b.returnPoint.position),b.roomId+" 退室");
                if(!r.Contains(r.arrival.position) || !r.Contains(r.exit.position)) errors.Add(b.roomId+": 室内アンカーが部屋の外");
                if(!PrefabUtility.IsPartOfPrefabInstance(b)) errors.Add(b.roomId+": Prefab未接続");
                var roof=b.transform.Find("Roof - assembled");
                if(!roof || !roof.GetComponent<SpriteRenderer>().sprite) errors.Add(b.roomId+": 屋根の参照切れ");
            }
            foreach(var destination in links.Values) if(links.ContainsKey(destination)) errors.Add("移動先が別の入口に重なっています: "+destination);
            var visited=Reach(map,town.start,links);
            if(!map.IsWalkable(town.start)) errors.Add("開始位置が通行不可");
            foreach(var point in links.Keys.Concat(links.Values)) if(!visited.Contains(point)) errors.Add("到達不可: "+point);
            foreach(var point in links.Values) if(!Reach(map,point,links).Contains(town.start)) errors.Add("街へ帰還不可: "+point);
            foreach(var npc in town.residents)
                if(!Directions.Any(d=>visited.Contains(npc.cell+d) && !links.ContainsKey(npc.cell+d))) errors.Add("住民に隣接できません: "+npc.name);
            foreach(var renderer in town.GetComponentsInChildren<SpriteRenderer>())
                if(!renderer.sprite) errors.Add("Sprite参照切れ: "+renderer.name);
            string report="Town authoring validation\n"+DateTime.Now.ToString("s")+"\nBuildings: "+town.buildings.Length+" Rooms: "+town.rooms.Length+" Links: "+links.Count+" Reachable cells: "+visited.Count+"\n"+
                (errors.Count==0?"PASS: entrances, returns, residents, references, prefabs.\nRuntime play test is separate.":string.Join("\n",errors));
            File.WriteAllText("TownAudit.txt",report);
            if(errors.Count>0) Debug.LogError(report); else Debug.Log(report);
        }
        static readonly Vector3Int[] Directions={Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right};
        static HashSet<Vector3Int> Reach(GridMap map,Vector3Int start,Dictionary<Vector3Int,Vector3Int> links)
        {
            return TownReachability.Reach(start,map.IsWalkable,c=>Directions.Select(d=>c+d),links);
        }
    }
}
