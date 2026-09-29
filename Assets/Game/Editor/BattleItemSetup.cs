using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class BattleItemSetup
    {
        static BattleItemSetup() { EditorApplication.delayCall+=Install; EditorSceneManager.sceneOpened+=(s,m)=>Install(); }
        [MenuItem("Meadow Quest/Setup/Battle Items")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            CaptureSetup.Install(); BattlePartySetup.Install();
            var view=Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if(!view || !view.back || view.previousBattleItems && view.nextBattleItems) return;
            Undo.RecordObject(view,"Setup Battle Items");
            view.previousBattleItems=Create(view,"Previous Battle Items","前へ",.76f);
            view.nextBattleItems=Create(view,"Next Battle Items","次へ",.86f);
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
        static Button Create(BattleView view,string name,string label,float x)
        {
            var button=Object.Instantiate(view.back,view.back.transform.parent); button.name=name;
            Undo.RegisterCreatedObjectUndo(button.gameObject,"Setup Battle Items");
            var rect=button.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(x,.15f); rect.anchorMax=new Vector2(x+.08f,.24f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            button.GetComponentInChildren<Text>().text=label; button.GetComponentInChildren<Text>().fontSize=18;
            button.gameObject.SetActive(false); return button;
        }
    }
}
