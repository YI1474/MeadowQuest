namespace MeadowQuest
{
    public static class ShopRules
    {
        public const int StartingMoney = 1000, MaxMoney = 9999999;
        public static bool Stocked(ItemDefinition item) => item != null && (item.FieldImplemented || item.effect == ItemEffect.Capture);
        public static string Trade(Inventory inventory, ref int money, ItemDefinition item, int quantity, bool buying)
        {
            if (item == null || quantity < 1 || quantity > 999)
                return "個数が不正です。";
            int price = buying ? item.buyPrice : item.sellPrice;
            if (price <= 0 || (buying && !Stocked(item)))
                return "この道具は取引できません。";
            long total = (long)price * quantity;
            if (buying)
            {
                if (quantity > 999 - inventory.Count(item.id))
                    return "所持上限は999個です。";
                if (total > money)
                    return "お金が足りません。";
                inventory.Add(item.id, quantity);
                money -= (int)total;
            }
            else
            {
                if (quantity > inventory.Count(item.id))
                    return "道具が足りません。";
                if (total > MaxMoney - money)
                    return "所持金の上限を超えます。";
                for (int i = 0; i < quantity; i++)
                    inventory.Consume(item.id);
                money += (int)total;
            }

            return null;
        }
    }
}
