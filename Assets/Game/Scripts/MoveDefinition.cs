using UnityEngine;

namespace MeadowQuest
{
    [CreateAssetMenu(menuName = "Meadow Quest/Move")]
    public sealed class MoveDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string element = "Normal";
        public bool special;
        [Min(1)]
        public int power = 10;
        [Range(1, 100)]
        public int accuracy = 100;
        [Min(1)]
        public int maxPp = 20;
    }
}
