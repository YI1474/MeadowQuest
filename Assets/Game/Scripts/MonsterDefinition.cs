using UnityEngine;
namespace MeadowQuest
{
    [CreateAssetMenu(menuName="Meadow Quest/Monster")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        public string id;
        [HideInInspector] public int growthDataVersion;
        [Min(1)] public int baseExperience=64;
        public int[] effortYield=new int[]{0,0,0,0,0,1};
        public string element="Normal";
        public MoveDefinition[] moves = new MoveDefinition[0];
        [Range(1,100)] public int level=10;
        [Range(1,255)] public int baseHp=60,baseAttack=50,baseDefense=50,baseSpecialAttack=50,baseSpecialDefense=50,baseSpeed=50;
        public int Hp => BattleMath.Stat(baseHp,level,true);
        public int PhysicalAttack => BattleMath.Stat(baseAttack,level,false);
        public int Defense => BattleMath.Stat(baseDefense,level,false);
        public int SpecialAttack => BattleMath.Stat(baseSpecialAttack,level,false);
        public int SpecialDefense => BattleMath.Stat(baseSpecialDefense,level,false);
        public int Speed => BattleMath.Stat(baseSpeed,level,false);
        public string displayName;
        public Sprite portrait;
        public Sprite backPortrait;
        // Legacy scene setup fields, retained for serialized compatibility only.
        [HideInInspector] public int maxHp=30;
        [HideInInspector] public int attack=7;
    }
}
