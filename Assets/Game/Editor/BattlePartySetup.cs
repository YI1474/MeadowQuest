using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class BattlePartySetup
    {
        static BattlePartySetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (s, m) => Install();
        }

        [MenuItem("Meadow Quest/Setup/Battle Party And Recovery")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var view = Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if (!view || !view.back)
                return;
            if (view.partyButtons.Length == 6 && view.defeatShade)
                return;
            Undo.RecordObject(view, "Setup Battle Party");
            if (view.partyButtons.Length != 6)
            {
                view.partyButtons = new Button[6];
                for (int i = 0; i < 6; i++)
                {
                    var button = Object.Instantiate(view.back, view.back.transform.parent);
                    button.name = "Battle Party " + i;
                    Undo.RegisterCreatedObjectUndo(button.gameObject, "Setup Battle Party");
                    var rect = button.GetComponent<RectTransform>();
                    float x = .06f + (i % 2) * .34f, y = .18f - (i / 2) * .07f;
                    rect.anchorMin = new Vector2(x, y);
                    rect.anchorMax = new Vector2(x + .32f, y + .06f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var text = button.GetComponentInChildren<Text>();
                    text.fontSize = 16;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 12;
                    text.resizeTextMaxSize = 16;
                    view.partyButtons[i] = button;
                    button.gameObject.SetActive(false);
                }
            }

            if (!view.defeatShade)
            {
                var shade = new GameObject("Defeat Fade", typeof(RectTransform), typeof(Image));
                shade.transform.SetParent(view.root.transform, false);
                Undo.RegisterCreatedObjectUndo(shade, "Setup Recovery Fade");
                var rect = shade.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                view.defeatShade = shade.GetComponent<Image>();
                view.defeatShade.color = Color.black;
                var label = new GameObject("Recovery Message", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(shade.transform, false);
                rect = label.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(.1f, .3f);
                rect.anchorMax = new Vector2(.9f, .7f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                view.defeatText = label.GetComponent<Text>();
                view.defeatText.font = view.message.font;
                view.defeatText.fontSize = 28;
                view.defeatText.color = Color.white;
                view.defeatText.alignment = TextAnchor.MiddleCenter;
                view.defeatText.raycastTarget = false;
                shade.SetActive(false);
            }

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
    }
}
