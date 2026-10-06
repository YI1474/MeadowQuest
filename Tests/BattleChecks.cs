using System;
using MeadowQuest;

static class BattleChecks
{
    static int checks;

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++;
    }

    static void Main()
    {
        var battle = new BattleSession(30, 24);
        Check(battle.State == BattleSession.Phase.PlayerTurn, "player starts");
        Check(!battle.Counterattack(5), "no early counterattack");
        Check(battle.Attack(8) && battle.EnemyHp == 16, "attack damage");
        Check(!battle.Attack(8) && battle.EnemyHp == 16, "double click rejected");
        Check(!battle.Escape(), "cannot escape during enemy turn");
        Check(battle.Counterattack(5) && battle.PlayerHp == 25, "counterattack damage");
        Check(battle.Attack(100) && battle.EnemyHp == 0 && battle.State == BattleSession.Phase.Won, "victory clamps HP");
        Check(!battle.Counterattack(999) && battle.PlayerHp == 25, "defeated enemy cannot retaliate");

        var loss = new BattleSession(1, 30);
        loss.Attack(1);
        loss.Counterattack(5);
        Check(loss.PlayerHp == 0 && loss.State == BattleSession.Phase.Lost, "defeat");
        Check(!loss.Attack(100) && !loss.Escape(), "terminal state cannot act");
        Check(loss.SwitchPlayer(10, 20, true) && loss.State == BattleSession.Phase.PlayerTurn, "forced switch resumes battle");

        var escape = new BattleSession(30, 24);
        Check(escape.Escape() && escape.State == BattleSession.Phase.Escaped, "escape");
        Check(!escape.Attack(9) && !escape.Counterattack(9), "escaped fight cannot continue");
        var fresh = new BattleSession(30, 24);
        Check(fresh.PlayerHp == 30 && fresh.EnemyHp == 24, "constructor accepts initial HP");
        bool invalid = false;
        try { new BattleSession(0, 10); }
        catch (ArgumentOutOfRangeException) { invalid = true; }
        Check(invalid, "invalid HP rejected");

        var fasterFoe = new BattleSession(20, 30, 40);
        fasterFoe.StartRound(false);
        Check(!fasterFoe.Attack(5), "slower player cannot act first");
        Check(fasterFoe.Counterattack(3) && fasterFoe.PlayerHp == 17, "enemy first retains current HP");
        Check(fasterFoe.Attack(5) && fasterFoe.State == BattleSession.Phase.PlayerTurn, "round completes after both actions");
        Check(fasterFoe.SwitchPlayer(15, 20, false) && fasterFoe.State == BattleSession.Phase.EnemyTurn, "voluntary switch spends turn");
        var captured = new BattleSession(20, 20);
        Check(captured.Capture() && !captured.Attack(1) && !captured.Counterattack(1), "capture ends battle");
        var healing = new BattleSession(5, 20, 20);
        Check(!healing.SpendItemTurn(21), "healing cannot exceed max HP");
        Check(healing.SpendItemTurn(15) && healing.PlayerHp == 15 && !healing.CanUseItem, "item consumes player action");

        Check(BattleMath.Damage(10, 35, 20, 20, 1m, false, false, 100) == 6, "neutral damage");
        Check(BattleMath.Damage(10, 35, 20, 20, 2m, true, false, 100) == 18, "type match and weakness");
        Check(BattleMath.Damage(10, 35, 20, 20, 0m, true, true, 100) == 0, "immunity remains zero");
        Check(BattleMath.Effectiveness("Normal", "Grass") == 1m, "normal is neutral");
        Check(BattleMath.Effectiveness("Fire", "Grass") == 2m, "fire beats grass");
        Check(Progression.LevelAt(999) == 9 && Progression.LevelAt(1000) == 10, "level boundary");
        Check(Progression.LevelAt(1000000) == 100, "level cap");
        Check(Progression.Stat(50, 50, 0, 0, 0, 1) == 60, "nature boosts selected stat");
        Check(Progression.Stat(50, 50, 0, 0, 0, 2) == 55, "nature does not lower other stats");
        var effort = new[] { 250, 252, 0, 0, 0, 0 };
        Check(Progression.AddEffort(effort, 0, 10) == 2 && effort[0] == 252, "per-stat effort cap");
        Check(Progression.AddEffort(effort, 2, 10) == 6, "total effort cap");
        Check(Progression.AddEffort(effort, 3, 10) == 0, "no gain beyond total cap");
        Console.WriteLine(checks + " checks passed");
    }
}
