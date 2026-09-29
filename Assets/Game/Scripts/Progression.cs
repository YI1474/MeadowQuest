using System;
namespace MeadowQuest
{
    // Order is shared by CSV, saves and UI: HP, Atk, Def, SpA, SpD, Spe.
    public static class Progression
    {
        public static readonly string[] StatNames={"HP","攻撃","防御","特攻","特防","素早さ"};
        public const int NatureCount=5;
        public static readonly string[] NatureNames={"情熱家","忍耐家","探究家","平常心","行動派"};
        public static string NatureDescription(int nature) => NatureNames[nature]+"（"+StatNames[nature+1]+"＋10％）";
        // Preserve the former boosted stat. Former neutral natures gain their row's bonus.
        public static int MigrateLegacyNature(int nature)
        {
            if(nature<0 || nature>24) throw new ArgumentOutOfRangeException(nameof(nature));
            int row=nature/5;
            return row==2?4:row==3?2:row==4?3:row;
        }
        public static int Stat(int basis,int level,int iv,int ev,int nature,int stat)
        {
            if(basis<1 || basis>255 || level<1 || level>100 || iv<0 || iv>31 || ev<0 || ev>252 || nature<0 || nature>=NatureCount || stat<0 || stat>5)
                throw new ArgumentOutOfRangeException();
            int common=(2*basis+iv+ev/4)*level/100;
            if(stat==0) return common+level+10;
            int percent=stat==nature+1?110:100;
            return (common+5)*percent/100;
        }
        // Medium Fast growth for all current species. Level 1 starts at zero.
        public static int ExperienceAt(int level)
        { if(level<1 || level>100) throw new ArgumentOutOfRangeException(); return level==1?0:level*level*level; }
        public static int LevelAt(int xp)
        { if(xp<0 || xp>1000000) throw new ArgumentOutOfRangeException(); int level=1; while(level<100 && xp>=ExperienceAt(level+1)) level++; return level; }
        public static int Reward(int basis,int enemyLevel,int ownLevel)
        {
            if(basis<1 || basis>9999 || enemyLevel<1 || enemyLevel>100 || ownLevel<1 || ownLevel>100) throw new ArgumentOutOfRangeException();
            return Math.Max(1,(int)Math.Floor(basis*enemyLevel/5.0*Math.Pow((2.0*enemyLevel+10)/(enemyLevel+ownLevel+10),2.5))+1);
        }
        public static int AddEffort(int[] ev,int stat,int amount)
        {
            if(ev==null || ev.Length!=6 || stat<0 || stat>5 || amount<0) throw new ArgumentOutOfRangeException();
            int total=0; foreach(int n in ev) { if(n<0 || n>252) throw new ArgumentOutOfRangeException(); total+=n; }
            if(total>510) throw new ArgumentOutOfRangeException();
            int gain=Math.Min(amount,Math.Min(252-ev[stat],510-total)); ev[stat]+=gain; return gain;
        }
    }
    [Serializable]
    public sealed class IndividualData
    {
        public string instanceId,speciesId;
        public int experience,nature,currentHp;
        public int[] iv,ev;
        public string[] moveIds;
        public int[] pp;
    }
}
