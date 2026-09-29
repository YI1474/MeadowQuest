using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace MeadowQuest
{
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] GridMover player;
        [SerializeField] MonsterDefinition partner;
        [SerializeField] BattleView view;
        [SerializeField] GameObject dialogueCanvas;
        MonsterDefinition opponent;
        MonsterIndividual foe;
        MonsterParty roster;
        int activeIndex;
        bool forcedSwitch;
        string recoveryRoom="home";
        MonsterIndividual companion => IsActive?roster.Members[activeIndex]:roster.Lead;
        public System.Collections.Generic.IReadOnlyList<MonsterIndividual> Party => roster.Members;
        public bool ReorderParty(int from,int to) => progressionReady && !IsActive && roster.Swap(from,to);
        public bool AddPartyMember(MonsterIndividual monster) => progressionReady && !IsActive && roster.TryAdd(monster);
        readonly System.Random individualRandom=new System.Random();
        bool progressionReady;
        readonly System.Collections.Generic.HashSet<string> defeatedTrainers=new System.Collections.Generic.HashSet<string>();
        TrainerNpc activeTrainer;
        TrainerLineup enemyTeam;
        bool pendingClear;
        public string AdventureGoal => AdventureProgress.Goal(TrainerDefeated);
        public bool CanChallengeTrainer(TrainerNpc trainer) => trainer && AdventureProgress.CanChallenge(trainer.id,TrainerDefeated);
        public bool TrainerDefeated(string id) => defeatedTrainers.Contains(id);
        public bool BeginTrainer(TrainerNpc trainer)
        {
            if(!trainer || string.IsNullOrWhiteSpace(trainer.id) || !CanChallengeTrainer(trainer)) return false;
            TrainerLineup prepared;
            try
            {
                var entries=trainer.team!=null && trainer.team.Length>0?trainer.team:new[]{new TrainerNpc.TeamMember{monster=trainer.monster,level=trainer.levelOverride>0?trainer.levelOverride:trainer.monster.level}};
                prepared=new TrainerLineup(entries.Select(entry=>
                {
                    if(entry==null || !entry.monster || entry.level<1 || entry.level>100) throw new System.InvalidOperationException("対戦相手の手持ちを確認してください。");
                    var member=MonsterIndividual.Create(entry.monster,individualRandom);
                    member.Data.experience=Progression.ExperienceAt(entry.level); member.Heal(); return member;
                }).ToArray());
            }
            catch(System.Exception e) {Debug.LogError(e.Message); return false;}
            if(!Begin(prepared.Current.Definition)) return false;
            activeTrainer=trainer;
            enemyTeam=prepared; foe=prepared.Current;
            session=new BattleSession(companion.Data.currentHp,companion.Hp,foe.Hp);
            Refresh(trainer.displayName+"が勝負を挑んできた！\n"+opponent.displayName+"を繰り出した！\nクリック／Enterで次へ");
            return true;
        }
        Inventory inventory;
        int money;
        bool captureGiftReceived;
        public int Money => money;
        public string TradeItem(string id,int quantity,bool buying)
        {
            if(!progressionReady || IsActive) return "今は取引できません。";
            var item=Items.FirstOrDefault(candidate=>candidate.id==id);
            var error=ShopRules.Trade(inventory,ref money,item,quantity,buying);
            return error ?? item.displayName+"を"+quantity+"個"+(buying?"買いました。":"売りました。");
        }
        public ItemDefinition[] Items { get; private set; } = new ItemDefinition[0];
        Vector3Int? savedPosition;
        public MonsterIndividual Companion => companion;
        public bool ProgressionReady => progressionReady;
        public int ItemCount(string id) => inventory!=null?inventory.Count(id):0;
        public Vector3Int? SavedPosition => savedPosition;
        BattleSession session;
        bool busy;
        ItemDefinition selectedCapture;
        ItemDefinition[] captureItems=new ItemDefinition[0];
        ItemDefinition[] ownedBattleItems=new ItemDefinition[0];
        int battleItemPage;
        int introFrame;
        public bool IsActive { get; private set; }
        public event System.Action Finished;
        public void Configure(GridMover mover,MonsterDefinition starter,BattleView screen,GameObject dialogue)
        { player=mover; partner=starter; view=screen; dialogueCanvas=dialogue; }
        void Start()
        {
            if(!FindAnyObjectByType<TitleMenu>())
            { var error=StartAdventure(true); if(error!=null) Debug.LogError(error); }
            view.root.GetComponent<Canvas>().enabled=true;
            view.root.SetActive(false);
            view.BindMoves();
            if(view.clearPanel) view.clearPanel.SetActive(false);
            if(view.clearConfirm) view.clearConfirm.onClick.AddListener(()=>
            {var ending=view.clearPanel.GetComponent<EndingSequence>(); if(ending) ending.Confirm(); else {view.clearPanel.SetActive(false); Close();}});
            if(view.defeatShade) view.defeatShade.gameObject.SetActive(false);
            for(int i=0;i<view.partyButtons.Length;i++) {int slot=i; view.partyButtons[i].onClick.AddListener(()=>SelectPartyTarget(slot));}
            view.MoveSelected+=Attack;
            view.escape.onClick.AddListener(Escape);
            view.close.onClick.AddListener(Close);
            if(view.next) view.next.onClick.AddListener(()=>Navigate(BattleView.Screen.Commands));
            if(view.fight) view.fight.onClick.AddListener(()=>Navigate(BattleView.Screen.Moves));
            if(view.items) view.items.onClick.AddListener(()=>Navigate(BattleView.Screen.Items));
            if(view.party) view.party.onClick.AddListener(()=>Navigate(BattleView.Screen.Party));
            if(view.back) view.back.onClick.AddListener(()=>Navigate((view.CurrentScreen==BattleView.Screen.CaptureConfirm || view.CurrentScreen==BattleView.Screen.ItemTarget)?BattleView.Screen.Items:BattleView.Screen.Commands));
            captureItems=Items.Where(item=>item.useInBattle && (item.effect==ItemEffect.Capture || Inventory.IsRecovery(item))).ToArray();
            for(int i=0;i<view.captureButtons.Length;i++) { int index=i; view.captureButtons[i].onClick.AddListener(()=>SelectCapture(index)); }
            if(view.captureUse) view.captureUse.onClick.AddListener(UseCapture);
            if(view.previousBattleItems) view.previousBattleItems.onClick.AddListener(()=>ChangeItemPage(-1));
            if(view.nextBattleItems) view.nextBattleItems.onClick.AddListener(()=>ChangeItemPage(1));
        }
        public string StartAdventure(bool loadSave)
        {
            if(progressionReady || IsActive) return "冒険はすでに開始しています。";
            try
            {
                var catalog=Resources.Load<MonsterCatalog>("MonsterCatalog");
                if(!catalog) throw new System.InvalidOperationException("Data > Import CSVでモンスター一覧を取り込んでください。");
                ProgressSaveData saved=null;
                var first=(loadSave?ProgressSave.Load(partner,out saved,catalog.Find):null) ?? MonsterIndividual.Create(partner,individualRandom);
                roster=new MonsterParty(saved!=null?saved.party.Select(data=>new MonsterIndividual(catalog.Find(data.speciesId),data)):new[]{first});
                defeatedTrainers.Clear();
                if(saved!=null) foreach(string id in saved.defeatedTrainers) defeatedTrainers.Add(id);
                Items=Resources.LoadAll<ItemDefinition>("Items").OrderBy(item=>item.effect).ThenBy(item=>item.buyPrice).ThenBy(item=>item.id).ToArray();
                if(Items.Length==0) throw new System.InvalidOperationException("Data > Import CSVで道具を取り込んでください。");
                inventory=new Inventory(saved!=null?saved.inventory:Items.Select(item=>new ItemStack(item.id,item.initialCount)).ToArray());
                recoveryRoom=saved!=null?saved.recoveryRoom:"home";
                money=saved!=null?saved.money:ShopRules.StartingMoney;
                captureGiftReceived=saved!=null?saved.captureGiftReceived:ItemCount("bond_thread")>0;
                savedPosition=saved!=null && saved.hasPosition?new Vector3Int(saved.cellX,saved.cellY,0):(Vector3Int?)null;
                captureItems=Items.Where(item=>item.useInBattle && (item.effect==ItemEffect.Capture || Inventory.IsRecovery(item))).ToArray();
                progressionReady=true;
                return null;
            }
            catch(System.Exception e) { progressionReady=false; return "育成データを開始できません："+e.Message; }
        }
        public bool Begin(MonsterDefinition enemy)
        {
            if(!progressionReady || IsActive || player.MovementLocked || player.IsMoving || !enemy || !partner) return false;
            opponent=enemy;
            activeTrainer=null;
            enemyTeam=null;
            pendingClear=false;
            try { foe=MonsterIndividual.Create(enemy,individualRandom); }
            catch(System.Exception e) { Debug.LogError(e.Message); return false; }
            activeIndex=Enumerable.Range(0,roster.Members.Count).FirstOrDefault(i=>roster.Members[i].Data.currentHp>0);
            if(roster.Members[activeIndex].Data.currentHp<=0) return false;
            forcedSwitch=false; IsActive=true;
            session=new BattleSession(companion.Data.currentHp,companion.Hp,foe.Hp);
            IsActive=true; busy=false; player.MovementLocked=true;
            if(dialogueCanvas) dialogueCanvas.SetActive(false);
            view.root.SetActive(true);
            view.CurrentScreen=BattleView.Screen.Intro;
            introFrame=Time.frameCount;
            Refresh("野生の"+opponent.displayName+"があらわれた！\nクリック／Enterで次へ");
            return true;
        }
        void Refresh(string text)
        {
            view.Display(companion,foe,session,text,!busy);
            if(activeTrainer && enemyTeam!=null) view.enemyLabel.text+="\n相手の手持ち "+(enemyTeam.Index+1)+" / "+enemyTeam.Count;
            var closeLabel=view.close.GetComponentInChildren<UnityEngine.UI.Text>();
            if(closeLabel) closeLabel.text=activeTrainer && enemyTeam!=null && enemyTeam.HasNext && session.State==BattleSession.Phase.Won?"次の相手へ":"次へ";
            bool partyVisible=view.CurrentScreen==BattleView.Screen.Party || view.CurrentScreen==BattleView.Screen.ItemTarget;
            if(partyVisible && view.details) view.details.gameObject.SetActive(false);
            for(int i=0;i<view.partyButtons.Length;i++)
            {
                var button=view.partyButtons[i]; button.gameObject.SetActive(partyVisible && i<roster.Members.Count);
                if(i>=roster.Members.Count) continue;
                var member=roster.Members[i]; button.GetComponentInChildren<UnityEngine.UI.Text>().text=member.Definition.displayName+" Lv."+member.Level+"　HP "+member.Data.currentHp+"/"+member.Hp+(i==activeIndex?"（出場中）":"");
                button.interactable=!busy && (view.CurrentScreen==BattleView.Screen.ItemTarget || i!=activeIndex && member.Data.currentHp>0);
            }
            if(forcedSwitch) {view.close.gameObject.SetActive(false); if(view.back) view.back.gameObject.SetActive(false);}
            bool usable=!activeTrainer && roster.Members.Count<MonsterParty.Capacity;
            ownedBattleItems=captureItems.Where(item=>ItemCount(item.id)>0).ToArray();
            battleItemPage=Mathf.Clamp(battleItemPage,0,Mathf.Max(0,(ownedBattleItems.Length-1)/3));
            for(int i=0;i<view.captureButtons.Length;i++)
            {
                var button=view.captureButtons[i]; int slot=battleItemPage*3+i; var item=slot<ownedBattleItems.Length?ownedBattleItems[slot]:null;
                button.gameObject.SetActive(view.CurrentScreen==BattleView.Screen.Items && item && ItemCount(item.id)>0);
                button.interactable=!busy && item && (item.effect!=ItemEffect.Capture || usable);
                if(item) button.GetComponentInChildren<UnityEngine.UI.Text>().text=item.displayName+" × "+ItemCount(item.id);
            }
            if(view.CurrentScreen==BattleView.Screen.Items)
            {
                if(view.details) view.details.gameObject.SetActive(false);
                view.message.text=ownedBattleItems.Length==0?"使える道具を持っていません。道具屋で購入できます。":"道具を選んでください。　"+(battleItemPage+1)+" / "+Mathf.Max(1,(ownedBattleItems.Length+2)/3);
                if(view.previousBattleItems) view.previousBattleItems.interactable=!busy && battleItemPage>0;
                if(view.nextBattleItems) view.nextBattleItems.interactable=!busy && (battleItemPage+1)*3<ownedBattleItems.Length;
            }
        }
        public string ReceiveCaptureSupplies()
        {
            if(!progressionReady || IsActive) return "今は道具を渡せません。";
            if(captureGiftReceived) return "西の草原と洞窟を抜け、岩峰の街の試練場を目指そう！ 縁結びの糸は野生との戦闘で使えるよ。";
            int amount=System.Math.Min(5,999-ItemCount("bond_thread"));
            if(amount==0) return "道具がいっぱいだね。";
            inventory.Add("bond_thread",amount); captureGiftReceived=true; return "冒険の応援に、縁結びの糸を"+amount+"個どうぞ！ 最初の一度だけの贈り物だよ。";
        }
        void ChangeItemPage(int direction)
        {
            if(!IsActive || busy || view.CurrentScreen!=BattleView.Screen.Items) return;
            battleItemPage=Mathf.Clamp(battleItemPage+direction,0,Mathf.Max(0,(ownedBattleItems.Length-1)/3)); Refresh("どうぐ");
        }
        void SelectCapture(int index)
        {
            int slot=battleItemPage*3+index;
            if(!IsActive || busy || view.CurrentScreen!=BattleView.Screen.Items || slot<0 || slot>=ownedBattleItems.Length) return;
            selectedCapture=ownedBattleItems[slot];
            if(selectedCapture.effect==ItemEffect.Capture)
            {
                if(activeTrainer || roster.Members.Count>=6) return;
                view.CurrentScreen=BattleView.Screen.CaptureConfirm;
                Refresh(selectedCapture.displayName+"を1個使いますか？\nHPが少ないほど捕まえやすくなります。");
            }
            else
            {
                view.CurrentScreen=BattleView.Screen.ItemTarget;
                Refresh(selectedCapture.displayName+"："+selectedCapture.description+"\n使う仲間を選んでください。");
            }
        }
        void SelectPartyTarget(int index)
        {
            if(view.CurrentScreen!=BattleView.Screen.ItemTarget) {SwitchMember(index); return;}
            if(!IsActive || busy || forcedSwitch || !selectedCapture) return;
            if(!BattleItemRules.Use(session,roster,activeIndex,index,inventory,selectedCapture))
            {Refresh("この仲間には効果がありません。道具とターンは消費していません。\n別の仲間を選ぶか、戻ってください。"); return;}
            StartCoroutine(CounterAfterSwitch(roster.Members[index].Definition.displayName+"に"+selectedCapture.displayName+"を使った！"));
        }
        void UseCapture()
        {
            if(!IsActive || busy || session.State!=BattleSession.Phase.PlayerTurn || view.CurrentScreen!=BattleView.Screen.CaptureConfirm || !selectedCapture) return;
            StartCoroutine(ResolveCapture());
        }
        IEnumerator ResolveCapture()
        {
            busy=true;
            var result=CaptureRules.Attempt(roster,foe,inventory,selectedCapture,activeTrainer,individualRandom.NextDouble());
            view.CurrentScreen=BattleView.Screen.Resolving;
            if(result==CaptureRules.Result.Blocked) { busy=false; Navigate(BattleView.Screen.Commands); yield break; }
            Refresh(selectedCapture.displayName+"を使った！"); yield return new WaitForSeconds(.8f);
            if(result==CaptureRules.Result.Caught)
            {
                session.Capture(); busy=false; Refresh(foe.Definition.displayName+"が仲間になった！\n手持ち "+roster.Members.Count+" / 6"); yield break;
            }
            Refresh("糸がほどけてしまった……！"); yield return new WaitForSeconds(.8f);
            session.StartRound(true); session.Attack(0);
            int slot=System.Array.FindIndex(foe.Data.pp,p=>p>0); var move=slot>=0?foe.Moves[slot]:null;
            if(slot>=0) foe.Data.pp[slot]--;
            int damage=Calculate(foe,companion,move,out string reply); session.Counterattack(damage); companion.Data.currentHp=session.PlayerHp;
            Refresh(reply); yield return new WaitForSeconds(.8f);
            if(session.State==BattleSession.Phase.Lost) { FinishBattle(); yield break; }
            busy=false; Navigate(BattleView.Screen.Commands);
        }
        void Update()
        {
            if(!IsActive || busy || !Application.isFocused || Time.frameCount<=introFrame+1) return;
            if(view.CurrentScreen!=BattleView.Screen.Intro) return;
            var keyboard=Keyboard.current;
            bool confirm=keyboard!=null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            bool click=Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame;
            if(!confirm && !click) return;
            // Do not let the same press also submit/click a newly revealed command.
            StartCoroutine(AdvanceIntroduction());
        }
        IEnumerator AdvanceIntroduction()
        {
            busy=true;
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            busy=false;
            if(IsActive && view.CurrentScreen==BattleView.Screen.Intro) Navigate(BattleView.Screen.Commands);
        }
        void Navigate(BattleView.Screen screen)
        {
            if(!IsActive || busy || forcedSwitch || session.State!=BattleSession.Phase.PlayerTurn) return;
            view.CurrentScreen=screen;
            Refresh(screen==BattleView.Screen.Moves?"使う技を選んでください。":screen==BattleView.Screen.Items?"どうぐ":screen==BattleView.Screen.Party?"なかま":"どうする？");
        }
        int Calculate(MonsterIndividual source,MonsterIndividual target,MoveDefinition move,out string text)
        {
            string title=move?move.displayName:"あがく";
            string type=move?move.element:"None";
            text=source.Definition.displayName+"の"+title+"！";
            if(Random.Range(1,101)>(move?move.accuracy:100)) { text+="　外れた！"; return 0; }
            bool critical=Random.Range(0,24)==0;
            bool special=move && move.special;
            decimal effectiveness=BattleMath.Effectiveness(type,target.Definition.element);
            int damage=BattleMath.Damage(source.Level,move?move.power:40,
                source.Stat(special?3:1),target.Stat(special?4:2),
                effectiveness,type==source.Definition.element,critical,Random.Range(85,101));
            text+="　"+damage+"ダメージ！";
            if(effectiveness==0) text+=" 効果がない！";
            else
            {
                if(critical) text+=" 急所に当たった！";
                if(effectiveness>1) text+=" 効果ばつぐん！";
                if(effectiveness<1) text+=" 効果はいまひとつ。";
            }
            return damage;
        }
        void Attack(int index)
        {
            if(!IsActive || busy || session.State!=BattleSession.Phase.PlayerTurn || view.CurrentScreen!=BattleView.Screen.Moves) return;
            if(!companion.CanUseAnyMove) { if(index==0) StartCoroutine(ResolveTurn(-1)); return; }
            if(index<0 || index>=companion.Moves.Length || companion.Data.pp[index]<=0) return;
            StartCoroutine(ResolveTurn(index));
        }
        IEnumerator ResolveTurn(int selectedIndex)
        {
            busy=true;
            view.CurrentScreen=BattleView.Screen.Resolving;
            int enemyIndex=System.Array.FindIndex(foe.Data.pp,p=>p>0);
            bool playerFirst=companion.Stat(5)>foe.Stat(5) || companion.Stat(5)==foe.Stat(5) && Random.Range(0,2)==0;
            session.StartRound(playerFirst);
            for(int action=0;action<2;action++)
            {
                bool isPlayer=action==0?playerFirst:!playerFirst;
                var source=isPlayer?companion:foe; var target=isPlayer?foe:companion;
                int slot=isPlayer?selectedIndex:enemyIndex;
                var move=slot>=0?source.Moves[slot]:null;
                if(slot>=0) source.Data.pp[slot]--;
                int damage=Calculate(source,target,move,out string text);
                if(isPlayer) session.Attack(damage); else session.Counterattack(damage);
                companion.Data.currentHp=session.PlayerHp; foe.Data.currentHp=session.EnemyHp;
                Refresh(text); yield return new WaitForSeconds(.8f);
                if(session.State==BattleSession.Phase.Won || session.State==BattleSession.Phase.Lost)
                { FinishBattle(); yield break; }
            }
            busy=false; view.CurrentScreen=BattleView.Screen.Commands; Refresh("どうする？");
        }
        void SwitchMember(int index)
        {
            if(!IsActive || busy || view.CurrentScreen!=BattleView.Screen.Party || index<0 || index>=roster.Members.Count || index==activeIndex) return;
            var member=roster.Members[index]; bool free=forcedSwitch;
            if(!session.SwitchPlayer(member.Data.currentHp,member.Hp,free)) return;
            activeIndex=index; forcedSwitch=false;
            if(free) {view.CurrentScreen=BattleView.Screen.Commands; Refresh(member.Definition.displayName+"、頼んだ！");}
            else StartCoroutine(CounterAfterSwitch());
        }
        IEnumerator CounterAfterSwitch(string actionText=null)
        {
            busy=true; view.CurrentScreen=BattleView.Screen.Resolving; Refresh(actionText ?? companion.Definition.displayName+"に交代した！"); yield return new WaitForSeconds(.8f);
            int slot=System.Array.FindIndex(foe.Data.pp,p=>p>0); var move=slot>=0?foe.Moves[slot]:null;
            if(slot>=0) foe.Data.pp[slot]--;
            int damage=Calculate(foe,companion,move,out string text); session.Counterattack(damage); companion.Data.currentHp=session.PlayerHp;
            Refresh(text); yield return new WaitForSeconds(.8f);
            if(session.State==BattleSession.Phase.Lost) {FinishBattle(); yield break;}
            busy=false; Navigate(BattleView.Screen.Commands);
        }
        IEnumerator ReturnAfterDefeat()
        {
            busy=true; forcedSwitch=false; view.CurrentScreen=BattleView.Screen.Resolving; Refresh("仲間たちは力尽きてしまった……。");
            if(view.defeatShade)
            {
                view.defeatShade.gameObject.SetActive(true); view.defeatText.text="仲間たちは力尽きてしまった……。";
                for(float t=0;t<.5f;t+=Time.unscaledDeltaTime) {view.defeatShade.color=new Color(0,0,0,Mathf.Clamp01(t/.5f)); yield return null;}
                view.defeatShade.color=Color.black;
            }
            yield return new WaitForSecondsRealtime(1);
            foreach(var member in roster.Members) member.Heal();
            var town=FindAnyObjectByType<TownWorld>(); if(town) town.ReturnToRecovery(recoveryRoom);
            if(view.defeatText) view.defeatText.text="仲間たちは元気を取り戻した！";
            yield return new WaitForSecondsRealtime(.8f);
            if(view.defeatShade) view.defeatShade.gameObject.SetActive(false);
            busy=false; Close();
        }
        void FinishBattle()
        {
            string text;
            System.Collections.Generic.List<PartyExperience.LevelUp> levelUps=null;
            if(session.State==BattleSession.Phase.Won)
            {
                int reward=Progression.Reward(opponent.baseExperience,foe.Level,companion.Level);
                levelUps=PartyExperience.Award(roster,activeIndex,reward,opponent.effortYield);
                text=opponent.displayName+"を倒した！ 手持ち全員に経験値を"+reward+"ずつ！";
                if(activeTrainer && !enemyTeam.HasNext)
                {
                    bool firstWin=defeatedTrainers.Add(activeTrainer.id);
                    int prize=System.Math.Min(System.Math.Max(0,firstWin?activeTrainer.prizeMoney:activeTrainer.prizeMoney/2),ShopRules.MaxMoney-money); money+=prize;
                    text=activeTrainer.displayName+"に勝利！\n手持ち全員に経験値を"+reward+"ずつ、賞金を"+prize+"獲得！";
                    pendingClear=activeTrainer.id==AdventureProgress.FinalTrainer;
                }
                if(levelUps.Count>0) text+="\n"+levelUps.Count+"匹がレベルアップ！";
                if(roster.Members.Any(member=>member.Level==100)) text+="\nLv.100の仲間は経験値上限です。";
            }
            else
            {
                if(roster.Members.Any(member=>member.Data.currentHp>0)) {forcedSwitch=true; busy=false; view.CurrentScreen=BattleView.Screen.Party; Refresh("仲間が力尽きた……。次に出す仲間を選んでください。");}
                else StartCoroutine(ReturnAfterDefeat());
                return;
            }
            busy=false; Refresh(text);
            view.ShowLevelUps(levelUps);
        }
        string SaveProgress()
        {
            try { ProgressSave.Save(roster.Members,inventory.Snapshot(),Vector3Int.FloorToInt(player.transform.position),defeatedTrainers.OrderBy(id=>id).ToArray(),money,captureGiftReceived,recoveryRoom); return ""; }
            catch(System.Exception e) { Debug.LogError("保存失敗: "+e.Message); return "\n保存に失敗しました。"; }
        }
        public string RecoverCompanion(string roomId="home")
        {
            if(!progressionReady) return "仲間データを読み込めません。Consoleを確認してください。";
            if(IsActive) return "戦闘中は回復できません。";
            recoveryRoom=roomId=="ridge_clinic"?"ridge_clinic":roomId=="clinic"?"clinic":"home";
            foreach(var member in roster.Members) member.Heal();
            return "仲間全員のHPとPPを全回復しました。気をつけて冒険してね！";
        }
        public string SaveFromMenu()
        {
            if(!progressionReady || IsActive) return "今はセーブできません。";
            string error=SaveProgress();
            return error.Length==0?"セーブしました。次回はこの場所から再開します。":error;
        }
        public string UsePotionFromMenu() => UseItemFromMenu("potion");
        public string UseItemFromMenu(string id)
        {
            if(!progressionReady || IsActive) return "今は道具を使えません。";
            var item=Items.FirstOrDefault(candidate=>candidate.id==id);
            if(!item) return "道具が見つかりません。";
            if(!item.FieldImplemented) return item.effect==ItemEffect.Capture?"野生との戦闘中に使ってください。":"この道具の機能は準備中です。消費していません。";
            if(ItemCount(id)==0) return "この道具を持っていません。";
            return inventory.Use(item,companion)?item.displayName+"を使いました。":"今の仲間には効果がありません。消費していません。";
        }
        void Escape()
        {
            if(IsActive && activeTrainer && !busy && view.CurrentScreen==BattleView.Screen.Commands) { Refresh("対戦相手との勝負では逃げられません。"); return; }
            if(!IsActive || busy || view.CurrentScreen!=BattleView.Screen.Commands || !session.Escape()) return;
            Refresh("無事に逃げ出した！");
        }
        void Close()
        {
            if(view.clearPanel && view.clearPanel.activeSelf) return;
            if(view.ShowingLevelUp || forcedSwitch) return;
            if(!IsActive || busy || session.State==BattleSession.Phase.PlayerTurn || session.State==BattleSession.Phase.EnemyTurn) return;
            if(session.State==BattleSession.Phase.Won && enemyTeam!=null && enemyTeam.HasNext)
            {
                if(!enemyTeam.Advance()) return;
                foe=enemyTeam.Current; opponent=foe.Definition;
                session=new BattleSession(companion.Data.currentHp,companion.Hp,foe.Hp);
                view.CurrentScreen=BattleView.Screen.Intro; introFrame=Time.frameCount;
                Refresh(activeTrainer.displayName+"は"+opponent.displayName+"を繰り出した！\nクリック／Enterで次へ");
                return;
            }
            if(pendingClear && view.clearPanel)
            {
                pendingClear=false; view.clearPanel.SetActive(true);
                var ending=view.clearPanel.GetComponent<EndingSequence>(); if(ending) ending.Begin(Close);
                if(EventSystem.current) EventSystem.current.SetSelectedGameObject(view.clearConfirm.gameObject);
                return;
            }
            view.root.SetActive(false); IsActive=false;
            if(dialogueCanvas) dialogueCanvas.SetActive(true);
            StartCoroutine(ReleaseMovement());
            Finished?.Invoke();
        }
        IEnumerator ReleaseMovement()
        {
            // Do not carry a held movement key straight into another encounter.
            yield return null;
            var input=player.GetComponent<KeyboardMoveInput>();
            var approach=FindAnyObjectByType<TrainerApproach>();
            while(input.Direction!=Vector2Int.zero || approach && approach.IsBusy) yield return null;
            player.MovementLocked=false;
        }
        void OnDisable()
        {
            StopAllCoroutines();
            if(IsActive) { if(view) view.root.SetActive(false); if(dialogueCanvas) dialogueCanvas.SetActive(true); }
            if(player) player.MovementLocked=false;
            IsActive=false; busy=false;
        }
    }
}

