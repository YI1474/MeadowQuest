using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeadowQuest.Editor
{
    public static class MasterDataImporter
    {
        const string Root = "Assets/Game/Data/";
        sealed class MonsterRow
        {
            public string[] row;
            public Sprite front, back;
            public int hp, attack;
        }

        [MenuItem("Meadow Quest/Data/Validate CSV")]
        public static void Validate()
        {
            Run(false);
        }

        [MenuItem("Meadow Quest/Data/Import CSV")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            ContentExpansionSetup.PrepareSprites();
            Run(true);
        }

        static void Run(bool write)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("再生を停止してからCSVを取り込んでください。");
                return;
            }

            try
            {
                var moves = Read("moves.csv", "id,name,element,power,accuracy,maxPp,category");
                var monsters = Read("monsters.csv", "id,name,element,baseHp,baseAttack,front,back,moves,level,baseDefense,baseSpecialAttack,baseSpecialDefense,baseSpeed,baseExperience,evHp,evAttack,evDefense,evSpecialAttack,evSpecialDefense,evSpeed");
                var moveIds = new HashSet<string>(moves.Select(r => r[0]));
                var items = ItemMasterImporter.Validate();
                foreach (var r in moves)
                {
                    Number(r[3], 1, 9999, r[0] + " power");
                    Number(r[4], 1, 100, r[0] + " accuracy");
                    Number(r[5], 1, 999, r[0] + " maxPp");
                    if (r[6] != "Physical" && r[6] != "Special")
                        throw new FormatException(r[0] + ": categoryはPhysical/Specialです。");
                    CheckTarget<MoveDefinition>(Root + "Moves/" + r[0] + ".asset", r[0]);
                }

                var planned = new List<MonsterRow>();
                foreach (var r in monsters)
                {
                    Number(r[8], 1, 100, r[0] + " level");
                    for (int c = 9; c <= 12; c++)
                        Number(r[c], 1, 255, r[0] + " baseStat");
                    Number(r[13], 1, 9999, r[0] + " baseExperience");
                    for (int c = 14; c < 20; c++)
                        Number(r[c], 0, 3, r[0] + " effortYield");
                    var references = r[7].Split('|');
                    if (references.Length > 4 || references.Distinct().Count() != references.Length || references.Any(id => !moveIds.Contains(id)))
                        throw new FormatException(r[0] + ": 技IDが不明・重複、または4個を超えています。");
                    var item = new MonsterRow
                    {
                        row = r,
                        hp = Number(r[3], 1, 255, r[0] + " hp"),
                        attack = Number(r[4], 1, 255, r[0] + " attack"),
                        front = Sprite(r[5]),
                        back = Sprite(r[6])
                    };
                    CheckTarget<MonsterDefinition>(MonsterPath(r[0]), r[0]);
                    planned.Add(item);
                }

                if (!write)
                {
                    Debug.Log($"CSV検証OK: モンスター{monsters.Count}件、技{moves.Count}件、道具{items.Count}件");
                    return;
                }

                // All source rows and references are checked before any assets are changed.
                Directory.CreateDirectory(Root + "Moves");
                Directory.CreateDirectory(Root + "Monsters");
                AssetDatabase.Refresh();
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Import Master CSV");
                var definitions = new Dictionary<string, MoveDefinition>();
                foreach (var r in moves)
                {
                    var data = Get<MoveDefinition>(Root + "Moves/" + r[0] + ".asset");
                    data.id = r[0];
                    data.displayName = r[1];
                    data.element = r[2];
                    data.power = int.Parse(r[3]);
                    data.accuracy = int.Parse(r[4]);
                    data.maxPp = int.Parse(r[5]);
                    data.special = r[6] == "Special";
                    EditorUtility.SetDirty(data);
                    definitions.Add(data.id, data);
                }

                foreach (var item in planned)
                {
                    var r = item.row;
                    var data = Get<MonsterDefinition>(MonsterPath(r[0]));
                    data.id = r[0];
                    data.displayName = r[1];
                    data.element = r[2];
                    data.baseHp = item.hp;
                    data.baseAttack = item.attack;
                    data.level = int.Parse(r[8]);
                    data.baseDefense = int.Parse(r[9]);
                    data.baseSpecialAttack = int.Parse(r[10]);
                    data.baseSpecialDefense = int.Parse(r[11]);
                    data.baseSpeed = int.Parse(r[12]);
                    data.baseExperience = int.Parse(r[13]);
                    data.effortYield = r.Skip(14).Select(int.Parse).ToArray();
                    data.growthDataVersion = 1;
                    data.portrait = item.front;
                    data.backPortrait = item.back;
                    data.moves = r[7].Split('|').Select(id => definitions[id]).ToArray();
                    EditorUtility.SetDirty(data);
                }

                ItemMasterImporter.Apply(items);
                PartySetup.UpdateCatalog();
                Undo.CollapseUndoOperations(group);
                AssetDatabase.SaveAssets();
                ContentExpansionSetup.ConfigureEncounters();
                TrainerSetup.Install();
                Debug.Log($"CSV取り込み完了: モンスター{monsters.Count}件、技{moves.Count}件。既存アセットの参照を維持しました。");
            }
            catch (Exception e)
            {
                Debug.LogError("CSV取り込み中止: " + e.Message);
            }
        }

        static List<string[]> Read(string file, string header)
        {
            var rows = MasterCsv.Parse(File.ReadAllText(Root + "Source/" + file));
            if (rows.Count < 2 || string.Join(",", rows[0]) != header)
                throw new FormatException(file + ": ヘッダーが違うか、データが空です。");
            int count = rows[0].Length;
            rows.RemoveAt(0);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Length != count || r.Any(string.IsNullOrWhiteSpace))
                    throw new FormatException(file + ": データ行" + (i + 1) + "の列数または空欄を確認してください。");
                if (!Regex.IsMatch(r[0], "^[a-z0-9][a-z0-9_]*$") || !ids.Add(r[0]))
                    throw new FormatException(file + ": IDが不正または重複: " + r[0]);
                if (!new[]
                {
                    "Normal",
                    "Grass",
                    "Fire",
                    "Water"
                }.Contains(r[2]))
                    throw new FormatException(file + ": 未対応タイプ: " + r[2]);
            }

            return rows;
        }

        static int Number(string value, int min, int max, string label)
        {
            if (!int.TryParse(value, out int n) || n < min || n > max)
                throw new FormatException(label + ": " + min + "〜" + max + "の整数が必要です。");
            return n;
        }

        static Sprite Sprite(string path)
        {
            var sprite = path.StartsWith("Assets/", StringComparison.Ordinal) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
            if (!sprite)
                throw new FormatException("Spriteが見つかりません: " + path);
            return sprite;
        }

        // Preserve the GUIDs already referenced by the scene.
        static string MonsterPath(string id) => Root + "Monsters/" + (id == "starter" ? "Starter" : id == "wild" ? "Wild" : id) + ".asset";
        static void CheckTarget<T>(string path, string id)
            where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing && !(existing is T))
                throw new FormatException("出力先に異なる種類のアセットがあります: " + path);
            string previous = existing is MonsterDefinition m ? m.id : existing is MoveDefinition a ? a.id : null;
            if (!string.IsNullOrEmpty(previous) && previous != id)
                throw new FormatException("既存IDと一致しません: " + path);
        }

        static T Get<T>(string path)
            where T : ScriptableObject
        {
            var data = AssetDatabase.LoadAssetAtPath<T>(path);
            if (data)
                Undo.RecordObject(data, "Import Master CSV");
            else
            {
                data = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(data, path);
                Undo.RegisterCreatedObjectUndo(data, "Import Master CSV");
            }

            return data;
        }
    }
}
