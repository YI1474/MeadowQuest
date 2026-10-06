using System;
using System.Collections.Generic;
using System.Linq;

namespace MeadowQuest
{
    public static class PartyExperience
    {
        public sealed class LevelUp
        {
            public MonsterIndividual Monster { get; }
            public int PreviousLevel { get; }
            public int[] PreviousStats { get; }

            public LevelUp(MonsterIndividual monster)
            {
                Monster = monster;
                PreviousLevel = monster.Level;
                PreviousStats = Enumerable.Range(0, 6).Select(monster.Stat).ToArray();
            }
        }

        public static List<LevelUp> Award(MonsterParty party, int activeIndex, int experience, int[] effortYield)
        {
            if (party == null || activeIndex < 0 || activeIndex >= party.Members.Count || experience < 0 || effortYield == null || effortYield.Length != 6 || effortYield.Any(n => n < 0 || n > 3))
                throw new ArgumentException("経験値報酬が不正です。");
            var leveled = new List<LevelUp>();
            for (int i = 0; i < party.Members.Count; i++)
            {
                var member = party.Members[i];
                var before = new LevelUp(member);
                member.Grow(experience, effortYield);
                if (member.Level > before.PreviousLevel)
                    leveled.Add(before);
            }

            return leveled;
        }
    }
}
