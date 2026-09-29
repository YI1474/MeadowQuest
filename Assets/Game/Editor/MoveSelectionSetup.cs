using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class MoveSelectionSetup
    {
        static MoveSelectionSetup()
        {
            EditorApplication.delayCall+=Install;
            EditorSceneManager.sceneOpened+=(scene,mode)=>Install();
        }
        [MenuItem("Meadow Quest/Setup/Move Selection")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Game/Scenes/Meadow.unity") return;
            var view=Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if(!view || !view.attack) return;
            if(view.moveButtons!=null && view.moveButtons.Length==4 && System.Array.TrueForAll(view.moveButtons,b=>b)
                && view.fight && view.items && view.party && view.back && view.next && view.details) return;
            Undo.RecordObject(view,"Setup Move Selection");
            view.moveButtons=new Button[4]; view.moveButtons[0]=view.attack;
            for(int i=1;i<4;i++)
            {
                var existing=view.attack.transform.parent.Find("Move "+(i+1));
                var button=existing?existing.GetComponent<Button>():null;
                if(!button)
                {
                    button=Object.Instantiate(view.attack,view.attack.transform.parent);
                    button.name="Move "+(i+1);
                    Undo.RegisterCreatedObjectUndo(button.gameObject,"Setup Move Selection");
                }
                view.moveButtons[i]=button;
            }
            for(int i=0;i<4;i++)
            {
                var button=view.moveButtons[i];
                float x=.06f+(i%2)*.34f, y=i<2?.15f:.04f;
                Place(button.transform,new Vector2(x,y),new Vector2(x+.32f,y+.095f));
                var label=button.GetComponentInChildren<Text>(true);
                if(label) { Undo.RecordObject(label,"Setup Move Selection"); label.fontSize=18; label.text="技 "+(i+1); EditorUtility.SetDirty(label); }
            }
            view.fight=Command(view,"Fight Command","たたかう",.06f,.15f,.38f);
            view.items=Command(view,"Items Command","どうぐ",.40f,.15f,.72f);
            view.party=Command(view,"Party Command","なかま",.06f,.04f,.38f);
            view.back=Command(view,"Back Command","戻る",.76f,.04f,.94f);
            view.next=Command(view,"Next Command","次へ",.76f,.04f,.94f);
            Place(view.escape.transform,new Vector2(.40f,.04f),new Vector2(.72f,.135f));
            view.escape.GetComponentInChildren<Text>(true).text="にげる";
            if(!view.details)
            {
                var existing=view.root.transform.Find("Command Details");
                view.details=existing?existing.GetComponent<Text>():null;
                if(!view.details)
                {
                    view.details=Object.Instantiate(view.message,view.root.transform);
                    view.details.name="Command Details";
                    Undo.RegisterCreatedObjectUndo(view.details.gameObject,"Setup Move Selection");
                }
            }
            view.details.fontSize=20;
            Place(view.details.transform,new Vector2(.06f,.04f),new Vector2(.72f,.24f));
            Place(view.message.transform,new Vector2(.06f,.25f),new Vector2(.94f,.33f));
            var arena=view.root.transform.Find("Arena");
            if(arena) Place(arena,new Vector2(.04f,.35f),new Vector2(.96f,.87f));
            foreach(var button in view.moveButtons) button.gameObject.SetActive(false);
            view.back.gameObject.SetActive(false); view.next.gameObject.SetActive(false);
            view.details.gameObject.SetActive(false);
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("技選択UIを追加しました。シーンを保存してください。CSV未取り込みの場合はData > Import CSVを実行してください。");
        }
        static Button Command(BattleView view,string name,string caption,float left,float bottom,float right)
        {
            var existing=view.root.transform.Find(name);
            var button=existing?existing.GetComponent<Button>():null;
            if(!button)
            {
                button=Object.Instantiate(view.attack,view.root.transform); button.name=name;
                Undo.RegisterCreatedObjectUndo(button.gameObject,"Setup Move Selection");
            }
            button.gameObject.SetActive(true);
            Place(button.transform,new Vector2(left,bottom),new Vector2(right,bottom+.095f));
            var label=button.GetComponentInChildren<Text>(true); label.text=caption; label.fontSize=22;
            return button;
        }
        static void Place(Transform target,Vector2 min,Vector2 max)
        {
            var rect=(RectTransform)target; Undo.RecordObject(rect,"Setup Move Selection");
            rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
