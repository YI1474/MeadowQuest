using System;
using System.Collections.Generic;
using System.Linq;

namespace MeadowQuest
{
    public sealed class Inventory
    {
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        public Inventory(ItemStack[] items)
        {
            if (items == null)
                throw new ArgumentException("道具データがありません。");
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || item.count < 0 || item.count > 999 || counts.ContainsKey(item.id))
                    throw new ArgumentException("道具のID・所持数が不正です。");
                counts.Add(item.id, item.count);
            }
        }

        public int Count(string id) => counts.TryGetValue(id, out int n) ? n : 0;
        public bool Consume(string id)
        {
            if (Count(id) <= 0)
                return false;
            counts[id]--;
            return true;
        }

        public void Add(string id, int amount)
        {
            if (string.IsNullOrWhiteSpace(id) || amount < 0 || amount > 999 - Count(id))
                throw new ArgumentException();
            counts[id] = Count(id) + amount;
        }

        public ItemStack[] Snapshot() => counts.OrderBy(pair => pair.Key).Select(pair => new ItemStack(pair.Key, pair.Value)).ToArray();
        public bool Use(ItemDefinition item, MonsterIndividual monster, bool inBattle = false)
        {
            if (item == null || !(inBattle ? item.useInBattle && IsRecovery(item) : item.FieldImplemented) || monster == null || Count(item.id) == 0)
                return false;
            bool changed = false;
            switch (item.effect)
            {
                case ItemEffect.HealHp:
                case ItemEffect.HealAll:
                    if (monster.Data.currentHp <= 0 || monster.Data.currentHp >= monster.Hp)
                        break;
                    monster.Data.currentHp = item.effect == ItemEffect.HealAll ? monster.Hp : Math.Min(monster.Hp, monster.Data.currentHp + item.value);
                    changed = true;
                    break;
                case ItemEffect.RestorePp:
                    if (monster.Data.currentHp <= 0)
                        break;
                    for (int i = 0; i < monster.Moves.Length; i++)
                    {
                        int pp = Math.Min(monster.Moves[i].maxPp, monster.Data.pp[i] + item.value);
                        changed |= pp != monster.Data.pp[i];
                        monster.Data.pp[i] = pp;
                    }

                    break;
                case ItemEffect.Revive:
                    if (monster.Data.currentHp != 0)
                        break;
                    monster.Data.currentHp = Math.Max(1, monster.Hp * item.value / 100);
                    changed = true;
                    break;
            }

            if (changed)
                counts[item.id]--;
            return changed;
        }

        public static bool IsRecovery(ItemDefinition item) => item != null && (item.effect == ItemEffect.HealHp || item.effect == ItemEffect.HealAll || item.effect == ItemEffect.RestorePp || item.effect == ItemEffect.Revive);
    }
}
