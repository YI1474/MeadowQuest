using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class RidgeDoorRepair
    {
        static RidgeDoorRepair() { EditorApplication.delayCall+=Upgrade; EditorSceneManager.sceneOpened+=(s,m)=>Upgrade(); }
        static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(town && town.ridgeDoorVersion<1) Repair();
        }
        [MenuItem("Meadow Quest/Repair/Ridge Building Doors")]
        public static void Repair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(!town || town.gameObject.scene.path!="Assets/Game/Scenes/Meadow.unity" || !town.map) return;
            var root=town.transform.Find("Route - 洞窟と岩峰の街"); if(!root) return;
            string[] names={"回復所","道具屋","試練場"}; int[] roomX={266,286,239};
            var houses=names.Select(name=>root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name==name && r.transform.parent==root)).ToArray();
            var path=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");
            if(!path || houses.Any(h=>!h)) return;
            var ground=town.map.transform.Find("Ground").GetComponent<Tilemap>();
            var walls=town.map.transform.Find("Obstacles").GetComponent<Tilemap>();
            Undo.RecordObjects(new Object[]{town,ground,walls},"Align Ridge Doors");
            var passages=new List<TownWorld.Passage>(town.passages);
            for(int i=0;i<houses.Length;i++)
            {
                var house=houses[i].transform;
                Undo.RecordObject(house,"Align Ridge House To Grid");
                // Same facade and door alignment as the starting town: the visible door is left of center.
                house.position=new Vector3(Mathf.Floor(house.position.x),Mathf.Floor(house.position.y),house.position.z);
                var entry=Anchor(house,"Entrance",new Vector3(-2,-2f/3,0));
                var exit=Anchor(house,"Return Point",new Vector3(-2,-2,0));
                var entryCell=town.map.CellAt(entry.position); var returnCell=town.map.CellAt(exit.position);
                var arrival=new Vector3Int(roomX[i],2,0); var insideExit=new Vector3Int(roomX[i],1,0);
                passages.RemoveAll(p=>p.destination==arrival || p.cell==insideExit);
                passages.Add(new TownWorld.Passage{cell=entryCell,destination=arrival,label=names[i]+"へ"});
                passages.Add(new TownWorld.Passage{cell=insideExit,destination=returnCell,label="岩峰の街へ"});
                for(int x=entryCell.x;x<=Mathf.FloorToInt(house.position.x);x++) for(int y=returnCell.y;y<=entryCell.y;y++)
                {var cell=new Vector3Int(x,y,0); ground.SetTile(cell,path); walls.SetTile(cell,null);}
                EditorUtility.SetDirty(house);
            }
            town.passages=passages.ToArray(); town.ridgeDoorVersion=1;
            EditorUtility.SetDirty(town); EditorUtility.SetDirty(ground); EditorUtility.SetDirty(walls); EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
            RouteEntranceRepair.Validate(town);
            Debug.Log("岩峰の街の回復所・道具屋・試練場の入口と帰還位置をドア前に合わせました。Ctrl+Sで保存してください。");
        }
        static Transform Anchor(Transform parent,string name,Vector3 local)
        {
            var anchor=parent.Find(name);
            if(!anchor) {var obj=new GameObject(name); anchor=obj.transform; anchor.SetParent(parent,false); Undo.RegisterCreatedObjectUndo(obj,"Ridge Door Anchor");}
            Undo.RecordObject(anchor,"Ridge Door Anchor"); anchor.localPosition=local; return anchor;
        }
    }
}
