using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class JapaneseUiSetup
    {
        static JapaneseUiSetup() { EditorApplication.delayCall+=Apply; }
        [MenuItem("Meadow Quest/Setup/Japanese UI")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/Game/Scenes/Meadow.unity") return;
            var canvas=GameObject.Find("Dialogue Canvas");
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            if(!canvas || !font) return;
            bool changed=false;
            foreach(var text in canvas.GetComponentsInChildren<Text>(true))
            {
                if(text.font!=font) { text.font=font; EditorUtility.SetDirty(text); changed=true; }
                if(text.name=="Controls Hint" && text.text.Contains("WASD / Arrows"))
                { text.text="WASD / 矢印キー：移動　｜　E / Enter：話す"; EditorUtility.SetDirty(text); changed=true; }
            }
            if(changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }
    }
}
