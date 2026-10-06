using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class CaveRouteSetup
    {
        const string RootName = "Route - 洞窟と岩峰の街";
        static CaveRouteSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (s, m) => Install();
        }

        [MenuItem("Meadow Quest/Build/Cave And Trial Route")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var town = Object.FindAnyObjectByType<TownWorld>();
            if (!town || town.gameObject.scene.path != "Assets/Game/Scenes/Meadow.unity" || town.transform.Find(RootName))
                return;
            TrainerSetup.Install();
            var trainers = town.residents.Where(r => r.trainer).ToArray();
            if (trainers.Length < 4)
                return;
            var map = town.map ? town.map : Object.FindAnyObjectByType<GridMap>();
            var ground = map.transform.Find("Ground").GetComponent<Tilemap>();
            var walls = map.transform.Find("Obstacles").GetComponent<Tilemap>();
            var stone = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Town/Stone.asset");
            var path = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Path.asset");
            var grass = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Grass.asset");
            var wood = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Game/Tiles/Town/Wood.asset");
            if (!stone || !path || !grass || !wood)
                return;
            Undo.RecordObject(town, "Build Cave Route");
            Undo.RecordObject(map, "Build Cave Route");
            Undo.RecordObject(ground, "Build Cave Route");
            Undo.RecordObject(walls, "Build Cave Route");
            var root = new GameObject(RootName);
            root.transform.SetParent(town.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Build Cave Route");
            var doors = new List<TownWorld.Passage>(town.passages);
            var residents = new List<TownWorld.Resident>(town.residents);
            var floor = Tinted(path, "CaveFloor", new Color(.52f, .55f, .61f));
            var rock = Tinted(stone, "CaveWall", new Color(.36f, .39f, .48f));
            Area(root, "こだまの洞窟", 120, 0, 40, 30);
            for (int y = 0; y < 30; y++)
                for (int x = 0; x < 40; x++)
                {
                    var cell = new Vector3Int(120 + x, y, 0);
                    ground.SetTile(cell, floor);
                    walls.SetTile(cell, CaveLayout.IsFloor(x, y) ? null : rock);
                }

            // Openings are actual floor tiles, not gaps hidden behind a passage trigger.
            Link(doors, new Vector3Int(1, 8, 0), new Vector3Int(139, 2, 0), "洞窟へ");
            Link(doors, new Vector3Int(139, 1, 0), new Vector3Int(2, 8, 0), "洞窟から草原へ");
            for (int x = 0; x <= 4; x++)
            {
                ground.SetTile(new Vector3Int(x, 8, 0), path);
                walls.SetTile(new Vector3Int(x, 8, 0), x == 0 ? stone : null);
            }

            Sign(root, "洞窟入口", 2, 10);
            Area(root, "岩峰の街", 180, 0, 36, 28);
            Fill(ground, walls, 180, 0, 36, 28, grass, stone);
            for (int y = 1; y < 27; y++)
                for (int x = 181; x < 215; x++)
                    if (y >= 6 && y <= 9 || x >= 195 && x <= 199)
                        ground.SetTile(new Vector3Int(x, y, 0), path);
            Link(doors, new Vector3Int(139, 28, 0), new Vector3Int(197, 3, 0), "岩峰の街へ");
            Link(doors, new Vector3Int(197, 1, 0), new Vector3Int(139, 27, 0), "洞窟へ戻る");
            Sign(root, "↓ こだまの洞窟", 197, 2);
            House(root, map, ground, path, 186, 13, "回復所");
            House(root, map, ground, path, 207, 13, "道具屋");
            House(root, map, ground, path, 197, 22, "試練場");
            Area(root, "試練場", 230, 0, 20, 24);
            Fill(ground, walls, 230, 0, 20, 24, wood, stone);
            // A dividing wall gives the first trainer a mandatory crossing, without standing in the doorway.
            for (int x = 231; x < 249; x++)
                if (x < 238 || x > 240)
                    walls.SetTile(new Vector3Int(x, 10, 0), stone);
            Link(doors, new Vector3Int(197, 21, 0), new Vector3Int(239, 2, 0), "試練場へ");
            Link(doors, new Vector3Int(239, 1, 0), new Vector3Int(197, 20, 0), "街へ");
            var clinic = Area(root, "岩峰の回復所", 260, 0, 13, 11);
            clinic.recoveryRoom = "ridge_clinic";
            clinic.recoveryCell = new Vector3Int(266, 2, 0);
            Fill(ground, walls, 260, 0, 13, 11, wood, stone);
            Link(doors, new Vector3Int(186, 12, 0), clinic.recoveryCell, "回復所へ");
            Link(doors, new Vector3Int(266, 1, 0), new Vector3Int(186, 11, 0), "街へ");
            Resident(root, map, residents, 266, 7, "岩峰の回復係", true, false, "仲間を休ませましょう。");
            Area(root, "岩峰の道具屋", 280, 0, 13, 11);
            Fill(ground, walls, 280, 0, 13, 11, wood, stone);
            Link(doors, new Vector3Int(207, 12, 0), new Vector3Int(286, 2, 0), "道具屋へ");
            Link(doors, new Vector3Int(286, 1, 0), new Vector3Int(207, 11, 0), "街へ");
            Resident(root, map, residents, 286, 7, "岩峰の道具係", false, true, "冒険の準備をどうぞ。");
            Resident(root, map, residents, 194, 6, "岩峰の案内人", false, false, "お疲れさま！ 左に回復所、右に道具屋、北に試練場があるよ。試練場でレン、次にアサギへ挑戦しよう。");
            var ids = new[]
            {
                "town_sora",
                "town_nagi",
                "town_ren",
                "town_master"
            };
            var cells = new[]
            {
                new Vector3Int(136, 8, 0),
                new Vector3Int(142, 20, 0),
                new Vector3Int(236, 9, 0),
                new Vector3Int(239, 20, 0)
            };
            var facing = new[]
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.down
            };
            for (int i = 0; i < ids.Length; i++)
            {
                var resident = trainers.First(r => r.trainer.id == ids[i]);
                var npc = resident.trainer;
                Undo.RecordObject(npc, "Move Trainer");
                Undo.RecordObject(npc.transform, "Move Trainer");
                map.RemoveOccupiedCell(resident.cell);
                resident.cell = cells[i];
                map.AddOccupiedCell(resident.cell);
                npc.transform.SetParent(root.transform, true);
                npc.transform.position = map.CenterOf(resident.cell);
                npc.facing = facing[i];
                npc.sightRange = 4;
                npc.sightEnabled = !npc.IsMaster;
                npc.walkFrames = Enumerable.Range(0, 12).Select(frame => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Player_" + (frame / 3) + "_" + (frame % 3) + ".png")).ToArray();
                npc.Pose(npc.facing);
                npc.GetComponent<SpriteRenderer>().sortingOrder = 1000 - Mathf.RoundToInt(npc.transform.position.y * 2);
                npc.prizeMoney = i == 3 ? 600 : 300;
                npc.alert = Sign(npc.gameObject, "！", 0, 0);
                npc.alert.transform.localPosition = new Vector3(0, 1, 0);
                npc.alert.gameObject.SetActive(false);
                EditorUtility.SetDirty(npc);
            }

            var approach = town.GetComponent<TrainerApproach>();
            if (!approach)
                approach = Undo.AddComponent<TrainerApproach>(town.gameObject);
            approach.town = town;
            foreach (var resident in residents)
                if (resident.name == "街の案内人" || resident.name == "草原の案内人")
                    resident.text = "西の洞窟を抜けると岩峰の街。試練場を目指そう！";
            town.residents = residents.ToArray();
            town.passages = doors.ToArray();
            foreach (var door in doors.Where(p => p.cell.x >= 120))
                ground.SetTile(door.cell, path);
            var battleView = Object.FindAnyObjectByType<BattleView>(FindObjectsInactive.Include);
            if (battleView && battleView.clearPanel)
                foreach (var text in battleView.clearPanel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    if (text.name == "Clear Message")
                        text.text = "試練認定！\n\n師範アサギとの勝負に勝った！\n最初の認定を獲得した。\n\n冒険はまだ続けられます。\n終了前にメニューからセーブしてね。";
            // Invisible encounter tilemap is an authored region; entrances remain safe.
            var zone = new GameObject("Cave Encounters", typeof(Tilemap), typeof(GrassEncounter));
            zone.transform.SetParent(map.transform, false);
            Undo.RegisterCreatedObjectUndo(zone, "Cave Encounters");
            var tiles = zone.GetComponent<Tilemap>();
            for (int y = 6; y < 26; y++)
                for (int x = 1; x < 39; x++)
                    if (CaveLayout.IsFloor(x, y) && y != 8 && y != 20)
                        tiles.SetTile(new Vector3Int(120 + x, y, 0), floor);
            var pool = EncounterRoster.Cave.Select(id => AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/" + id + ".asset")).Where(monster => monster).ToArray();
            zone.GetComponent<GrassEncounter>().habitat = EncounterHabitat.Cave;
            zone.GetComponent<GrassEncounter>().Configure(town.player, tiles, town.battle, pool[0]);
            zone.GetComponent<GrassEncounter>().SetPool(pool);
            EditorUtility.SetDirty(town);
            EditorUtility.SetDirty(map);
            EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(walls);
            EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
            AssetDatabase.SaveAssets();
            RouteEntranceRepair.Repair();
            RidgeDoorRepair.Repair();
            Validate(town);
            Debug.Log("洞窟・岩峰の街・試練場と対戦相手を追加しました。Ctrl+Sでシーンを保存してください。");
        }

        static void Fill(Tilemap floor, Tilemap walls, int x, int y, int w, int h, Tile ground, Tile wall)
        {
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                {
                    var cell = new Vector3Int(x + dx, y + dy, 0);
                    floor.SetTile(cell, ground);
                    walls.SetTile(cell, dx == 0 || dy == 0 || dx == w - 1 || dy == h - 1 ? wall : null);
                }
        }

        static void Link(List<TownWorld.Passage> doors, Vector3Int from, Vector3Int to, string text) => doors.Add(new TownWorld.Passage { cell = from, destination = to, label = text });
        static WorldArea Area(GameObject root, string name, int x, int y, int w, int h)
        {
            var obj = new GameObject(name, typeof(WorldArea));
            obj.transform.SetParent(root.transform, false);
            obj.transform.position = new Vector3(x, y, 0);
            var area = obj.GetComponent<WorldArea>();
            area.displayName = name;
            area.size = new Vector2(w, h);
            return area;
        }

        static Tile Tinted(Tile source, string name, Color color)
        {
            string path = "Assets/Game/Tiles/Town/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile)
                return tile;
            tile = Object.Instantiate(source);
            tile.name = name;
            tile.color = color;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        static TextMesh Sign(GameObject root, string text, float x, float y)
        {
            var obj = new GameObject(text, typeof(TextMesh));
            obj.transform.SetParent(root.transform, false);
            obj.transform.position = new Vector3(x + .5f, y + .5f, 0);
            var label = obj.GetComponent<TextMesh>();
            label.text = text;
            label.fontSize = 36;
            label.characterSize = .12f;
            label.anchor = TextAnchor.MiddleCenter;
            label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            obj.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            obj.GetComponent<MeshRenderer>().sortingOrder = 2000;
            return label;
        }

        static void House(GameObject root, GridMap map, Tilemap ground, Tile path, int x, int y, string name)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(root.transform, false);
            obj.transform.position = new Vector3(x + .5f, y, 0);
            obj.transform.localScale = Vector3.one * .75f;
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = TownBuilder.HouseSprite(false);
            renderer.sortingOrder = 1000 - y * 2;
            TownBuilder.AssembleRoof(renderer);
            for (int dx = -3; dx < 3; dx++)
                for (int dy = 0; dy < 4; dy++)
                    map.AddOccupiedCell(new Vector3Int(x + dx, y + dy, 0));
            for (int dy = 8; dy < y; dy++)
                ground.SetTile(new Vector3Int(x, dy, 0), path);
            Sign(root, name, x, y - 1);
        }

        static void Resident(GameObject root, GridMap map, List<TownWorld.Resident> people, int x, int y, string name, bool heal, bool shop, string text)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(root.transform, false);
            var cell = new Vector3Int(x, y, 0);
            obj.transform.position = map.CenterOf(cell);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Player_0_1.png");
            renderer.color = heal ? new Color(1, .75f, .8f) : new Color(.8f, .9f, 1);
            renderer.sortingOrder = 1000 - y * 2;
            people.Add(new TownWorld.Resident { cell = cell, name = name, heal = heal, shop = shop, text = text });
            map.AddOccupiedCell(cell);
        }

        [MenuItem("Meadow Quest/Validate/Cave Route")]
        public static void ValidateMenu()
        {
            var town = Object.FindAnyObjectByType<TownWorld>();
            if (town)
                Validate(town);
        }

        static void Validate(TownWorld town)
        {
            var map = town.map ? town.map : Object.FindAnyObjectByType<GridMap>();
            var seen = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            seen.Add(town.start);
            queue.Enqueue(town.start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in new[]
                {
                    Vector3Int.up,
                    Vector3Int.down,
                    Vector3Int.left,
                    Vector3Int.right
                }

                )
                {
                    var next = cell + step;
                    if (map.IsWalkable(next) && seen.Add(next))
                        queue.Enqueue(next);
                }

                foreach (var door in town.passages)
                    if (door.cell == cell && map.IsWalkable(door.destination) && seen.Add(door.destination))
                        queue.Enqueue(door.destination);
            }

            foreach (var door in town.passages)
                if (!map.IsWalkable(door.cell) || !map.IsWalkable(door.destination) || !seen.Contains(door.cell))
                    throw new System.InvalidOperationException("出入口へ到達できません：" + door.label + " " + door.cell);
            foreach (var r in town.residents.Where(r => r.trainer))
                if (!new[]
                {
                    Vector3Int.up,
                    Vector3Int.down,
                    Vector3Int.left,
                    Vector3Int.right
                }.Any(d => seen.Contains(r.cell + d)))
                    throw new System.InvalidOperationException("対戦相手へ到達できません：" + r.name);
            Debug.Log("洞窟ルートの全出入口と対戦相手への到達確認：成功");
        }
    }
}
