using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class DialogueSetup
    {
        static DialogueSetup() { EditorApplication.delayCall+=Install; }
        [MenuItem("Meadow Quest/Setup/Guide And Dialogue")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Game/Scenes/Meadow.unity" || GameObject.Find("Dialogue System")) return;
            var player=Object.FindAnyObjectByType<GridMover>();
            var map=Object.FindAnyObjectByType<GridMap>();
            if(!player || !map) return;
            System.IO.Directory.CreateDirectory("Assets/Game/Data"); AssetDatabase.Refresh();
            const string path="Assets/Game/Data/GuideDialogue.asset";
            var data=AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if(!data)
            {
                data=ScriptableObject.CreateInstance<DialogueData>();
                data.speaker="草原の案内人";
                data.lines=new[]{"ようこそ、はじまりの草原へ！　きみの冒険はここから始まるよ。","まずは道に沿って歩いてみよう。茂みの向こうには進めないから、回り道をしてね。","この草原には、不思議な生き物が暮らしているんだ。どんな仲間に出会えるか、楽しみだね。"};
                AssetDatabase.CreateAsset(data,path);
            }
            var npc=new GameObject("Guide",typeof(SpriteRenderer));
            npc.transform.position=new Vector3(16.5f,8.5f,0);
            var sprite=npc.GetComponent<SpriteRenderer>();
            sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Player_0_1.png");
            sprite.color=new Color(.7f,.85f,1); sprite.sortingOrder=83;
            map.AddOccupiedCell(new Vector3Int(16,8,0)); EditorUtility.SetDirty(map);
            var canvasObject=new GameObject("Dialogue Canvas",typeof(Canvas),typeof(CanvasScaler));
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(960,600); scaler.matchWidthOrHeight=.5f;
            var panel=new GameObject("Dialogue Panel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvasObject.transform,false);
            var rect=panel.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(.05f,.05f); rect.anchorMax=new Vector2(.95f,.4f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=new Color(.04f,.1f,.16f,.97f);
            var body=MakeText("Dialogue Text",panel.transform,20);
            var bodyRect=body.rectTransform; bodyRect.offsetMin=new Vector2(22,12); bodyRect.offsetMax=new Vector2(-22,-12);
            var hint=MakeText("Controls Hint",canvasObject.transform,18);
            hint.rectTransform.anchorMin=new Vector2(.04f,.9f); hint.rectTransform.anchorMax=new Vector2(.96f,.98f);
            hint.text="WASD / 矢印キー：移動　｜　E / Enter：話す";
            var system=new GameObject("Dialogue System").AddComponent<DialogueController>();
            system.Configure(player,npc.transform,data,panel,body,hint);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Guide and Canvas dialogue saved to Meadow scene.");
        }
        static Text MakeText(string name,Transform parent,int size)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
            var text=obj.GetComponent<Text>(); text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf"); text.fontSize=size; text.color=Color.white; text.raycastTarget=false;
            text.rectTransform.anchorMin=Vector2.zero; text.rectTransform.anchorMax=Vector2.one; text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
            return text;
        }
    }
}

