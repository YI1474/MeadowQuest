using System;

namespace MeadowQuest
{
    public static class AdventureProgress
    {
        public const string FinalTrainer = "town_master";
        public static int Wins(Func<string, bool> defeated) => (defeated("town_sora") ? 1 : 0) + (defeated("town_nagi") ? 1 : 0) + (defeated("town_ren") ? 1 : 0);
        public static bool CanChallenge(string id, Func<string, bool> defeated) => id != FinalTrainer || defeated("town_ren");
        public static string Goal(Func<string, bool> defeated) => defeated(FinalTrainer) ? "ゲームクリア！ 引き続き探索を楽しもう。" : defeated("town_ren") ? "試練場の師範アサギに挑戦しよう。" : "西の草原と洞窟を抜け、次の街の試練場を目指そう。";
    }
}
