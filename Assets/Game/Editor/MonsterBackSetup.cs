using UnityEditor;
using UnityEngine;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class MonsterBackSetup
    {
        static MonsterBackSetup() { EditorApplication.delayCall += Install; }
        [MenuItem("Meadow Quest/Setup/Monster Back Sprites")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Assign("Starter", "2_0");
            Assign("Wild", "1_0");
            AssetDatabase.SaveAssets();
        }
        static void Assign(string definition, string id)
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/" + definition + ".asset");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/MonsterBacks/" + id + "_back.png");
            if (!data || !sprite || data.backPortrait) return;
            data.backPortrait = sprite;
            EditorUtility.SetDirty(data);
        }
    }
}
