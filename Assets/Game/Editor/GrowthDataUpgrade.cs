using UnityEditor;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class GrowthDataUpgrade
    {
        static GrowthDataUpgrade() { EditorApplication.delayCall+=Upgrade; }
        static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var data=AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/Starter.asset");
            if(data && data.growthDataVersion<1) MasterDataImporter.Import();
        }
    }
}
