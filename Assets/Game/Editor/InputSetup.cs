using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class InputSetup
    {
        const string ActionPath="Assets/Game/Resources/Input/PlayerControls.inputactions";
        static InputSetup() { EditorApplication.delayCall+=Repair; }

        [MenuItem("Meadow Quest/Repair/Input Setup")]
        public static void Repair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionPath);
            if(!asset)
            {
                AssetDatabase.ImportAsset(ActionPath,ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionPath);
            }
            if(!asset) { Debug.LogError("Input repair failed: PlayerControls is not imported as InputActionAsset."); return; }
            var action=asset.FindAction("Gameplay/Move",true);
            if(action.bindings.Count!=10) Debug.LogWarning("Move bindings have changed: " + action.bindings.Count);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player.prefab");
            if(!prefab) return;
            var serialized=new SerializedObject(prefab.GetComponent<KeyboardMoveInput>());
            var property=serialized.FindProperty("controls");
            if(property.objectReferenceValue!=asset)
            {
                property.objectReferenceValue=asset;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SavePrefabAsset(prefab);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("Input setup verified: PlayerControls / Gameplay / Move ("+action.bindings.Count+" bindings).");
        }
    }

    [CustomEditor(typeof(KeyboardMoveInput))]
    public sealed class KeyboardMoveInputInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if(!Application.isPlaying) return;
            var input=(KeyboardMoveInput)target;
            EditorGUILayout.LabelField("Move direction",input.Direction.ToString());
            EditorGUILayout.LabelField("Action enabled",input.ActionEnabled.ToString());
            EditorGUILayout.LabelField("Window focused",Application.isFocused.ToString());
            EditorGUILayout.LabelField("Keyboard detected",(Keyboard.current!=null).ToString());
            Repaint();
        }
    }
}
