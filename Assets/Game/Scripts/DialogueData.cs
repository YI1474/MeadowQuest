using UnityEngine;

namespace MeadowQuest
{
    [CreateAssetMenu(menuName = "Meadow Quest/Dialogue")]
    public sealed class DialogueData : ScriptableObject
    {
        public string speaker = "Guide";
        [TextArea(2, 5)]
        public string[] lines;
    }
}
