namespace MeadowQuest
{
    public static class BattleItemRules
    {
        public static bool Use(BattleSession session,MonsterParty party,int active,int target,Inventory bag,ItemDefinition item)
        {
            if(!session.CanUseItem || active<0 || active>=party.Members.Count || target<0 || target>=party.Members.Count) return false;
            var fighter=party.Members[active];
            if(fighter.Data.currentHp<=0 || fighter.Hp!=session.PlayerMaxHp || fighter.Data.currentHp!=session.PlayerHp) return false;
            if(!bag.Use(item,party.Members[target],true)) return false;
            session.SpendItemTurn(fighter.Data.currentHp); return true;
        }
    }
}
