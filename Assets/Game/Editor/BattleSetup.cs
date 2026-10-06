using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class BattleSetup
    {
        static BattleSetup()
        {
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Meadow Quest/Setup/Battle Foundation")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Game/Scenes/Meadow.unity" || GameObject.Find("Battle System"))
                return;
            var player = Object.FindAnyObjectByType<GridMover>();
            var world = GameObject.Find("World");
            if (!player || !world)
                return;
            System.IO.Directory.CreateDirectory("Assets/Game/Data/Monsters");
            AssetDatabase.Refresh();
            var starter = Monster("Starter", "アオマル", 30, 8);
            var enemy = Monster("Wild", "クサトカゲ", 24, 5);
            var grassObj = new GameObject("Encounter Grass", typeof(Tilemap), typeof(TilemapRenderer));
            grassObj.transform.SetParent(world.transform, false);
            grassObj.GetComponent<TilemapRenderer>().sortingOrder = 2;
            var grass = grassObj.GetComponent<Tilemap>();
            const string tilePath = "Assets/Game/Tiles/EncounterGrass.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (!tile)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Grass.png");
                tile.color = new Color(.45f, .7f, .48f);
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            for (int y = 3; y <= 6; y++)
                for (int x = 3; x <= 9; x++)
                    grass.SetTile(new Vector3Int(x, y, 0), tile);
            EditorUtility.SetDirty(grass);
            var canvasObj = new GameObject("Battle Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BattleView));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 600);
            scaler.matchWidthOrHeight = .5f;
            var screen = canvasObj.GetComponent<BattleView>();
            screen.root = canvasObj;
            Panel("Backdrop", canvasObj.transform, new Vector2(0, 0), new Vector2(1, 1), new Color(.055f, .11f, .16f));
            var arena = Panel("Arena", canvasObj.transform, new Vector2(.04f, .3f), new Vector2(.96f, .87f), new Color(.17f, .31f, .27f));
            Label("Title", canvasObj.transform, new Vector2(.06f, .89f), new Vector2(.95f, .98f), "野生のモンスターとの戦闘", 28);
            screen.enemyPortrait = Panel("Enemy Portrait", arena.transform, new Vector2(.65f, .42f), new Vector2(.91f, .94f), Color.white);
            screen.enemyPortrait.sprite = enemy.portrait;
            screen.enemyPortrait.preserveAspect = true;
            screen.enemyPortrait.raycastTarget = false;
            screen.playerPortrait = Panel("Partner Portrait", arena.transform, new Vector2(.09f, .04f), new Vector2(.35f, .56f), Color.white);
            screen.playerPortrait.sprite = starter.portrait;
            screen.playerPortrait.preserveAspect = true;
            screen.playerPortrait.raycastTarget = false;
            screen.enemyLabel = Label("Enemy HP", arena.transform, new Vector2(.06f, .66f), new Vector2(.56f, .93f), "クサトカゲ\nHP 24 / 24", 24);
            screen.playerLabel = Label("Partner HP", arena.transform, new Vector2(.55f, .09f), new Vector2(.96f, .36f), "アオマル\nHP 30 / 30", 24);
            screen.message = Label("Battle Message", canvasObj.transform, new Vector2(.06f, .17f), new Vector2(.94f, .28f), "どうする？", 22);
            screen.attack = Button("Attack", "攻撃", canvasObj.transform, .06f, .31f);
            screen.escape = Button("Escape", "逃げる", canvasObj.transform, .37f, .62f);
            screen.close = Button("Continue", "草原に戻る", canvasObj.transform, .68f, .94f);
            screen.close.gameObject.SetActive(false);
            if (!Object.FindAnyObjectByType<EventSystem>())
            {
                var events = new GameObject("EventSystem", typeof(EventSystem));
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var systems = new GameObject("Battle System");
            var battle = systems.AddComponent<BattleController>();
            battle.Configure(player, starter, screen, GameObject.Find("Dialogue Canvas"));
            systems.AddComponent<GrassEncounter>().Configure(player, grass, battle, enemy);
            // Keep the saved editor view focused on the map; children remain editable in Hierarchy.
            canvas.enabled = false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Battle foundation saved: data, encounter grass, Canvas and EventSystem.");
        }

        static MonsterDefinition Monster(string file, string name, int hp, int attack)
        {
            string path = "Assets/Game/Data/Monsters/" + file + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(path);
            if (data)
                return data;
            data = ScriptableObject.CreateInstance<MonsterDefinition>();
            data.displayName = name;
            data.maxHp = hp;
            data.attack = attack;
            data.portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/" + file + ".png");
            if (!data.portrait)
                throw new System.InvalidOperationException("Missing sprite: " + file);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static void Rect(Transform obj, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)obj;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Rect(obj.transform, min, max);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static Text Label(string name, Transform parent, Vector2 min, Vector2 max, string content, int size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Rect(obj.transform, min, max);
            var text = obj.GetComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            text.fontSize = size;
            text.text = content;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static Button Button(string name, string text, Transform parent, float left, float right)
        {
            var image = Panel(name, parent, new Vector2(left, .045f), new Vector2(right, .14f), new Color(.2f, .38f, .48f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var caption = Label("Label", image.transform, Vector2.zero, Vector2.one, text, 24);
            caption.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
