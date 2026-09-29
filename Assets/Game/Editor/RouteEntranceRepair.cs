using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class RouteEntranceRepair
    {
        static RouteEntranceRepair() { EditorApplication.delayCall+=Upgrade; EditorSceneManager.sceneOpened+=(s,m)=>Upgrade(); }
        static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(town && town.entranceVersion<3 && town.transform.Find("Route - 洞窟と岩峰の街")) Repair();
        }
        [MenuItem("Meadow Quest/Repair/Route Entrances")]
        public static void Repair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(!town || town.gameObject.scene.path!="Assets/Game/Scenes/Meadow.unity") return;
            var root=town.transform.Find("Route - 洞窟と岩峰の街"); if(!root) return;
            var map=town.map; if(!map) return;
            var ground=map.transform.Find("Ground").GetComponent<Tilemap>();
            var walls=map.transform.Find("Obstacles").GetComponent<Tilemap>();
            var path=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");

            if(!path) return;
            Undo.RecordObjects(new Object[]{town,ground,walls},"Repair Route Entrances");
            var doors=new List<TownWorld.Passage>(town.passages);
            // Remove only the old outdoor connection cells, preserving building entrances.
            doors.RemoveAll(p=>p.cell.x>=23 && p.cell.x<=24 && p.cell.y>=7 && p.cell.y<=9 || p.cell.x>=40 && p.cell.x<=41 && p.cell.y>=21 && p.cell.y<=23 || p.cell.x<=1 && p.cell.x>=0 && p.cell.y>=7 && p.cell.y<=9 || p.cell.x>=138 && p.cell.x<=140 && (p.cell.y==1 || p.cell.y==28) || p.cell.x>=196 && p.cell.x<=198 && (p.cell.y==0 || p.cell.y==1));
            for(int lane=-1;lane<=1;lane++)
            {
                Add(doors,24,8+lane,42,22+lane,"はじまりの街へ");
                Add(doors,40,22+lane,22,8+lane,"草原へ");
                Add(doors,1,8+lane,139+lane,2,"洞窟へ");
                Add(doors,139+lane,1,2,8+lane,"洞窟から草原へ");
                Add(doors,139+lane,28,197+lane,3,"岩峰の街へ");
                Add(doors,197+lane,0,139+lane,27,"洞窟へ戻る");
                // Open the actual outer wall, not just the floor one row in front of it.
                for(int y=0;y<=3;y++) Floor(ground,walls,path,197+lane,y);
                var entrance=new Vector3Int(197+lane,0,0);
                ground.SetTileFlags(entrance,TileFlags.None);
                ground.SetColor(entrance,new Color(.4f,.4f,.4f,1));
                for(int x=0;x<=4;x++) Floor(ground,walls,path,x,8+lane);
                for(int x=22;x<=24;x++) Floor(ground,walls,path,x,8+lane);
                for(int x=40;x<=42;x++) Floor(ground,walls,path,x,22+lane);
            }
            foreach(var p in doors) if(p.cell.x>=138 && p.cell.x<=140 && (p.cell.y==1 || p.cell.y==28) || p.cell.x>=196 && p.cell.x<=198 && p.cell.y==1) Floor(ground,walls,path,p.cell.x,p.cell.y);
            var visual=root.Find("Cave Entrance Visual");
            if(visual) Undo.DestroyObjectImmediate(visual.gameObject);
            // Keep the existing road texture; only tint the three entry cells.
            for(int y=7;y<=9;y++)
            {
                var cell=new Vector3Int(1,y,0);
                ground.SetTileFlags(cell,TileFlags.None);
                ground.SetColor(cell,new Color(.4f,.4f,.4f,1));
            }
            var sign=root.Find("洞窟入口"); if(sign) {Undo.RecordObject(sign,"Move Cave Sign"); sign.position=new Vector3(2,10.5f,0);}
            var ridgeSign=root.Find("↓ こだまの洞窟");
            if(ridgeSign) {Undo.RecordObject(ridgeSign,"Move Ridge Cave Sign"); ridgeSign.position=new Vector3(202,2.5f,0);}
            town.passages=doors.ToArray(); town.entranceVersion=3;
            EditorUtility.SetDirty(town); EditorUtility.SetDirty(ground); EditorUtility.SetDirty(walls); EditorSceneManager.MarkSceneDirty(town.gameObject.scene); AssetDatabase.SaveAssets();
            Validate(town);
            Debug.Log("洞窟入口の見た目と、草原・洞窟・街の3マス移動判定を修正しました。Ctrl+Sで保存してください。");
        }
        static void Add(List<TownWorld.Passage> list,int x,int y,int tx,int ty,string label) => list.Add(new TownWorld.Passage{cell=new Vector3Int(x,y,0),destination=new Vector3Int(tx,ty,0),label=label});
        static void Floor(Tilemap ground,Tilemap walls,Tile path,int x,int y)
        {var cell=new Vector3Int(x,y,0); ground.SetTile(cell,path); walls.SetTile(cell,null);}
        public static void Validate(TownWorld town)
        {
            var triggers=new HashSet<Vector3Int>();
            foreach(var p in town.passages)
            {
                if(!triggers.Add(p.cell)) throw new System.InvalidOperationException("移動判定が重複しています："+p.cell);
                if(!town.map.IsWalkable(p.cell) || !town.map.IsWalkable(p.destination)) throw new System.InvalidOperationException("出入口が通行不可です："+p.label);
            }
            foreach(var p in town.passages) if(triggers.Contains(p.destination)) throw new System.InvalidOperationException("到着地点が移動判定と重なっています："+p.label);
            for(int x=196;x<=198;x++)
            {
                var cell=new Vector3Int(x,0,0);
                if(!town.map.IsWalkable(cell) || !triggers.Contains(cell)) throw new System.InvalidOperationException("街側の洞窟入口が開いていません："+cell);
            }
        }
    }
}

