using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MeadowQuest.Editor
{
    public sealed class PixelSpriteImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Game/Art/"))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }

    [InitializeOnLoad]
    public static class MeadowSceneBuilder
    {
        const string ScenePath = "Assets/Game/Scenes/Meadow.unity";
        static MeadowSceneBuilder()
        {
            EditorApplication.delayCall += CreateIfMissing;
            EditorApplication.delayCall += RepairEmptyMap;
        }

        [MenuItem("Meadow Quest/Repair/Empty Map")]
        public static void RepairEmptyMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;
            var world = GameObject.Find("World");
            if (!world)
                return;
            var ground = world.transform.Find("Ground")?.GetComponent<Tilemap>();
            var walls = world.transform.Find("Obstacles")?.GetComponent<Tilemap>();
            if (!ground || !walls || ground.GetUsedTilesCount() != 0 || walls.GetUsedTilesCount() != 0)
                return;
            FillMap(ground, walls);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ValidateScene();
            Debug.Log("Empty meadow map restored and saved.");
        }

        static void FillMap(Tilemap ground, Tilemap walls)
        {
            var grass = MakeTile("Grass");
            var path = MakeTile("Path");
            var bush = MakeTile("Bush");
            for (int y = 0; y < 17; y++)
                for (int x = 0; x < 25; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    ground.SetTile(cell, x >= 11 && x <= 13 || y >= 7 && y <= 9 ? path : grass);
                    if (x == 0 || x == 24 || y == 0 || y == 16 || x >= 4 && x <= 6 && y >= 11 && y <= 13)
                        walls.SetTile(cell, bush);
                }

            EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(walls);
        }

        public static void CreateIfMissing()
        {
            if (File.Exists(ScenePath) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            Build();
        }

        public static void Build()
        {
            // An editor-only authoring step. The finished scene is saved, not generated at runtime.
            if (File.Exists(ScenePath))
                return;
            foreach (var folder in new[]
            {
                "Scenes",
                "Prefabs",
                "Tiles"
            }

            )
                Directory.CreateDirectory("Assets/Game/" + folder);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grid = new GameObject("World", typeof(Grid));
            var ground = MakeMap(grid, "Ground", 0);
            var walls = MakeMap(grid, "Obstacles", 1);
            FillMap(ground, walls);
            var map = grid.AddComponent<GridMap>();
            map.Configure(ground, walls);
            var player = new GameObject("Player");
            var renderer = player.AddComponent<SpriteRenderer>();
            var frames = new Sprite[12];
            for (int row = 0; row < 4; row++)
                for (int frame = 0; frame < 3; frame++)
                    frames[row * 3 + frame] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Game/Art/Player_{row}_{frame}.png");
            renderer.sprite = frames[1];
            renderer.sortingOrder = 100;
            player.AddComponent<KeyboardMoveInput>();
            player.AddComponent<GridMover>();
            player.AddComponent<WalkSpriteView>().Configure(frames);
            var prefab = PrefabUtility.SaveAsPrefabAsset(player, "Assets/Game/Prefabs/Player.prefab");
            Object.DestroyImmediate(player);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = ground.GetCellCenterWorld(new Vector3Int(12, 8, 0));
            instance.GetComponent<GridMover>().Configure(map);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<GridMover>());
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(12.5f, 8.5f, -10);
            var lens = camera.GetComponent<Camera>();
            lens.orthographic = true;
            lens.orthographicSize = 10;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(.06f, .12f, .14f);
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "Portfolio";
            PlayerSettings.productName = "Meadow Quest";
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            if (SceneView.lastActiveSceneView)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.LookAt(new Vector3(12.5f, 8.5f, 0), Quaternion.identity, 15);
            }

            AssetDatabase.SaveAssets();
            ValidateScene();
            Debug.Log("MEADOW_QUEST_SCENE_READY");
        }

        public static void ValidateScene()
        {
            var map = Object.FindAnyObjectByType<GridMap>();
            var mover = Object.FindAnyObjectByType<GridMover>();
            if (!map || !mover)
                throw new System.InvalidOperationException("Missing map or player.");
            if (!map.IsWalkable(new Vector3Int(12, 8, 0)) || map.IsWalkable(new Vector3Int(0, 0, 0)) || map.IsWalkable(new Vector3Int(-1, 8, 0)) || map.IsWalkable(new Vector3Int(5, 12, 0)))
                throw new System.InvalidOperationException("Walkability validation failed.");
            if (!PrefabUtility.IsPartOfPrefabInstance(mover))
                throw new System.InvalidOperationException("Player is not a prefab instance.");
            var serialized = new SerializedObject(mover);
            if (serialized.FindProperty("map").objectReferenceValue != map)
                throw new System.InvalidOperationException("Player map reference was not saved.");
            var view = new SerializedObject(mover.GetComponent<WalkSpriteView>());
            var sprites = view.FindProperty("frames");
            for (int i = 0; i < 12; i++)
                if (!sprites.GetArrayElementAtIndex(i).objectReferenceValue)
                    throw new System.InvalidOperationException("Missing player frame " + i);
            Debug.Log("MEADOW_QUEST_VALIDATION_OK");
        }

        static Tile MakeTile(string name)
        {
            string path = "Assets/Game/Tiles/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing)
                return existing;
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/" + name + ".png");
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        static Tilemap MakeMap(GameObject parent, string name, int order)
        {
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            obj.transform.SetParent(parent.transform, false);
            obj.GetComponent<TilemapRenderer>().sortingOrder = order;
            return obj.GetComponent<Tilemap>();
        }
    }
}
