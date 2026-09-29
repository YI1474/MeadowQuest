using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class TownGateRepair
    {
        static TownGateRepair()
        {
            EditorApplication.delayCall+=Upgrade;
            EditorSceneManager.sceneOpened+=(scene,mode)=>Upgrade();
        }
        static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(town && town.gateVersion<1) Repair();
        }
        [MenuItem("Meadow Quest/Repair/West Town Gate")]
        public static void Repair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); if(!town) return;
            var map=Object.FindAnyObjectByType<GridMap>();
            var ground=map.transform.Find("Ground").GetComponent<Tilemap>();
            var walls=map.transform.Find("Obstacles").GetComponent<Tilemap>();
            var path=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");
            if(!path) { Debug.LogError("出口用の道タイルが見つかりません。"); return; }
            Undo.RecordObjects(new Object[]{town,ground,walls},"Open West Gate");
            var passages=new List<TownWorld.Passage>(town.passages);
            passages.RemoveAll(p=>p.label=="草原へ");
            for(int y=21;y<=23;y++)
            {
                var cell=new Vector3Int(40,y,0);
                walls.SetTile(cell,null); ground.SetTile(cell,path);
                passages.Add(new TownWorld.Passage{cell=cell,destination=new Vector3Int(22,8,0),label="草原へ"});
            }
            town.passages=passages.ToArray(); town.gateVersion=1;
            EditorUtility.SetDirty(town); EditorUtility.SetDirty(ground); EditorUtility.SetDirty(walls);
            EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
            RouteEntranceRepair.Repair();
            if(town.authoringVersion>=1) TownAuthoringAudit.Validate();
            Debug.Log("西の生け垣を3マス開けました。開口部を歩くと草原へ移動します。Ctrl+Sで保存してください。");
        }
    }
}
