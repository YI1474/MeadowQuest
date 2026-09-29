using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class FurnitureAndLevelSetup
    {
        static FurnitureAndLevelSetup() { EditorApplication.delayCall+=Apply; EditorSceneManager.sceneOpened+=(s,m)=>Apply(); }
        [MenuItem("Meadow Quest/Repair/Interior Furniture And Level Display")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); var map=Object.FindAnyObjectByType<GridMap>();
            if(town && map)
            {
                bool changed=false;
                foreach(var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
                {
                    if(renderer.gameObject.scene!=town.gameObject.scene) continue;
                    string sprite=renderer.sprite?renderer.sprite.name:"";
                    if(renderer.name!="ベッド" && renderer.name!="テーブル" && sprite!="Bed" && sprite!="Table" && sprite!="TableCleanV2") continue;
                    Undo.DestroyObjectImmediate(renderer.gameObject); changed=true;
                }
                Undo.RecordObject(map,"Remove Furniture Collision");
                for(int room=0;room<5;room++)
                {
                    for(int y=87;y<90;y++) for(int x=room*20+2;x<room*20+4;x++) map.RemoveOccupiedCell(new Vector3Int(x,y,0));
                    for(int y=85;y<87;y++) for(int x=room*20+8;x<room*20+11;x++) map.RemoveOccupiedCell(new Vector3Int(x,y,0));
                    for(int x=room*20+8;x<room*20+10;x++) map.RemoveOccupiedCell(new Vector3Int(x,87,0));
                }
                EditorUtility.SetDirty(map);
                if(changed) { EditorSceneManager.MarkSceneDirty(town.gameObject.scene); Debug.Log("室内のベッド・テーブルと当たり判定を撤去しました。Ctrl+Sで保存してください。"); }
            }
            var view=Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if(!view || view.levelUpPanel) return;
            Undo.RecordObject(view,"Add Level Up Display");
            var shade=Rect(view.root.transform,"Level Up Overlay",0,0,1,1); var bg=shade.gameObject.AddComponent<Image>(); bg.color=new Color(0,0,0,.85f);
            Undo.RegisterCreatedObjectUndo(shade.gameObject,"Add Level Up Display");
            var panel=Rect(shade,"Level Up Card",.23f,.12f,.77f,.88f); panel.gameObject.AddComponent<Image>().color=new Color(.05f,.15f,.2f);
            var content=Rect(panel,"Level Up Details",.08f,.24f,.92f,.94f).gameObject.AddComponent<Text>();
            content.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf"); content.fontSize=23; content.color=Color.white; content.raycastTarget=false; content.alignment=TextAnchor.UpperCenter;
            content.resizeTextForBestFit=true; content.resizeTextMinSize=16; content.resizeTextMaxSize=23;
            var confirm=Rect(panel,"Confirm",.25f,.06f,.75f,.18f); var image=confirm.gameObject.AddComponent<Image>(); image.color=new Color(.15f,.4f,.45f);
            var button=confirm.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var label=Rect(confirm,"Label",0,0,1,1).gameObject.AddComponent<Text>(); label.font=content.font; label.fontSize=22; label.alignment=TextAnchor.MiddleCenter; label.text="確認"; label.raycastTarget=false;
            view.levelUpPanel=shade.gameObject; view.levelUpText=content; view.levelUpConfirm=button; shade.gameObject.SetActive(false);
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(view.gameObject.scene); AssetDatabase.SaveAssets();
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float r,float t)
        {
            var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(r,t); rect.offsetMin=rect.offsetMax=Vector2.zero; return rect;
        }
    }
}
