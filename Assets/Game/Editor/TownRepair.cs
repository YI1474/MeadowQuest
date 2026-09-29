using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class TownRepair
    {
        static TownRepair()
        {
            EditorApplication.delayCall+=AutoRepair;
            EditorSceneManager.sceneOpened+=(scene,mode)=>AutoRepair();
        }
        static void AutoRepair()
        {
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(town && town.repairVersion<3) Repair();
        }
        [MenuItem("Meadow Quest/Repair/Town Visuals And Doors")]
        public static void Repair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); if(!town) return;
            foreach(var renderer in town.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Undo.RecordObject(renderer,"Repair Town");
                if(renderer.name=="Roof - assembled") continue;
                renderer.sortingOrder=1000-Mathf.RoundToInt(renderer.transform.position.y*2);
                string sprite=renderer.sprite?renderer.sprite.name:"";
                if(sprite.StartsWith("House"))
                {
                    renderer.sprite=TownBuilder.HouseSprite(sprite.Contains("Red"));
                    if(sprite.Contains("Gray")) renderer.color=new Color(.8f,.85f,.95f);
                    TownBuilder.AssembleRoof(renderer);
                }
                EditorUtility.SetDirty(renderer);
            }
            foreach(var label in town.GetComponentsInChildren<TextMesh>(true)) label.GetComponent<MeshRenderer>().sortingOrder=2000;
            var playerRenderer=town.player.GetComponent<SpriteRenderer>();
            playerRenderer.sortingOrder=1000-Mathf.RoundToInt(town.player.transform.position.y*2);
            var map=Object.FindAnyObjectByType<GridMap>();
            var ground=map.transform.Find("Ground").GetComponent<Tilemap>();
            var colors=new[]{new Color(1,.92f,.82f),new Color(.85f,1,1),new Color(.85f,1,.8f),new Color(.9f,.86f,1),new Color(1,.85f,.8f)};
            var path=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");
            for(int room=0;room<5;room++)
            {
                for(int y=81;y<90;y++) for(int x=room*20+1;x<room*20+12;x++)
                {
                    var cell=new Vector3Int(x,y,0); ground.SetTileFlags(cell,TileFlags.None); ground.SetColor(cell,colors[room]);
                }
                var exit=new Vector3Int(room*20+6,81,0);
                ground.SetTile(exit,path); ground.SetTileFlags(exit,TileFlags.None); ground.SetColor(exit,Color.white);
            }
            foreach(var p in town.passages)
                if(!map.IsWalkable(p.cell) || !map.IsWalkable(p.destination)) throw new System.InvalidOperationException("出入口が塞がれています: "+p.label);
            town.repairVersion=3;
            EditorUtility.SetDirty(town); EditorUtility.SetDirty(ground);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
            Debug.Log("屋根の不要パーツ・室内の描画順を修正し、出口マットを追加しました。Ctrl+Sで保存してください。");
        }
    }
}

