using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad]
    public static class FieldMenuSaveChecks
    {
        static FieldMenuSaveChecks() { EditorApplication.delayCall+=Run; }
        [MenuItem("Meadow Quest/Validate/Field Menu Save")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var definition=AssetDatabase.LoadAssetAtPath<MonsterDefinition>("Assets/Game/Data/Monsters/Starter.asset");
            if(!definition || definition.moves.Length==0) return;
            try
            {
                var original=MonsterIndividual.Create(definition,new System.Random(42));
                var legacy=new ProgressSaveData{version=1,companion=original.Data};
                var restored=ProgressSave.Decode(definition,JsonUtility.ToJson(legacy),out var migrated);
                Check(migrated.potions==3 && !migrated.hasPosition,"Legacy inventory migration");
                Check(restored.Data.instanceId==original.Data.instanceId,"Legacy identity preserved");
                var current=new ProgressSaveData{version=8,companion=original.Data,party=new[]{original.Data},money=420,captureGiftReceived=true,recoveryRoom="clinic",potions=1,inventory=new[]{new ItemStack("potion",1),new ItemStack("bond_thread",5)},defeatedTrainers=new[]{"town_sora", "town_nagi", "town_ren", "town_master"},hasPosition=true,cellX=26,cellY=82};
                restored=ProgressSave.Decode(definition,JsonUtility.ToJson(current),out var saved);
                Check(saved.potions==1 && saved.hasPosition && saved.cellX==26 && saved.cellY==82,"Inventory and interior position roundtrip");
                Check(JsonUtility.ToJson(restored.Data)==JsonUtility.ToJson(original.Data),"Individual roundtrip");
                Check(saved.inventory.Length==2 && saved.inventory[1].id=="bond_thread" && saved.inventory[1].count==5,"Multiple item IDs preserved");
                Check(saved.defeatedTrainers.Length==4 && saved.defeatedTrainers[0]=="town_sora" && saved.money==420 && saved.captureGiftReceived && saved.recoveryRoom=="clinic" && saved.party.Length==1,"Economy, recovery, party and clear state preserved");
                for(int oldNature=0;oldNature<25;oldNature++)
                {
                    var oldData=JsonUtility.FromJson<IndividualData>(JsonUtility.ToJson(original.Data));
                    oldData.nature=oldNature;
                    foreach(int version in new[]{1,2})
                    {
                        var oldSave=new ProgressSaveData{version=version,companion=oldData,potions=2};
                        var updated=ProgressSave.Decode(definition,JsonUtility.ToJson(oldSave),out var updatedSave);
                        Check(updated.Data.nature==Progression.MigrateLegacyNature(oldNature),"Legacy nature migration");
                        var again=ProgressSave.Decode(definition,JsonUtility.ToJson(updatedSave),out _);
                        Check(again.Data.nature==updated.Data.nature,"Nature migration runs once");
                    }
                }
                current.potions=-1; bool rejected=false;
                try { ProgressSave.Decode(definition,JsonUtility.ToJson(current),out _); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Invalid count rejected");
                current.potions=1; current.version=999; rejected=false;
                try { ProgressSave.Decode(definition,JsonUtility.ToJson(current),out _); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Future version rejected");
                File.WriteAllText("FieldMenuSaveChecks.txt",DateTime.Now.ToString("s")+"\nPASS: 108 checks using Unity JsonUtility and production save decoding.\nUser save file was not modified.\nUI interaction and disk write failures require separate verification.");
                Debug.Log("メニュー用セーブ形式の検証108項目が通りました。実セーブは変更していません。");
            }
            catch(Exception e) { Debug.LogError("メニュー用セーブ検証失敗: "+e); }
        }
        static void Check(bool value,string name) { if(!value) throw new InvalidOperationException(name); }
    }
}
