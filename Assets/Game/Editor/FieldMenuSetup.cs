using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class FieldMenuSetup
    {
        static FieldMenuSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => Install();
        }

        [MenuItem("Meadow Quest/Setup/Field Menu")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Game/Scenes/Meadow.unity")
                return;
            var existing = Object.FindAnyObjectByType<FieldMenu>(FindObjectsInactive.Include);
            if (existing)
            {
                AddItemPaging(existing);
                AddPartyCards(existing);
                AddItemList(existing);
                PartySetup.Install();
                if (existing.body && !existing.body.resizeTextForBestFit)
                {
                    existing.body.resizeTextForBestFit = true;
                    existing.body.resizeTextMinSize = 16;
                    existing.body.resizeTextMaxSize = 20;
                    EditorUtility.SetDirty(existing.body);
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                return;
            }

            var player = Object.FindAnyObjectByType<GridMover>();
            var battle = Object.FindAnyObjectByType<BattleController>();
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            if (!player || !battle || !font)
                return;
            var root = new GameObject("Field Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FieldMenu));
            Undo.RegisterCreatedObjectUndo(root, "Setup Field Menu");
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.matchWidthOrHeight = .5f;
            var menu = root.GetComponent<FieldMenu>();
            menu.player = player;
            menu.battle = battle;
            menu.open = Button(root.transform, font, "Menu", "メニュー", .82f, .91f, .98f, .98f);
            var shade = Box(root.transform, "Menu Overlay", 0, 0, 1, 1, new Color(0, 0, 0, .65f));
            menu.panel = shade.gameObject;
            var panel = Box(shade.transform, "Menu Panel", .06f, .08f, .94f, .89f, new Color(.04f, .09f, .14f, .98f));
            menu.title = Label(panel.transform, font, "Heading", "メニュー", .05f, .84f, .75f, .95f, 28);
            menu.body = Label(panel.transform, font, "Content", "", .34f, .17f, .95f, .79f, 20);
            menu.body.resizeTextForBestFit = true;
            menu.body.resizeTextMinSize = 16;
            menu.body.resizeTextMaxSize = 20;
            menu.party = Button(panel.transform, font, "Party", "なかま", .04f, .66f, .28f, .78f);
            menu.items = Button(panel.transform, font, "Items", "どうぐ", .04f, .50f, .28f, .62f);
            menu.save = Button(panel.transform, font, "Save", "セーブ", .04f, .34f, .28f, .46f);
            menu.help = Button(panel.transform, font, "Help", "操作説明", .04f, .18f, .28f, .30f);
            menu.close = Button(panel.transform, font, "Close", "閉じる", .79f, .85f, .96f, .95f);
            menu.back = Button(panel.transform, font, "Back", "戻る", .04f, .035f, .28f, .13f);
            menu.usePotion = Button(panel.transform, font, "Use Potion", "きずぐすりを使う", .48f, .035f, .94f, .13f);
            AddItemPaging(menu);
            AddPartyCards(menu);
            AddItemList(menu);
            PartySetup.Install();
            menu.back.gameObject.SetActive(false);
            menu.usePotion.gameObject.SetActive(false);
            shade.gameObject.SetActive(false);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("フィールドメニューを追加しました。Ctrl+Sでシーン保存。Tab/M/Escで開けます。");
        }

        static Image Box(Transform parent, string name, float x, float y, float right, float top, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Rect(obj.GetComponent<RectTransform>(), x, y, right, top);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static void AddItemPaging(FieldMenu menu)
        {
            if (menu.previousItem && menu.nextItem)
                return;
            if (!menu.usePotion || !menu.body)
                return;
            var parent = menu.usePotion.transform.parent;
            var font = menu.body.font;
            Undo.RecordObject(menu, "Add Item Paging");
            Rect(menu.usePotion.GetComponent<RectTransform>(), .72f, .035f, .94f, .13f);
            menu.usePotion.GetComponentInChildren<Text>().text = "使う";
            if (!menu.previousItem)
                menu.previousItem = Button(parent, font, "Previous Item", "前へ", .34f, .035f, .51f, .13f);
            if (!menu.nextItem)
                menu.nextItem = Button(parent, font, "Next Item", "次へ", .53f, .035f, .70f, .13f);
            menu.previousItem.gameObject.SetActive(false);
            menu.nextItem.gameObject.SetActive(false);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        }

        static void AddItemList(FieldMenu menu)
        {
            if (menu.itemScroll)
                return;
            Undo.RecordObject(menu, "Setup Item List");
            var parent = menu.body.transform.parent;
            var font = menu.body.font;
            var root = Box(parent, "Owned Items Scroll", .34f, .27f, .95f, .79f, new Color(.07f, .15f, .20f));
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Setup Item List");
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            menu.itemScroll = scroll;
            var viewport = Box(root.transform, "Viewport", 0, 0, .95f, 1, new Color(1, 1, 1, .01f));
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            var row = Button(content.transform, font, "Item Row Template", "道具名　× 1", 0, 0, 1, 1);
            var size = row.gameObject.AddComponent<LayoutElement>();
            size.minHeight = 48;
            size.preferredHeight = 48;
            var label = row.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(14, 0);
            label.rectTransform.offsetMax = new Vector2(-10, 0);
            row.gameObject.AddComponent<ScrollSelectionIntoView>();
            row.gameObject.SetActive(false);
            menu.itemRowTemplate = row;
            var track = Box(root.transform, "Scrollbar", .96f, 0, 1, 1, new Color(.1f, .22f, .28f));
            var bar = track.gameObject.AddComponent<Scrollbar>();
            var handle = Box(track.transform, "Handle", 0, 0, 1, 1, new Color(.4f, .7f, .75f));
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            menu.itemListHint = Label(parent, font, "Item List Hint", "", .34f, .16f, .95f, .25f, 17);
            Undo.RegisterCreatedObjectUndo(menu.itemListHint.gameObject, "Setup Item List");
            menu.itemListHint.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            if (menu.previousItem)
                menu.previousItem.gameObject.SetActive(false);
            if (menu.nextItem)
                menu.nextItem.gameObject.SetActive(false);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        }

        static void AddPartyCards(FieldMenu menu)
        {
            if (menu.partyList)
                return;
            var parent = menu.body.transform.parent;
            var font = menu.body.font;
            Undo.RecordObject(menu, "Setup Party Cards");
            var list = new GameObject("Party List", typeof(RectTransform));
            list.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(list, "Setup Party Cards");
            Rect(list.GetComponent<RectTransform>(), .34f, .17f, .95f, .79f);
            menu.partyList = list;
            var card = Box(list.transform, "Companion Card", 0, .64f, 1, 1, new Color(.12f, .26f, .34f));
            menu.monsterCard = card.gameObject.AddComponent<Button>();
            menu.monsterCard.targetGraphic = card;
            var colors = menu.monsterCard.colors;
            colors.highlightedColor = new Color(.75f, 1f, .9f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.white;
            menu.monsterCard.colors = colors;
            menu.monsterIcon = Box(card.transform, "Portrait", .02f, .12f, .22f, .92f, Color.white);
            menu.monsterIcon.preserveAspect = true;
            menu.monsterIcon.raycastTarget = false;
            menu.monsterSummary = Label(card.transform, font, "Summary", "", .26f, .3f, .97f, .91f, 22);
            var track = Box(card.transform, "HP Track", .27f, .13f, .93f, .22f, new Color(.03f, .08f, .11f));
            track.raycastTarget = false;
            menu.partyHp = Box(track.transform, "HP Fill", 0, 0, 1, 1, new Color(.3f, .85f, .6f));
            // A built-in sprite is needed for uGUI's filled-image geometry.
            menu.partyHp.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            menu.partyHp.type = Image.Type.Filled;
            menu.partyHp.fillMethod = Image.FillMethod.Horizontal;
            menu.partyHp.fillOrigin = 0;
            menu.partyHp.raycastTarget = false;
            menu.partyHint = Label(list.transform, font, "Hint", "", 0, .01f, 1, .16f, 18);
            var actions = Box(list.transform, "Companion Actions", .35f, .18f, 1, .60f, new Color(.07f, .16f, .22f));
            menu.partyActions = actions.gameObject;
            menu.viewStats = Button(actions.transform, font, "View Stats", "つよさを見る", .04f, .68f, .96f, .96f);
            menu.reorderParty = Button(actions.transform, font, "Reorder", "並び替える", .04f, .36f, .96f, .64f);
            menu.partyReturn = Button(actions.transform, font, "Return", "戻る", .04f, .04f, .96f, .32f);
            menu.reorderParty.interactable = false;
            actions.gameObject.SetActive(false);
            list.SetActive(false);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        }

        static Text Label(Transform parent, Font font, string name, string text, float x, float y, float right, float top, int size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            Rect(label.rectTransform, x, y, right, top);
            return label;
        }

        static Button Button(Transform parent, Font font, string name, string text, float x, float y, float right, float top)
        {
            var image = Box(parent, name, x, y, right, top, new Color(.15f, .29f, .38f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Label(image.transform, font, "Label", text, 0, 0, 1, 1, 22).alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static void Rect(RectTransform rect, float x, float y, float right, float top)
        {
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
