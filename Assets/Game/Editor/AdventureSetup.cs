using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class AdventureSetup
    {
        static AdventureSetup() {EditorApplication.delayCall+=Install; EditorSceneManager.sceneOpened+=(s,m)=>Install();}
        [MenuItem("Meadow Quest/Setup/Adventure Goal")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            TrainerSetup.Install();
            var view=Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if(!view || view.clearPanel || !view.close) return;
            Undo.RecordObject(view,"Add Adventure Clear");
            var obj=new GameObject("Adventure Clear",typeof(RectTransform),typeof(Image)); obj.transform.SetParent(view.root.transform,false); Undo.RegisterCreatedObjectUndo(obj,"Add Adventure Clear");
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; obj.GetComponent<Image>().color=new Color(.02f,.09f,.13f,.98f);
            var text=Object.Instantiate(view.message,obj.transform); text.name="Clear Message"; text.rectTransform.anchorMin=new Vector2(.1f,.25f); text.rectTransform.anchorMax=new Vector2(.9f,.8f); text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero; text.fontSize=28; text.alignment=TextAnchor.MiddleCenter;
            text.text="試練認定！\n\n師範アサギとの勝負に勝った！\n最初の認定を獲得した。\n\n冒険はまだ続けられます。\n終了前にメニューからセーブしてね。";
            var button=Object.Instantiate(view.close,obj.transform); button.name="Continue Adventure"; rect=button.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(.32f,.10f); rect.anchorMax=new Vector2(.68f,.21f); rect.offsetMin=rect.offsetMax=Vector2.zero; button.GetComponentInChildren<Text>().text="冒険を続ける"; button.gameObject.SetActive(true);
            view.clearPanel=obj; view.clearConfirm=button; obj.SetActive(false); EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
    }
}

