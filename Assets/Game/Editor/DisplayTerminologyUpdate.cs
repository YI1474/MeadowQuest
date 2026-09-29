using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class DisplayTerminologyUpdate
    {
        static DisplayTerminologyUpdate()
        {
            EditorApplication.delayCall+=Apply;
            EditorSceneManager.sceneOpened+=(scene,mode)=>Apply();
        }
        static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach(var town in Object.FindObjectsByType<TownWorld>(FindObjectsInactive.Include))
            {
                if(town.residents==null) continue;
                bool changed=false;
                foreach(var resident in town.residents)
                {
                    if(resident==null || resident.text==null) continue;
                    string updated=resident.text.Replace("個体値","素質").Replace("努力値","鍛錬値").Replace("種族値","種族基本値");
                    if(updated==resident.text) continue;
                    if(!changed) Undo.RecordObject(town,"Update display terminology");
                    resident.text=updated; changed=true;
                }
                if(changed) { EditorUtility.SetDirty(town); EditorSceneManager.MarkSceneDirty(town.gameObject.scene); }
            }
        }
    }
}
