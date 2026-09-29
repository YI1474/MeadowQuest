using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class PartySetup
    {
        static PartySetup() { EditorApplication.delayCall+=Install; EditorSceneManager.sceneOpened+=(s,m)=>Install(); }
        public static void UpdateCatalog()
        {
            const string path="Assets/Game/Resources/MonsterCatalog.asset";
            System.IO.Directory.CreateDirectory("Assets/Game/Resources"); AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<MonsterCatalog>(path);
            if(!catalog) { catalog=ScriptableObject.CreateInstance<MonsterCatalog>(); AssetDatabase.CreateAsset(catalog,path); }
            catalog.monsters=AssetDatabase.FindAssets("t:MonsterDefinition",new[]{"Assets/Game/Data/Monsters"}).Select(g=>AssetDatabase.LoadAssetAtPath<MonsterDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }
        [MenuItem("Meadow Quest/Setup/Party Menu")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!AssetDatabase.LoadAssetAtPath<MonsterCatalog>("Assets/Game/Resources/MonsterCatalog.asset")) UpdateCatalog();
            var menu=Object.FindAnyObjectByType<FieldMenu>(FindObjectsInactive.Include);
            if(!menu || !menu.monsterCard || menu.partyCards.Length==6) return;
            Undo.RecordObject(menu,"Expand Party Menu");
            menu.partyCards=new Button[6]; menu.partyCards[0]=menu.monsterCard;
            for(int i=1;i<6;i++) { menu.partyCards[i]=Object.Instantiate(menu.monsterCard,menu.monsterCard.transform.parent); menu.partyCards[i].name="Companion Card "+(i+1); Undo.RegisterCreatedObjectUndo(menu.partyCards[i].gameObject,"Expand Party Menu"); }
            foreach(var card in menu.partyCards)
            {
                var summary=card.transform.Find("Summary").GetComponent<Text>(); summary.fontSize=18; summary.resizeTextForBestFit=true; summary.resizeTextMinSize=14; summary.resizeTextMaxSize=18;
            }
            menu.partyActions.transform.SetAsLastSibling();
            EditorUtility.SetDirty(menu); EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        }
    }
}
