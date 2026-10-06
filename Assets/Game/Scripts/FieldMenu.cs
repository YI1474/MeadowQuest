using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MeadowQuest
{
    [DefaultExecutionOrder(70)]
    public sealed class FieldMenu : MonoBehaviour
    {
        public GridMover player;
        public BattleController battle;
        public GameObject panel;
        public Text title, body;
        public Button open, party, items, save, help, back, close, usePotion;
        public Button previousItem, nextItem;
        public GameObject partyList, partyActions;
        public Button monsterCard, viewStats, reorderParty, partyReturn;
        public Button[] partyCards = new Button[0];
        int selectedMember, reorderFrom = -1;
        public Image monsterIcon, partyHp;
        public Text monsterSummary, partyHint;
        public ScrollRect itemScroll;
        public Button itemRowTemplate;
        public Text itemListHint;
        readonly System.Collections.Generic.List<Button> itemRows = new System.Collections.Generic.List<Button>();
        ItemDefinition selectedItem;
        float itemScrollPosition = 1;
        enum Page
        {
            Home,
            Party,
            PartyActions,
            PartyStats,
            Items,
            ItemDetails,
            Save,
            Help
        }

        Page page;
        bool isOpen;
        void Start()
        {
            panel.SetActive(false);
            open.onClick.AddListener(Open);
            party.onClick.AddListener(() => Show(Page.Party));
            items.onClick.AddListener(() => Show(Page.Items));
            help.onClick.AddListener(() => Show(Page.Help));
            save.onClick.AddListener(() => Show(Page.Save, battle.SaveFromMenu()));
            back.onClick.AddListener(Back);
            close.onClick.AddListener(Close);
            usePotion.onClick.AddListener(UseSelectedItem);
            for (int i = 0; i < partyCards.Length; i++)
            {
                int index = i;
                partyCards[i].onClick.AddListener(() => SelectMember(index));
            }

            if (reorderParty)
                reorderParty.onClick.AddListener(() =>
                {
                    reorderFrom = selectedMember;
                    Show(Page.Party);
                });
            if (viewStats)
                viewStats.onClick.AddListener(() => Show(Page.PartyStats));
            if (partyReturn)
                partyReturn.onClick.AddListener(() => Show(Page.Party));
        }

        void Update()
        {
            bool available = battle.ProgressionReady && !battle.IsActive;
            open.gameObject.SetActive(available && !isOpen);
            open.interactable = available && !player.MovementLocked && !player.IsMoving;
            if (!Application.isFocused || !available || ShopMenu.ClosedFrame == Time.frameCount)
                return;
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame)
            {
                if (isOpen)
                    Back();
                else
                    Open();
            }
        }

        void Open()
        {
            if (isOpen || !battle.ProgressionReady || battle.IsActive || player.MovementLocked || player.IsMoving)
                return;
            isOpen = true;
            player.MovementLocked = true;
            panel.SetActive(true);
            open.gameObject.SetActive(false);
            Show(Page.Home);
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(party.gameObject);
        }

        void Back()
        {
            if (page == Page.Party && reorderFrom >= 0)
            {
                reorderFrom = -1;
                Show(Page.Party);
            }
            else if (page == Page.Home)
                Close();
            else if (page == Page.ItemDetails)
                Show(Page.Items);
            else if (page == Page.PartyStats)
                Show(Page.PartyActions);
            else if (page == Page.PartyActions)
                Show(Page.Party);
            else
                Show(Page.Home);
        }

        void SelectMember(int index)
        {
            if (index >= battle.Party.Count)
                return;
            if (reorderFrom >= 0)
            {
                battle.ReorderParty(reorderFrom, index);
                reorderFrom = -1;
                selectedMember = index;
                Show(Page.Party);
            }
            else
            {
                selectedMember = index;
                Show(Page.PartyActions);
            }
        }

        void UseSelectedItem()
        {
            if (page != Page.ItemDetails || selectedItem == null)
                return;
            string message = battle.UseItemFromMenu(selectedItem.id);
            Show(battle.ItemCount(selectedItem.id) > 0 ? Page.ItemDetails : Page.Items, message);
        }

        void RefreshItemList(string message)
        {
            foreach (var row in itemRows)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }

            itemRows.Clear();
            if (!itemScroll || !itemRowTemplate)
                return;
            foreach (var item in battle.Items)
            {
                int count = battle.ItemCount(item.id);
                if (count <= 0)
                    continue;
                var row = Instantiate(itemRowTemplate, itemScroll.content);
                row.name = "Item - " + item.id;
                row.gameObject.SetActive(true);
                row.GetComponentInChildren<Text>().text = item.displayName + "　× " + count;
                row.onClick.AddListener(() =>
                {
                    selectedItem = item;
                    itemScrollPosition = itemScroll.verticalNormalizedPosition;
                    Show(Page.ItemDetails);
                });
                itemRows.Add(row);
            }

            itemListHint.text = message ?? (itemRows.Count == 0 ? "持っている道具はありません。" : "道具を選んでください。ホイール・ドラッグでスクロールできます。");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemScroll.content);
            itemScroll.StopMovement();
            itemScroll.verticalNormalizedPosition = itemScrollPosition;
        }

        void Close()
        {
            if (!isOpen)
                return;
            isOpen = false;
            reorderFrom = -1;
            if (panel)
                panel.SetActive(false);
            if (player)
                player.MovementLocked = false;
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);
        }

        void Show(Page value, string message = null)
        {
            if (value != Page.Party)
                reorderFrom = -1;
            page = value;
            back.gameObject.SetActive(value != Page.Home);
            usePotion.gameObject.SetActive(value == Page.ItemDetails);
            if (previousItem)
                previousItem.gameObject.SetActive(false);
            if (nextItem)
                nextItem.gameObject.SetActive(false);
            bool listVisible = value == Page.Party || value == Page.PartyActions;
            if (partyList)
                partyList.SetActive(listVisible);
            if (partyActions)
                partyActions.SetActive(value == Page.PartyActions);
            body.gameObject.SetActive(!listVisible && value != Page.Items);
            if (itemScroll)
                itemScroll.gameObject.SetActive(value == Page.Items);
            if (itemListHint)
                itemListHint.gameObject.SetActive(value == Page.Items);
            selectedMember = Mathf.Clamp(selectedMember, 0, battle.Party.Count - 1);
            var monster = (value == Page.PartyStats) ? battle.Party[selectedMember] : battle.Companion;
            switch (value)
            {
                case Page.Party:
                case Page.PartyActions:
                    title.text = "なかま　" + battle.Party.Count + " / 6";
                    for (int i = 0; i < partyCards.Length; i++)
                    {
                        var card = partyCards[i];
                        bool visible = i < battle.Party.Count && (value == Page.Party || i == selectedMember);
                        card.gameObject.SetActive(visible);
                        if (!visible)
                            continue;
                        var member = battle.Party[i];
                        var rect = card.GetComponent<RectTransform>();
                        if (value == Page.PartyActions)
                        {
                            rect.anchorMin = new Vector2(0, .64f);
                            rect.anchorMax = Vector2.one;
                        }
                        else
                        {
                            int column = i % 2, row = i / 2;
                            rect.anchorMin = new Vector2(column * .51f, .75f - row * .28f);
                            rect.anchorMax = new Vector2(column * .51f + .49f, .99f - row * .28f);
                        }

                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                        var icon = card.transform.Find("Portrait").GetComponent<Image>();
                        icon.sprite = member.Definition.portrait;
                        icon.enabled = icon.sprite != null;
                        card.transform.Find("Summary").GetComponent<Text>().text = (i == 0 ? "先頭　" : "") + member.Definition.displayName + "\nLv." + member.Level + "　HP " + member.Data.currentHp + "/" + member.Hp;
                        var hp = card.transform.Find("HP Track/HP Fill").GetComponent<Image>();
                        hp.fillAmount = (float)member.Data.currentHp / member.Hp;
                        card.interactable = value == Page.Party;
                    }

                    if (partyHint)
                        partyHint.text = reorderFrom >= 0 ? "入れ替える相手を選んでください。戻るで取消。" : value == Page.Party ? "仲間を選んでください。先頭の仲間が戦闘に出ます。" : battle.Party.Count < 2 ? "並び替えには仲間が2匹以上必要です。" : "どうしますか？";
                    if (reorderParty)
                        reorderParty.interactable = battle.Party.Count > 1;
                    break;
                case Page.PartyStats:
                    title.text = monster.Definition.displayName + "のつよさ";
                    var text = new StringBuilder();
                    text.AppendLine(monster.Definition.displayName + "　Lv." + monster.Level + "　HP " + monster.Data.currentHp + " / " + monster.Hp);
                    text.AppendLine("性格：" + Progression.NatureDescription(monster.Data.nature));
                    text.AppendLine(monster.Level == 100 ? "レベルは最大です。" : "次のレベルまで " + (Progression.ExperienceAt(monster.Level + 1) - monster.Data.experience) + " EXP");
                    text.AppendLine("HP " + monster.Hp + "　攻撃 " + monster.Stat(1) + "　防御 " + monster.Stat(2));
                    text.AppendLine("特攻 " + monster.Stat(3) + "　特防 " + monster.Stat(4) + "　素早さ " + monster.Stat(5));
                    text.AppendLine("素質：" + string.Join(" / ", monster.Data.iv));
                    text.AppendLine("鍛錬値：" + string.Join(" / ", monster.Data.ev));
                    text.AppendLine("（HP / 攻 / 防 / 特攻 / 特防 / 速）");
                    for (int i = 0; i < monster.Moves.Length; i++)
                        text.AppendLine(monster.Moves[i].displayName + "　PP " + monster.Data.pp[i] + " / " + monster.Moves[i].maxPp);
                    body.text = text.ToString();
                    break;
                case Page.Items:
                    title.text = "どうぐ";
                    RefreshItemList(message);
                    break;
                case Page.ItemDetails:
                    title.text = "どうぐの説明";
                    var item = selectedItem;
                    if (item == null)
                    {
                        Show(Page.Items);
                        return;
                    }

                    body.text = item.displayName + "　× " + battle.ItemCount(item.id) + "\n\n" + item.description + "\n\n使用先：" + monster.Definition.displayName + "\nHP " + monster.Data.currentHp + " / " + monster.Hp + "\n\n" + (item.FieldImplemented ? "この道具を使いますか？" : item.effect == ItemEffect.Capture ? "野生との戦闘中に使う道具です。" : "この道具はまだ使えません。") + "\n" + (message ?? "");
                    usePotion.interactable = battle.ItemCount(item.id) > 0 && item.FieldImplemented;
                    usePotion.GetComponentInChildren<Text>().text = "使う";
                    break;
                case Page.Save:
                    title.text = "セーブ";
                    body.text = (message ?? "") + "\n\n保存内容：仲間の成長・HP・PP、道具、所持金、現在地、復帰先、対戦相手への勝利\n保存枠：1つ（上書き保存）";
                    break;
                case Page.Help:
                    title.text = "操作説明";
                    body.text = "WASD / 矢印：移動\nE / Enter：目の前の人と話す\nTab / M / Esc：メニュー\nメニュー内のEsc：戻る・閉じる\n\n自宅・回復所でHPとPPを全回復できます。\n西の門から草原へ進めます。\n保存はメニューのセーブから行ってください。自動セーブはありません。";
                    break;
                default:
                    title.text = "メニュー　所持金 " + battle.Money + " コイン";
                    body.text = "目標：" + battle.AdventureGoal + "\n\nなかまの様子を見たり、道具を使えます。\n冒険を終える前にセーブしましょう。\n\nTab / M / Esc または「閉じる」で戻ります。";
                    break;
            }

            if (EventSystem.current)
            {
                GameObject selected = value == Page.Party && partyCards.Length > selectedMember ? partyCards[selectedMember].gameObject : value == Page.PartyActions && viewStats ? viewStats.gameObject : value == Page.PartyStats ? back.gameObject : value == Page.ItemDetails ? (usePotion.interactable ? usePotion.gameObject : back.gameObject) : value == Page.Items && itemRows.Count > 0 ? (itemRows.Find(row => selectedItem && row.name == "Item - " + selectedItem.id) ?? itemRows[0]).gameObject : null;
                EventSystem.current.SetSelectedGameObject(selected);
            }
        }

        void OnDisable()
        {
            Close();
        }
    }
}
