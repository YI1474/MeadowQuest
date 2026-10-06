using System;

namespace MeadowQuest
{
    public static class CaptureRules
    {
        public enum Result
        {
            Blocked,
            Failed,
            Caught
        }

        public static double Chance(int hp, int maxHp, int multiplier)
        {
            if (maxHp < 1 || hp < 1 || hp > maxHp || multiplier < 1)
                throw new ArgumentOutOfRangeException();
            return Math.Min(.95, (.2 + .6 * (1 - (double)hp / maxHp)) * multiplier / 100.0);
        }

        public static Result Attempt(MonsterParty party, MonsterIndividual foe, Inventory bag, ItemDefinition item, bool trainer, double roll)
        {
            if (roll < 0 || roll >= 1 || double.IsNaN(roll))
                throw new ArgumentOutOfRangeException();
            if (trainer || party.Members.Count >= MonsterParty.Capacity || foe.Data.currentHp <= 0 || item == null || item.effect != ItemEffect.Capture || !item.useInBattle || item.value < 1)
                return Result.Blocked;
            foreach (var member in party.Members)
                if (member.Data.instanceId == foe.Data.instanceId)
                    return Result.Blocked;
            if (!bag.Consume(item.id))
                return Result.Blocked;
            if (roll >= Chance(foe.Data.currentHp, foe.Hp, item.value))
                return Result.Failed;
            if (!party.TryAdd(foe))
                throw new InvalidOperationException("捕獲後の追加に失敗しました。");
            return Result.Caught;
        }
    }
}
