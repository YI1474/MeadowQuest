using UnityEngine;

namespace MeadowQuest
{
    public enum ItemEffect
    {
        HealHp,
        HealAll,
        RestorePp,
        Revive,
        Capture,
        CureStatus,
        Repel
    }

    [CreateAssetMenu(menuName = "Meadow Quest/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string id, displayName, description;
        public ItemEffect effect;
        public int value, buyPrice, sellPrice, initialCount;
        public bool useInField, useInBattle;
        public bool FieldImplemented => useInField && (effect == ItemEffect.HealHp || effect == ItemEffect.HealAll || effect == ItemEffect.RestorePp || effect == ItemEffect.Revive);
    }

    [System.Serializable]
    public sealed class ItemStack
    {
        public string id;
        public int count;
        public ItemStack(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }
}
