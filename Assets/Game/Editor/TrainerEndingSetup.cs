using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class TrainerEndingSetup
    {
        static TrainerEndingSetup() {EditorApplication.delayCall+=Install; EditorSceneManager.sceneOpened+=(s,m)=>Install();}
        [MenuItem("Meadow Quest/Setup/Trainer Teams And Ending")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>();
            if(!town || town.gameObject.scene.path!="Assets/Game/Scenes/Meadow.unity") return;
            AdventureSetup.Install();
            bool changed=false;
            foreach(var npc in town.GetComponentsInChildren<TrainerNpc>(true))
            {
                if(npc.team!=null && npc.team.Length>0) continue;
                string[] ids=npc.id=="town_ren"?new[]{"haineko","akabine"}:npc.id==AdventureProgress.FinalTrainer?new[]{"sunakuri","mizuhane","Wild"}:null;
                if(ids==null) continue;
                var definitions=ids.Select(id=>AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/"+id+".asset")).ToArray();
                if(definitions.Any(d=>!d)) {Debug.LogWarning("手持ちの設定にはData > Import CSVが必要です。"); continue;}
                Undo.RecordObject(npc,"Setup Trainer Team");
                npc.team=definitions.Select(d=>new TrainerNpc.TeamMember{monster=d,level=npc.id==AdventureProgress.FinalTrainer?12:10}).ToArray();
                npc.challenge=npc.id==AdventureProgress.FinalTrainer?"仲間との絆を見せてくれ。私の3匹と、最後の試練だ！":"僕の2匹と勝負だ！ 勝ったら師範に挑戦してね。";
                npc.afterVictory="いい勝負だった！ 仲間との冒険を楽しんでね。";
                EditorUtility.SetDirty(npc); changed=true;
            }
            var view=Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if(view && view.clearPanel && !view.clearPanel.GetComponent<EndingSequence>())
            {
                var ending=Undo.AddComponent<EndingSequence>(view.clearPanel);
                ending.confirm=view.clearConfirm;
                ending.heading=view.clearPanel.GetComponentsInChildren<Text>(true).First(t=>t.name=="Clear Message");
                Undo.RecordObject(ending.heading,"Setup Ending Heading"); Undo.RecordObject(ending.heading.rectTransform,"Setup Ending Heading");
                ending.heading.fontSize=26; Place(ending.heading.rectTransform,.06f,.77f,.94f,.98f);
                ending.heading.text="ゲームクリア！\n師範アサギに勝利しました！";
                var obj=new GameObject("Credits Viewport",typeof(RectTransform),typeof(RectMask2D)); obj.transform.SetParent(view.clearPanel.transform,false); Undo.RegisterCreatedObjectUndo(obj,"Create Credits Viewport");
                ending.viewport=obj.GetComponent<RectTransform>(); Place(ending.viewport,.06f,.24f,.94f,.75f);
                var textObject=new GameObject("Credits",typeof(RectTransform),typeof(Text)); textObject.transform.SetParent(obj.transform,false);
                var text=textObject.GetComponent<Text>(); text.font=ending.heading.font; text.fontSize=22; text.color=Color.white; text.alignment=TextAnchor.UpperCenter; text.raycastTarget=false;
                text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow;
                var rect=text.rectTransform; rect.anchorMin=new Vector2(0,0); rect.anchorMax=new Vector2(1,0); rect.pivot=new Vector2(.5f,1); rect.sizeDelta=new Vector2(0,900);
                ending.credits=text; text.gameObject.SetActive(false); EditorUtility.SetDirty(ending); changed=true;
            }
            if(changed) EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
        }
        static void Place(RectTransform rect,float x,float y,float r,float t)
        {rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(r,t); rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
