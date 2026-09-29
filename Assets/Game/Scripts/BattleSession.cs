using System;
namespace MeadowQuest
{
    // Pure C#: no Unity scene or UI dependencies. Definitions are never mutated.
    public sealed class BattleSession
    {
        public enum Phase { PlayerTurn, EnemyTurn, Won, Lost, Escaped, Captured }
        public bool Capture() { if(State!=Phase.PlayerTurn) return false; State=Phase.Captured; return true; }
        public bool SpendItemTurn(int activeHp)
        {
            if(State!=Phase.PlayerTurn || playerActed || enemyActed || activeHp<1 || activeHp>PlayerMaxHp) return false;
            PlayerHp=activeHp; Next(true); return true;
        }
        public bool CanUseItem => State==Phase.PlayerTurn && !playerActed && !enemyActed;
        public Phase State { get; private set; }=Phase.PlayerTurn;
        public int PlayerHp { get; private set; }
        public int EnemyHp { get; private set; }
        public int PlayerMaxHp { get; private set; }
        public bool SwitchPlayer(int hp,int maxHp,bool forced)
        {
            if(hp<1 || hp>maxHp || (forced?State!=Phase.Lost:State!=Phase.PlayerTurn)) return false;
            PlayerHp=hp; PlayerMaxHp=maxHp; playerActed=!forced; enemyActed=false;
            State=forced?Phase.PlayerTurn:Phase.EnemyTurn; return true;
        }
        public int EnemyMaxHp { get; }
        bool playerActed,enemyActed;
        public BattleSession(int playerHp,int enemyHp)
        {
            if(playerHp<1 || enemyHp<1) throw new ArgumentOutOfRangeException();
            PlayerHp=PlayerMaxHp=playerHp; EnemyHp=EnemyMaxHp=enemyHp;
        }
        public BattleSession(int currentHp,int maximumHp,int enemyHp):this(maximumHp,enemyHp)
        {
            if(currentHp<1 || currentHp>maximumHp) throw new ArgumentOutOfRangeException();
            PlayerHp=currentHp;
        }
        public void StartRound(bool playerFirst)
        {
            if(State!=Phase.PlayerTurn || playerActed || enemyActed) throw new InvalidOperationException();
            State=playerFirst?Phase.PlayerTurn:Phase.EnemyTurn;
        }
        void Next(bool playerMoved)
        {
            if(playerMoved) playerActed=true; else enemyActed=true;
            if(playerActed && enemyActed) { playerActed=enemyActed=false; State=Phase.PlayerTurn; }
            else State=playerMoved?Phase.EnemyTurn:Phase.PlayerTurn;
        }
        public bool Attack(int damage)
        {
            if(State!=Phase.PlayerTurn) return false;
            if(damage<0) throw new ArgumentOutOfRangeException(nameof(damage));
            EnemyHp=Math.Max(0,EnemyHp-damage);
            if(EnemyHp==0) State=Phase.Won; else Next(true);
            return true;
        }
        public bool Counterattack(int damage)
        {
            if(State!=Phase.EnemyTurn) return false;
            if(damage<0) throw new ArgumentOutOfRangeException(nameof(damage));
            PlayerHp=Math.Max(0,PlayerHp-damage);
            if(PlayerHp==0) State=Phase.Lost; else Next(false);
            return true;
        }
        public bool Escape()
        {
            if(State!=Phase.PlayerTurn) return false;
            State=Phase.Escaped; return true;
        }
    }
}
