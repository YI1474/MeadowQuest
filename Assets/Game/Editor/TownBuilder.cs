using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class TownBuilder
    {
        static readonly List<TownWorld.Passage> doors = new List<TownWorld.Passage>();
        static readonly List<TownWorld.Resident> people = new List<TownWorld.Resident>();
        static TownBuilder()
        {
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Meadow Quest/Build/Town")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Game/Scenes/Meadow.unity" || Object.FindAnyObjectByType<TownWorld>())
                return;
            var map = Object.FindAnyObjectByType<GridMap>();
            var mover = Object.FindAnyObjectByType<GridMover>();
            var battle = Object.FindAnyObjectByType<BattleController>();
            var dialogue = Object.FindAnyObjectByType<DialogueController>();
            if (!map || !mover || !battle || !dialogue)
            {
                Debug.LogWarning("先にMeadowシーンの戦闘・会話をセットアップしてください。");
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/Town/WorldAtlas.png"))
                return;
            Directory.CreateDirectory("Assets/Game/Tiles/Town");
            AssetDatabase.Refresh();
            var ground = map.transform.Find("Ground").GetComponent<Tilemap>();
            var walls = map.transform.Find("Obstacles").GetComponent<Tilemap>();
            var grass = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Grass.asset");
            var path = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");
            var bush = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Bush.asset");
            var wood = Tile("Wood", 400, 480);
            var stone = Tile("Stone", 400, 208);
            var water = Tile("Water", 224, 16);
            var root = new GameObject("Town - はじまりの街");
            var town = root.AddComponent<TownWorld>();
            Undo.RegisterCreatedObjectUndo(root, "Build Town");
            doors.Clear();
            people.Clear();
            for (int y = 0; y < 44; y++)
                for (int x = 40; x < 104; x++)
                {
                    bool road = y >= 20 && y <= 24 || x >= 67 && x <= 73 || y >= 27 && y <= 29 || y >= 5 && y <= 7;
                    ground.SetTile(new Vector3Int(x, y, 0), road ? path : grass);
                    if (x == 40 || x == 103 || y == 0 || y == 43)
                        walls.SetTile(new Vector3Int(x, y, 0), bush);
                }

            // Short approach paths connect every front door to the broad main streets.
            foreach (int x in new[]
            {
                52,
                68,
                88
            }

            )
                for (int y = 25; y < 30; y++)
                    ground.SetTile(new Vector3Int(x, y, 0), path);
            for (int y = 8; y < 20; y++)
                foreach (int x in new[]
                {
                    52,
                    88
                }

                )
                    ground.SetTile(new Vector3Int(x, y, 0), path);
            for (int y = 22; y < 25; y++)
                for (int x = 76; x < 81; x++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), stone);
                    if (x > 76 && x < 80 && y == 23)
                        walls.SetTile(new Vector3Int(x, y, 0), water);
                }

            for (int y = 12; y < 18; y++)
                for (int x = 94; x < 100; x++)
                    walls.SetTile(new Vector3Int(x, y, 0), water);
            for (int x = 44; x < 101; x += 8)
                foreach (int y in new[]
                {
                    3,
                    40
                }

                )
                    Visual(root, "Tree", Sprite("Tree", 0, 240, 32, 64), x, y, 1);
            House(root, map, ground, walls, wood, stone, 52, 30, 0, "自宅", true, "家族", "ここはきみの家。いつでも休みに帰ってきてね。", 0);
            House(root, map, ground, walls, wood, stone, 68, 30, 20, "回復所", true, "回復係", "冒険を続ける前に、仲間を休ませましょう。", 1);
            House(root, map, ground, walls, wood, stone, 88, 30, 40, "民家・庭師", false, "庭師", "街の東には池があるよ。西の門から草原へ行けるんだ。", 2);
            House(root, map, ground, walls, wood, stone, 52, 8, 60, "民家・研究家", false, "研究家", "同じ種類でも素質や性格が違うよ。なかま画面で見比べてみよう。", 0);
            House(root, map, ground, walls, wood, stone, 88, 8, 80, "民家・旅人", false, "旅人", "戦いで減ったHPやPPは持ち越される。回復所で休むのを忘れずに！", 2);
            Resident(root, map, 70, 25, "街の案内人", "ようこそ、はじまりの街へ！ 北西が自宅、北が回復所。西の門の先に草原があります。", false);
            Resident(root, map, 44, 23, "門番", "左の道へ進むと草原です。準備はできたかな？", false);
            people.Add(new TownWorld.Resident { cell = new Vector3Int(16, 8, 0), name = "草原の案内人", text = "草むらで野生のモンスターに出会えます。右の道を進むと街へ戻れます。" });
            walls.SetTile(new Vector3Int(24, 8, 0), null);
            doors.Add(new TownWorld.Passage { cell = new Vector3Int(23, 8, 0), destination = new Vector3Int(42, 22, 0), label = "街へ" });
            doors.Add(new TownWorld.Passage { cell = new Vector3Int(41, 22, 0), destination = new Vector3Int(22, 8, 0), label = "草原へ" });
            var serialized = new SerializedObject(dialogue);
            town.player = mover;
            town.battle = battle;
            town.lens = Camera.main;
            town.panel = (GameObject)serialized.FindProperty("panel").objectReferenceValue;
            town.body = (Text)serialized.FindProperty("body").objectReferenceValue;
            town.hint = (Text)serialized.FindProperty("hint").objectReferenceValue;
            Undo.RecordObject(dialogue, "Town dialogue");
            dialogue.enabled = false;
            town.passages = doors.ToArray();
            town.residents = people.ToArray();
            mover.transform.position = map.CenterOf(town.start);
            PrefabUtility.RecordPrefabInstancePropertyModifications(mover.transform);
            if (town.lens)
            {
                town.lens.transform.position = new Vector3(70.5f, 22.5f, -10);
                town.lens.orthographicSize = 8;
            }

            EditorUtility.SetDirty(map);
            EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(walls);
            EditorUtility.SetDirty(town);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Validate(map, town);
            TownRepair.Repair();
            TownGateRepair.Repair();
            TownAuthoringAudit.Apply();
            ShopSetup.Install();
            Debug.Log("64×44の街、自宅、民家3軒、回復所を追加しました。Ctrl+Sでシーンを保存してください。");
        }

        static void House(GameObject root, GridMap map, Tilemap ground, Tilemap walls, Tile wood, Tile stone, int x, int y, int roomX, string title, bool heal, string person, string text, int style)
        {
            var house = Visual(root, title, HouseSprite(style == 0), x + .5f, y, .75f);
            if (style == 2)
                house.GetComponent<SpriteRenderer>().color = new Color(.8f, .85f, .95f);
            AssembleRoof(house.GetComponent<SpriteRenderer>());
            var sign = new GameObject("看板 - " + title, typeof(TextMesh));
            sign.transform.SetParent(house.transform, false);
            sign.transform.localPosition = new Vector3(0, -.5f, 0);
            var label = sign.GetComponent<TextMesh>();
            label.text = title;
            label.fontSize = 32;
            label.characterSize = .12f;
            label.anchor = TextAnchor.MiddleCenter;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            label.font = font;
            sign.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            sign.GetComponent<MeshRenderer>().sortingOrder = 2000;
            for (int dy = 0; dy < 4; dy++)
                for (int dx = -3; dx < 3; dx++)
                    map.AddOccupiedCell(new Vector3Int(x + dx, y + dy, 0));
            for (int ry = 80; ry <= 90; ry++)
                for (int rx = roomX; rx <= roomX + 12; rx++)
                {
                    var cell = new Vector3Int(rx, ry, 0);
                    ground.SetTile(cell, wood);
                    if (rx == roomX || rx == roomX + 12 || ry == 80 || ry == 90)
                        walls.SetTile(cell, stone);
                }

            var furniture = new GameObject(title + " - 室内家具");
            furniture.transform.SetParent(root.transform);
            Resident(furniture, map, roomX + 6, 87, person, text, heal);
            doors.Add(new TownWorld.Passage { cell = new Vector3Int(x, y - 1, 0), destination = new Vector3Int(roomX + 6, 82, 0), label = title + "に入る" });
            doors.Add(new TownWorld.Passage { cell = new Vector3Int(roomX + 6, 81, 0), destination = new Vector3Int(x, y - 2, 0), label = "外へ" });
        }

        static void Resident(GameObject parent, GridMap map, int x, int y, string name, string text, bool heal)
        {
            var obj = Visual(parent, name, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Player_0_1.png"), x + .5f, y + .5f, 1);
            obj.GetComponent<SpriteRenderer>().color = heal ? new Color(1, .75f, .8f) : new Color(.8f, .9f, 1);
            map.AddOccupiedCell(new Vector3Int(x, y, 0));
            people.Add(new TownWorld.Resident { cell = new Vector3Int(x, y, 0), name = name, text = text, heal = heal });
        }

        static GameObject Visual(GameObject parent, string name, Sprite sprite, float x, float y, float scale)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent.transform);
            obj.transform.position = new Vector3(x, y, 0);
            obj.transform.localScale = Vector3.one * scale;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1000 - Mathf.RoundToInt(y * 2);
            return obj;
        }

        public static Sprite TableSprite() => Sprite("TableCleanV2", 560, 984, 32, 40);
        public static Sprite HouseSprite(bool red)
        {
            return Sprite(red ? "HouseFacadeRedV3" : "HouseFacadeYellowV3", 704, red ? 688 : 496, 128, 32);
        }

        public static void AssembleRoof(SpriteRenderer facade)
        {
            var existing = facade.transform.Find("Roof - assembled");
            var roof = existing ? existing.GetComponent<SpriteRenderer>() : null;
            if (!roof)
            {
                var obj = new GameObject("Roof - assembled", typeof(SpriteRenderer));
                obj.transform.SetParent(facade.transform, false);
                roof = obj.GetComponent<SpriteRenderer>();
                Undo.RegisterCreatedObjectUndo(obj, "Repair Roof");
            }

            roof.sprite = Sprite("CompleteHipRoofV3", 880, 192, 128, 112);
            roof.transform.localPosition = new Vector3(0, 2, 0);
            roof.transform.localScale = new Vector3(1, 4f / 7f, 1);
            roof.sortingOrder = facade.sortingOrder + 1;
            roof.color = facade.color;
            EditorUtility.SetDirty(roof);
        }

        static Sprite Sprite(string name, int x, int top, int width, int height)
        {
            string path = "Assets/Game/Tiles/Town/" + name + "Sprite.asset";
            var result = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (result)
                return result;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/Town/WorldAtlas.png");
            result = UnityEngine.Sprite.Create(texture, new Rect(x, texture.height - top - height, width, height), new Vector2(.5f, width == 16 && height == 16 ? .5f : 0), 16);
            result.name = name;
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static Tile Tile(string name, int x, int top)
        {
            string path = "Assets/Game/Tiles/Town/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile)
                return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite(name, x, top, 16, 16);
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        static void Validate(GridMap map, TownWorld town)
        {
            foreach (var p in town.passages)
                if (!map.IsWalkable(p.cell) || !map.IsWalkable(p.destination))
                    throw new System.InvalidOperationException("通行できない出入口: " + p.label);
            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(town.start);
            visited.Add(town.start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var step in new[]
                {
                    Vector3Int.up,
                    Vector3Int.down,
                    Vector3Int.left,
                    Vector3Int.right
                }

                )
                {
                    var n = c + step;
                    if (map.IsWalkable(n) && visited.Add(n))
                        queue.Enqueue(n);
                }

                foreach (var p in town.passages)
                    if (p.cell == c && visited.Add(p.destination))
                        queue.Enqueue(p.destination);
            }

            foreach (var p in town.passages)
                if (!visited.Contains(p.cell))
                    throw new System.InvalidOperationException("到達できない出入口: " + p.label);
            Debug.Log("街・室内・草原の出入口と接続を検証しました。");
        }
    }
}
