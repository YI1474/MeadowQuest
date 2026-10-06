using UnityEngine;

namespace MeadowQuest
{
    public sealed class MonsterCatalog : ScriptableObject
    {
        public MonsterDefinition[] monsters;
        public MonsterDefinition Find(string id) => System.Array.Find(monsters, m => m && m.id == id);
    }
}
