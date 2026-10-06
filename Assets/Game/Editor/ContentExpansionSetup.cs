using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class ContentExpansionSetup
    {
        static ContentExpansionSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => Install();
        }

        static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            PrepareSprites();
            if (!AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemMasterImporter.Folder + "potion.asset"))
                MasterDataImporter.Import();
            else
                ConfigureEncounters(false);
        }

        public static void PrepareSprites()
        {
            foreach (int n in new[]
            {
                3,
                4,
                5,
                6,
                9,
                10,
                18,
                20
            }

            )
            {
                string path = "Assets/Game/Art/Monsters/" + n + "_0.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.spritePixelsPerUnit != 32 || importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        public static void ConfigureEncounters(bool replace = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            foreach (var encounter in Object.FindObjectsByType<GrassEncounter>(FindObjectsInactive.Include))
            {
                var serialized = new SerializedObject(encounter);
                // Migrate the previously authored cave, whose enum defaults to Meadow.
                bool cave = encounter.habitat == EncounterHabitat.Cave || encounter.habitat == EncounterHabitat.Meadow && encounter.name == "Cave Encounters";
                if (encounter.habitat == EncounterHabitat.Custom)
                    continue;
                if (!replace && serialized.FindProperty("enemyPool").arraySize > 0)
                    continue;
                var ids = cave ? EncounterRoster.Cave : EncounterRoster.Meadow;
                var pool = ids.Select(id => AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/" + (id == "wild" ? "Wild" : id) + ".asset")).ToArray();
                if (pool.Any(monster => !monster))
                {
                    if (replace)
                        Debug.LogWarning("出現リストのモンスターが不足しています。Data > Import CSVを実行してください。");
                    continue;
                }

                Undo.RecordObject(encounter, "Configure Encounter Pool");
                encounter.SetPool(pool);
                encounter.habitat = cave ? EncounterHabitat.Cave : EncounterHabitat.Meadow;
                EditorUtility.SetDirty(encounter);
                EditorSceneManager.MarkSceneDirty(encounter.gameObject.scene);
            }
        }
    }
}
