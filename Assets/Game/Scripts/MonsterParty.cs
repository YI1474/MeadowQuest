using System;
using System.Collections.Generic;
using System.Linq;

namespace MeadowQuest
{
    public sealed class MonsterParty
    {
        public const int Capacity = 6;
        readonly List<MonsterIndividual> members;
        public IReadOnlyList<MonsterIndividual> Members { get; }
        public MonsterIndividual Lead => members[0];

        public MonsterParty(IEnumerable<MonsterIndividual> source)
        {
            members = source.ToList();
            if (members.Count < 1 || members.Count > Capacity || members.Any(m => m == null) || members.Select(m => m.Data.instanceId).Distinct().Count() != members.Count)
                throw new ArgumentException("手持ちの数または個体IDが不正です。");
            Members = members.AsReadOnly();
        }

        public bool TryAdd(MonsterIndividual monster)
        {
            if (monster == null || members.Count >= Capacity || members.Any(m => m.Data.instanceId == monster.Data.instanceId))
                return false;
            members.Add(monster);
            return true;
        }

        public bool Swap(int first, int second)
        {
            if (first < 0 || second < 0 || first >= members.Count || second >= members.Count || first == second)
                return false;
            var temp = members[first];
            members[first] = members[second];
            members[second] = temp;
            return true;
        }
    }
}
