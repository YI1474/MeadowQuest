using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace MeadowQuest.Editor
{
    public static class ItemMasterImporter
    {
        public const string Folder="Assets/Game/Resources/Items/";
        public static List<string[]> Validate()
        {
            var rows=MasterCsv.Parse(File.ReadAllText("Assets/Game/Data/Source/items.csv"));
            const string header="id,name,description,effect,value,buyPrice,sellPrice,useInField,useInBattle,initialCount";
            if(rows.Count<2 || string.Join(",",rows[0])!=header) throw new FormatException("items.csv: ヘッダーが違います。");
            rows.RemoveAt(0); var ids=new HashSet<string>();
            foreach(var r in rows)
            {
                if(r.Length!=10 || r.Any(string.IsNullOrWhiteSpace) || !Regex.IsMatch(r[0],"^[a-z0-9_]+$") || !ids.Add(r[0])) throw new FormatException("items.csv: 空欄・列数・IDを確認してください。");
                if(!Enum.TryParse(r[3],out ItemEffect effect) || !Enum.IsDefined(typeof(ItemEffect),effect)) throw new FormatException(r[0]+": 不明な効果");
                for(int i=4;i<=6;i++) if(!int.TryParse(r[i],out int n) || n<0 || n>99999) throw new FormatException(r[0]+": 数値が不正");
                if(int.Parse(r[6])>int.Parse(r[5])) throw new FormatException(r[0]+": 売値は買値以下にしてください。");
                if(!bool.TryParse(r[7],out _) || !bool.TryParse(r[8],out _) || !int.TryParse(r[9],out int count) || count<0 || count>999) throw new FormatException(r[0]+": 使用場所・所持数が不正");
                int value=int.Parse(r[4]);
                if(value<1 || (effect==ItemEffect.Revive && value>100)) throw new FormatException(r[0]+": 効果量が範囲外");
                var asset=AssetDatabase.LoadMainAssetAtPath(Folder+r[0]+".asset");
                if(asset && (!(asset is ItemDefinition item) || item.id!=r[0])) throw new FormatException(r[0]+": 出力先の型・IDが不一致");
            }
            if(!ids.Contains("potion")) throw new FormatException("旧セーブの互換性のためpotion IDを残してください。");
            return rows;
        }
        public static void Apply(List<string[]> rows)
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            foreach(var r in rows)
            {
                string path=Folder+r[0]+".asset";
                var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if(!item) { item=ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(item,path); Undo.RegisterCreatedObjectUndo(item,"Import Items"); }
                else Undo.RecordObject(item,"Import Items");
                item.id=r[0]; item.displayName=r[1]; item.description=r[2]; item.effect=(ItemEffect)Enum.Parse(typeof(ItemEffect),r[3]);
                item.value=int.Parse(r[4]); item.buyPrice=int.Parse(r[5]); item.sellPrice=int.Parse(r[6]); item.useInField=bool.Parse(r[7]); item.useInBattle=bool.Parse(r[8]); item.initialCount=int.Parse(r[9]);
                EditorUtility.SetDirty(item);
            }
        }
    }
}
