using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class CaptureSetup
    {
        static CaptureSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (s, m) => Install();
        }

        [MenuItem("Meadow Quest/Setup/Capture")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var view = Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if (!view || !view.back || view.captureUse)
                return;
            Undo.RecordObject(view, "Setup Capture");
            view.captureButtons = new Button[3];
            for (int i = 0; i < 3; i++)
                view.captureButtons[i] = Button(view, "Capture Item " + i, .06f, .04f + i * .067f, .72f, .102f + i * .067f);
            view.captureUse = Button(view, "Use Capture Item", .40f, .04f, .72f, .135f);
            view.captureUse.GetComponentInChildren<Text>().text = "使う";
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }

        static Button Button(BattleView view, string name, float x, float y, float r, float t)
        {
            var button = Object.Instantiate(view.back, view.back.transform.parent);
            button.name = name;
            Undo.RegisterCreatedObjectUndo(button.gameObject, "Setup Capture");
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(r, t);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            button.GetComponentInChildren<Text>().fontSize = 18;
            button.gameObject.SetActive(false);
            return button;
        }
    }
}
