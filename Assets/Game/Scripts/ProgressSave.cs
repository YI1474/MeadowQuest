using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MeadowQuest
{
    [Serializable]
    public sealed class ProgressSaveData
    {
        public int version;
        public IndividualData companion;
        public IndividualData[] party;
        public int potions;
        public ItemStack[] inventory;
        public string[] defeatedTrainers;
        public string[] rematchSpent;
        public int money;
        public bool captureGiftReceived;
        public string recoveryRoom;
        public bool hasPosition;
        public int cellX, cellY;
    }

    public static class ProgressSave
    {
        public static string PathName => Path.Combine(Application.persistentDataPath, "meadow-progress-v1.json");

        public static MonsterIndividual Load(MonsterDefinition definition, out ProgressSaveData saved, Func<string, MonsterDefinition> resolve = null)
        {
            saved = null;
            if (!File.Exists(PathName))
                return null;
            return Decode(definition, File.ReadAllText(PathName), out saved, resolve);
        }

        public static MonsterIndividual Decode(MonsterDefinition definition, string json, out ProgressSaveData saved, Func<string, MonsterDefinition> resolve = null)
        {
            saved = JsonUtility.FromJson<ProgressSaveData>(json);
            if (saved == null || saved.version < 1 || saved.version > 9)
                throw new InvalidDataException("未対応のセーブ形式です。元ファイルは変更していません。");
            if (saved.version == 1)
            {
                saved.potions = 3;
                saved.hasPosition = false;
            }

            if (saved.potions < 0 || saved.potions > 999)
                throw new InvalidDataException("道具の所持数が不正です。");
            if (saved.version < 3 && saved.companion != null)
                saved.companion.nature = Progression.MigrateLegacyNature(saved.companion.nature);
            if (saved.version < 4)
                saved.inventory = new[]
                {
                    new ItemStack("potion", saved.potions)
                };
            new Inventory(saved.inventory); // Validate before allowing gameplay; retain unknown IDs for forward compatibility.
            if (saved.version < 5)
                saved.defeatedTrainers = Array.Empty<string>();
            ValidateTrainers(saved.defeatedTrainers);
            if (saved.version < 9)
                saved.rematchSpent = Array.Empty<string>();
            ValidateTrainers(saved.rematchSpent);
            foreach (string id in saved.rematchSpent)
                if (!saved.defeatedTrainers.Contains(id))
                    throw new InvalidDataException("再戦の記録が不正です。");
            if (saved.version < 6)
                saved.party = new[]
                {
                    saved.companion
                };
            if (saved.party == null || saved.party.Length < 1 || saved.party.Length > MonsterParty.Capacity || saved.party.Any(m => m == null))
                throw new InvalidDataException("手持ちデータが不正です。");
            var party = new MonsterParty(saved.party.Select(data => new MonsterIndividual(resolve != null ? resolve(data.speciesId) : definition, data)));
            if (saved.version < 7)
            {
                saved.money = ShopRules.StartingMoney;
                saved.captureGiftReceived = saved.inventory.Any(item => item.id == "bond_thread" && item.count > 0);
            }

            if (saved.money < 0 || saved.money > ShopRules.MaxMoney)
                throw new InvalidDataException("所持金が範囲外です。");
            if (saved.version < 8)
                saved.recoveryRoom = "home";
            if (saved.recoveryRoom != "home" && saved.recoveryRoom != "clinic" && saved.recoveryRoom != "ridge_clinic")
                throw new InvalidDataException("復帰先が不正です。");
            saved.version = 9;
            saved.companion = saved.party[0];
            return party.Lead;
        }

        public static void Save(MonsterIndividual individual, int potions, Vector3Int? position)
        {
            Save(individual, new[] { new ItemStack("potion", potions) }, position);
        }

        public static void Save(MonsterIndividual individual, ItemStack[] inventory, Vector3Int? position, string[] defeatedTrainers = null)
        {
            Save(new[] { individual }, inventory, position, defeatedTrainers);
        }

        public static void Save(System.Collections.Generic.IReadOnlyList<MonsterIndividual> party, ItemStack[] inventory, Vector3Int? position, string[] defeatedTrainers = null, int money = ShopRules.StartingMoney, bool captureGiftReceived = false, string recoveryRoom = "home", string[] rematchSpent = null)
        {
            var members = new MonsterParty(party);
            if (recoveryRoom != "home" && recoveryRoom != "clinic" && recoveryRoom != "ridge_clinic")
                throw new ArgumentException("復帰先が不正です。");
            if (money < 0 || money > ShopRules.MaxMoney)
                throw new ArgumentOutOfRangeException(nameof(money));
            var validated = new Inventory(inventory);
            defeatedTrainers = defeatedTrainers ?? Array.Empty<string>();
            ValidateTrainers(defeatedTrainers);
            rematchSpent = rematchSpent ?? Array.Empty<string>();
            ValidateTrainers(rematchSpent);
            if (rematchSpent.Any(id => !defeatedTrainers.Contains(id)))
                throw new ArgumentException("再戦の記録が不正です。");
            var saved = new ProgressSaveData
            {
                rematchSpent = rematchSpent,
                version = 9,
                recoveryRoom = recoveryRoom,
                money = money,
                captureGiftReceived = captureGiftReceived,
                companion = members.Lead.Data,
                party = members.Members.Select(m => m.Data).ToArray(),
                potions = validated.Count("potion"),
                inventory = validated.Snapshot(),
                defeatedTrainers = defeatedTrainers,
                hasPosition = position.HasValue,
                cellX = position?.x ?? 0,
                cellY = position?.y ?? 0
            };
            Directory.CreateDirectory(Path.GetDirectoryName(PathName));
            string temp = PathName + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(saved, true));
            if (File.Exists(PathName))
                File.Replace(temp, PathName, PathName + ".bak");
            else
                File.Move(temp, PathName);
        }

        static void ValidateTrainers(string[] ids)
        {
            if (ids == null)
                throw new InvalidDataException("対戦相手の記録がありません。");
            var unique = new System.Collections.Generic.HashSet<string>();
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id) || !unique.Add(id))
                    throw new InvalidDataException("対戦相手の記録が不正です。");
        }
    }
}
