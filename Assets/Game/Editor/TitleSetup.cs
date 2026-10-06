using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class TitleSetup
    {
        static TitleSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (s, m) => Install();
        }

        [MenuItem("Meadow Quest/Setup/Title Screen")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var town = Object.FindAnyObjectByType<TownWorld>();
            if (!town || town.gameObject.scene.path != "Assets/Game/Scenes/Meadow.unity")
                return;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            if (!font)
                return;
            var existing = Object.FindAnyObjectByType<TitleMenu>(FindObjectsInactive.Include);
            if (existing)
            {
                Credits(existing, font);
                return;
            }

            var root = new GameObject("Title Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TitleMenu));
            Undo.RegisterCreatedObjectUndo(root, "Setup Title Screen");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.matchWidthOrHeight = .5f;
            var menu = root.GetComponent<TitleMenu>();
            menu.town = town;
            var panel = Box(root.transform, "Title Panel", 0, 0, 1, 1);
            panel.GetComponent<Image>().color = new Color(.025f, .075f, .10f, 1);
            menu.panel = panel.gameObject;
            var title = Label(panel, font, "Game Title", .1f, .79f, .9f, .94f, 44);
            title.text = "Meadow Quest";
            title.alignment = TextAnchor.MiddleCenter;
            menu.description = Label(panel, font, "Description", .12f, .37f, .88f, .76f, 22);
            menu.description.alignment = TextAnchor.MiddleCenter;
            menu.newGame = Button(panel, font, "New Game", "はじめから", .32f, .27f, .68f, .36f);
            menu.continueGame = Button(panel, font, "Continue", "つづきから", .32f, .16f, .68f, .25f);
            menu.help = Button(panel, font, "Help", "操作説明", .32f, .05f, .68f, .14f);
            menu.confirm = Button(panel, font, "Confirm New Game", "新しい冒険を始める", .27f, .19f, .73f, .29f);
            menu.back = Button(panel, font, "Back", "戻る", .32f, .06f, .68f, .15f);
            menu.confirm.gameObject.SetActive(false);
            menu.back.gameObject.SetActive(false);
            Credits(menu, font);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
        }

        static void Credits(TitleMenu menu, Font font)
        {
            if (!menu.panel || menu.panel.transform.Find("Artwork Credit"))
                return;
            var text = Label(menu.panel.transform, font, "Artwork Credit", .02f, .005f, .98f, .043f, 13);
            text.text = "Includes Guardian Monsters Artwork by Georg Eckert / lucidtanooki (CC BY 4.0)";
            text.alignment = TextAnchor.MiddleCenter;
            Undo.RegisterCreatedObjectUndo(text.gameObject, "Add Artwork Credit");
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        }

        static RectTransform Box(Transform parent, string name, float x, float y, float r, float t)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(r, t);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            obj.GetComponent<Image>().color = new Color(.07f, .17f, .22f, .98f);
            return rect;
        }

        static Text Label(Transform parent, Font font, string name, float x, float y, float r, float t, int size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = new Vector2(x, y);
            text.rectTransform.anchorMax = new Vector2(r, t);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return text;
        }

        static Button Button(Transform parent, Font font, string name, string caption, float x, float y, float r, float t)
        {
            var rect = Box(parent, name, x, y, r, t);
            rect.GetComponent<Image>().color = new Color(.16f, .32f, .4f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var text = Label(rect, font, "Label", 0, 0, 1, 1, 20);
            text.text = caption;
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
