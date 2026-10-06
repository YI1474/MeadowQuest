using System;
using System.Linq;

namespace MeadowQuest
{
    public sealed class TrainerLineup
    {
        readonly MonsterIndividual[] members;
        public int Index { get; private set; }
        public int Count => members.Length;
        public MonsterIndividual Current => members[Index];
        public bool HasNext => Index + 1 < Count;

        public TrainerLineup(MonsterIndividual[] monsters)
        {
            if (monsters == null || monsters.Length < 1 || monsters.Length > 6 || monsters.Any(m => m == null || m.Data.currentHp <= 0) || monsters.Select(m => m.Data.instanceId).Distinct().Count() != monsters.Length)
                throw new ArgumentException("対戦相手の手持ちが不正です。");
            members = monsters.ToArray();
        }

        public bool Advance()
        {
            if (Current.Data.currentHp > 0 || !HasNext)
                return false;
            Index++;
            return true;
        }
    }
}
