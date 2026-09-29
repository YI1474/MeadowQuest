using System;
using System.Linq;
namespace MeadowQuest
{
    public sealed class MonsterIndividual
    {
        public MonsterDefinition Definition { get; }
        public IndividualData Data { get; }
        public MoveDefinition[] Moves { get; }
        public int Level => Progression.LevelAt(Data.experience);
        public int Hp => Stat(0);
        public bool CanUseAnyMove => Data.pp.Any(p=>p>0);
        public int Stat(int index)
        {
            int basis;
            switch(index)
            {
                case 0: basis=Definition.baseHp; break;
                case 1: basis=Definition.baseAttack; break;
                case 2: basis=Definition.baseDefense; break;
                case 3: basis=Definition.baseSpecialAttack; break;
                case 4: basis=Definition.baseSpecialDefense; break;
                case 5: basis=Definition.baseSpeed; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
            return Progression.Stat(basis,Level,Data.iv[index],Data.ev[index],Data.nature,index);
        }
        public static MonsterIndividual Create(MonsterDefinition definition,Random random)
        {
            if(definition==null || string.IsNullOrEmpty(definition.id) || definition.moves==null || definition.moves.Length<1 || definition.moves.Any(m=>!m || string.IsNullOrEmpty(m.id)))
                throw new InvalidOperationException("モンスターマスターが未設定です。Data > Import CSVを実行してください。");
            var data=new IndividualData { instanceId=Guid.NewGuid().ToString("N"),speciesId=definition.id,
                experience=Progression.ExperienceAt(definition.level),nature=random.Next(Progression.NatureCount),iv=new int[6],ev=new int[6],
                moveIds=definition.moves.Select(m=>m.id).ToArray(),pp=definition.moves.Select(m=>m.maxPp).ToArray() };
            for(int i=0;i<6;i++) data.iv[i]=random.Next(32);
            var individual=new MonsterIndividual(definition,data); individual.Heal(); return individual;
        }
        public MonsterIndividual(MonsterDefinition definition,IndividualData data)
        {
            Definition=definition; Data=data;
            if(definition==null || data==null || data.speciesId!=definition.id || !Guid.TryParse(data.instanceId,out _) || data.nature<0 || data.nature>=Progression.NatureCount || data.experience<0 || data.experience>1000000)
                throw new InvalidOperationException("個体データのID・経験値・性格が不正です。");
            if(data.iv==null || data.iv.Length!=6 || data.iv.Any(n=>n<0 || n>31) || data.ev==null || data.ev.Length!=6 || data.ev.Any(n=>n<0 || n>252) || data.ev.Sum()>510)
                throw new InvalidOperationException("素質または鍛錬値が不正です。");
            if(data.moveIds==null || data.moveIds.Length<1 || data.moveIds.Length>4 || data.moveIds.Distinct().Count()!=data.moveIds.Length || data.pp==null || data.pp.Length!=data.moveIds.Length)
                throw new InvalidOperationException("技データが不正です。");
            Moves=data.moveIds.Select(id=>definition.moves.FirstOrDefault(m=>m && m.id==id)).ToArray();
            for(int i=0;i<Moves.Length;i++) if(!Moves[i] || data.pp[i]<0 || data.pp[i]>Moves[i].maxPp) throw new InvalidOperationException("保存された技IDまたはPPがマスターと一致しません。");
            if(data.currentHp<0 || data.currentHp>Hp) throw new InvalidOperationException("保存されたHPが範囲外です。");
        }
        public void Heal()
        { Data.currentHp=Hp; for(int i=0;i<Moves.Length;i++) Data.pp[i]=Moves[i].maxPp; }
        public int Grow(int experience,int[] yield)
        {
            if(experience<0 || yield==null || yield.Length!=6 || yield.Any(n=>n<0 || n>3)) throw new ArgumentOutOfRangeException();
            int before=Level,oldHp=Hp;
            for(int i=0;i<6;i++) Progression.AddEffort(Data.ev,i,yield[i]);
            Data.experience=(int)Math.Min(1000000L,(long)Data.experience+experience);
            if(Data.currentHp>0) Data.currentHp=Math.Min(Hp,Data.currentHp+Hp-oldHp);
            return Level-before;
        }
    }
}
