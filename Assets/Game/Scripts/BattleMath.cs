using System;

namespace MeadowQuest
{
    // Game-specific rules, intentionally not an exact reproduction of any generation.
    public static class BattleMath
    {
        public static int Stat(int basis, int level, bool hp)
        {
            if (basis < 1 || basis > 255 || level < 1 || level > 100)
                throw new ArgumentOutOfRangeException();
            // IV=15, EV=0, nature=1.0 for the initial game.
            return (2 * basis + 15) * level / 100 + (hp ? level + 10 : 5);
        }

        public static int Damage(int level, int power, int attack, int defense, decimal effectiveness, bool stab, bool critical, int roll)
        {
            if (level < 1 || level > 100 || power < 1 || power > 9999 || attack < 1 || defense < 1 || effectiveness < 0 || roll < 85 || roll > 100)
                throw new ArgumentOutOfRangeException();
            if (effectiveness == 0)
                return 0;
            long basic = ((2L * level / 5 + 2) * power * attack / defense) / 50 + 2;
            decimal result = basic;
            result = decimal.Floor(result * (critical ? 1.5m : 1m));
            result = decimal.Floor(result * roll / 100m);
            result = decimal.Floor(result * (stab ? 1.5m : 1m));
            result = decimal.Floor(result * effectiveness);
            return Math.Max(1, checked((int)result));
        }

        public static decimal Effectiveness(string move, string target)
        {
            if (move == "Fire" && target == "Grass" || move == "Grass" && target == "Water" || move == "Water" && target == "Fire")
                return 2m;
            if (move != "Normal" && move == target || move == "Fire" && target == "Water" || move == "Grass" && target == "Fire" || move == "Water" && target == "Grass")
                return .5m;
            return 1m;
        }
    }
}
