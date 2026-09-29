using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class TrainerSetup
    {
        static TrainerSetup() { EditorApplication.delayCall+=Install; EditorSceneManager.sceneOpened+=(s,m)=>Install(); }
        [MenuItem("Meadow Quest/Setup/Trainer Battles")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); var map=Object.FindAnyObjectByType<GridMap>();
            if(!town || !map || town.gameObject.scene.path!="Assets/Game/Scenes/Meadow.unity") return;
            string[] ids={"town_sora","town_nagi","town_ren","town_master"},names={"見習いのソラ","散歩好きのナギ","研究助手のレン","師範アサギ"},monsters={"akabine","mizuhane","haineko","sunakuri"};
            var definitions=monsters.Select(id=>AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/"+id+".asset")).ToArray();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Player_0_1.png");
            if(definitions.Any(m=>!m) || !sprite) return;
            var residents=new List<TownWorld.Resident>(town.residents ?? new TownWorld.Resident[0]);
            var preferred=new[]{new Vector3Int(60,23,0),new Vector3Int(80,23,0),new Vector3Int(70,17,0),new Vector3Int(70,36,0)};
            var colors=new[]{new Color(1,.7f,.45f),new Color(.5f,.8f,1),new Color(.85f,.65f,1),new Color(.65f,1,.65f)};
            bool changed=false;
            for(int i=0;i<ids.Length;i++)
            {
                if(residents.Any(r=>r.trainer && r.trainer.id==ids[i])) continue;
                Vector3Int cell=preferred[i]; bool found=false;
                for(int radius=0;radius<=4 && !found;radius++) for(int dx=-radius;dx<=radius && !found;dx++) for(int dy=-radius;dy<=radius && !found;dy++)
                {
                    var candidate=preferred[i]+new Vector3Int(dx,dy,0);
                    if(!map.IsWalkable(candidate) || residents.Any(r=>r.cell==candidate) || candidate==town.start) continue;
                    if(town.passages.Any(p=>p.cell==candidate || p.destination==candidate)) continue;
                    if(town.buildings.Any(b=>b && (map.CellAt(b.entrance.position)==candidate || map.CellAt(b.returnPoint.position)==candidate))) continue;
                    int open=0; foreach(var d in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}) if(map.IsWalkable(candidate+d)) open++;
                    if(open<3) continue;
                    cell=candidate; found=true;
                }
                if(!found) { Debug.LogWarning(names[i]+"の配置場所を確保できませんでした。"); continue; }
                Undo.RecordObject(town,"Add Trainer"); Undo.RecordObject(map,"Add Trainer");
                var obj=new GameObject(names[i],typeof(SpriteRenderer),typeof(TrainerNpc)); obj.transform.SetParent(town.transform,false); obj.transform.position=new Vector3(cell.x+.5f,cell.y+.5f,0);
                Undo.RegisterCreatedObjectUndo(obj,"Add Trainer");
                var renderer=obj.GetComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=colors[i]; renderer.sortingOrder=1000-Mathf.RoundToInt(obj.transform.position.y*2);
                var npc=obj.GetComponent<TrainerNpc>(); npc.id=ids[i]; npc.displayName=names[i]; npc.monster=definitions[i];
                if(ids[i]==AdventureProgress.FinalTrainer) {npc.levelOverride=12; npc.prizeMoney=600;}
                npc.challenge="僕の"+definitions[i].displayName+"と腕試ししよう！";
                npc.afterVictory="君たち、いいコンビだね！ 僕ももっと練習してくるよ。";
                residents.Add(new TownWorld.Resident{cell=cell,name=names[i],text=npc.challenge,trainer=npc}); map.AddOccupiedCell(cell); changed=true;
            }
            if(!changed) return;
            town.residents=residents.ToArray(); EditorUtility.SetDirty(town); EditorUtility.SetDirty(map);
            EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
            Debug.Log("街に対戦相手を配置しました。話しかけてE/Enterで対戦。Ctrl+Sでシーン保存してください。");
        }
    }
}
