using System;
using MeadowQuest;
static class BattleChecks {
static int checks;
static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;}
static void Main(){
var battle=new BattleSession(30,24);
Check(battle.State==BattleSession.Phase.PlayerTurn,"player starts");
Check(!battle.Counterattack(5),"no early counterattack");
Check(battle.Attack(8)&&battle.EnemyHp==16,"attack damage");
Check(!battle.Attack(8)&&battle.EnemyHp==16,"double click rejected");
Check(!battle.Escape(),"cannot escape during enemy turn");
Check(battle.Counterattack(5)&&battle.PlayerHp==25,"counterattack damage");
Check(battle.Attack(100)&&battle.EnemyHp==0&&battle.State==BattleSession.Phase.Won,"victory clamps HP");
Check(!battle.Counterattack(999)&&battle.PlayerHp==25,"defeated enemy cannot retaliate");
var loss=new BattleSession(1,30); loss.Attack(1); loss.Counterattack(5);
Check(loss.PlayerHp==0&&loss.State==BattleSession.Phase.Lost,"defeat");
Check(!loss.Attack(100)&&!loss.Escape(),"terminal state cannot act");
var escape=new BattleSession(30,24); Check(escape.Escape()&&escape.State==BattleSession.Phase.Escaped,"escape");
Check(!escape.Attack(9)&&!escape.Counterattack(9),"escaped fight cannot continue");
var fresh=new BattleSession(30,24); Check(fresh.PlayerHp==30&&fresh.EnemyHp==24,"new encounter recovers HP");
bool invalid=false;try{new BattleSession(0,10);}catch(ArgumentOutOfRangeException){invalid=true;}
Check(invalid,"invalid HP rejected");
Console.WriteLine(checks+" battle checks passed");
}}
