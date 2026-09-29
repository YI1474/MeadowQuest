using System;
using UnityEditor;
using UnityEngine;
namespace MeadowQuest.Editor
{
    public static class GrowthSaveChecks
    {
        [MenuItem("Meadow Quest/Validate/Growth Save Roundtrip")]
        public static void Run()
        {
            var definition=AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/Starter.asset");
            var original=MonsterIndividual.Create(definition,new System.Random(42));
            original.Data.currentHp=1; original.Data.pp[0]=0;
            original.Grow(700,new[]{0,0,0,0,0,1});
            string json=JsonUtility.ToJson(new ProgressSaveData{version=1,companion=original.Data});
            var decoded=JsonUtility.FromJson<ProgressSaveData>(json);
            var restored=new MonsterIndividual(definition,decoded.companion);
            if(decoded.version!=1 || JsonUtility.ToJson(original.Data)!=JsonUtility.ToJson(restored.Data) || restored.Hp!=original.Hp)
                throw new InvalidOperationException("個体データのJSON往復に失敗しました。");
            Debug.Log("育成セーブのJSON往復OK（実際のセーブファイルは変更していません）。");
        }
    }
}
