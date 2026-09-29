using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest
{
    public sealed class BattleView : MonoBehaviour
    {
        public GameObject root;
        public Text playerLabel,enemyLabel,message;
        public Image playerPortrait,enemyPortrait;
        public Button attack,escape,close;
        public Button[] moveButtons = new Button[0];
        public enum Screen { Intro, Commands, Moves, Items, Party, Resolving, CaptureConfirm, ItemTarget }
        public Button previousBattleItems,nextBattleItems;
        public Button[] captureButtons=new Button[0];
        public Button captureUse;
        public Button[] partyButtons=new Button[0];
        public Image defeatShade;
        public Text defeatText;
        public Screen CurrentScreen { get; set; }
        public Button fight,items,party,back,next;
        public Text details;
        public GameObject levelUpPanel;
        public Text levelUpText;
        public Button levelUpConfirm;
        public GameObject clearPanel;
        public Button clearConfirm;
        readonly System.Collections.Generic.Queue<PartyExperience.LevelUp> pendingLevelUps=new System.Collections.Generic.Queue<PartyExperience.LevelUp>();
        public bool ShowingLevelUp => levelUpPanel && levelUpPanel.activeSelf;
        public void ShowLevelUps(System.Collections.Generic.IEnumerable<PartyExperience.LevelUp> changes)
        {
            pendingLevelUps.Clear();
            if(!levelUpPanel || !levelUpText || !levelUpConfirm) return;
            foreach(var change in changes) pendingLevelUps.Enqueue(change);
            NextLevelUp();
        }
        void NextLevelUp()
        {
            if(pendingLevelUps.Count>0)
            {
                var change=pendingLevelUps.Dequeue();
                ShowLevelUp(change.PreviousLevel,change.PreviousStats,change.Monster);
                return;
            }
            levelUpPanel.SetActive(false);
            if(UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(close.gameObject);
        }
        public void ShowLevelUp(int previousLevel,int[] previousStats,MonsterIndividual monster)
        {
            if(!levelUpPanel || !levelUpText) return;
            var text=new System.Text.StringBuilder();
            text.AppendLine(monster.Definition.displayName+"がレベルアップ！");
            text.AppendLine("Lv."+previousLevel+" → Lv."+monster.Level+"\n");
            for(int i=0;i<6;i++) text.AppendLine(Progression.StatNames[i]+"　"+previousStats[i]+" → "+monster.Stat(i)+"　（＋"+(monster.Stat(i)-previousStats[i])+")");
            levelUpText.text=text.ToString(); levelUpPanel.SetActive(true);
            if(UnityEngine.EventSystems.EventSystem.current) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(levelUpConfirm.gameObject);
        }
        public event System.Action<int> MoveSelected;
        public void BindMoves()
        {
            if(levelUpPanel) levelUpPanel.SetActive(false);
            if(levelUpConfirm) levelUpConfirm.onClick.AddListener(()=>
            {
                NextLevelUp();
            });
            var buttons=moveButtons!=null && moveButtons.Length>0?moveButtons:new[]{attack};
            for(int i=0;i<buttons.Length;i++)
            {
                int index=i;
                if(buttons[i]) buttons[i].onClick.AddListener(()=>MoveSelected?.Invoke(index));
            }
        }
        static string TypeName(string value)
        {
            switch(value) { case "Normal": return "ノーマル"; case "Water": return "水"; case "Grass": return "草"; case "Fire": return "炎"; default: return value; }
        }
        public void Display(MonsterIndividual individual,MonsterIndividual opponent,BattleSession session,string text,bool ready)
        {
            var player=individual.Definition; var enemy=opponent.Definition;
            playerPortrait.sprite=player.backPortrait ? player.backPortrait : player.portrait;
            enemyPortrait.sprite=enemy.portrait;
            playerLabel.text=player.displayName+" Lv."+individual.Level+"\nHP "+individual.Data.currentHp+" / "+individual.Hp;
            enemyLabel.text=enemy.displayName+" Lv."+opponent.Level+"\nHP "+opponent.Data.currentHp+" / "+opponent.Hp;
            message.text=text;
            bool ongoing=session.State==BattleSession.Phase.PlayerTurn || session.State==BattleSession.Phase.EnemyTurn;
            bool canAct=ready && session.State==BattleSession.Phase.PlayerTurn;
            var buttons=moveButtons!=null && moveButtons.Length>0?moveButtons:new[]{attack};
            bool allPpEmpty=!individual.CanUseAnyMove;
            for(int i=0;i<buttons.Length;i++)
            {
                var button=buttons[i]; if(!button) continue;
                var move=i<individual.Moves.Length?individual.Moves[i]:null;
                button.gameObject.SetActive(ongoing && CurrentScreen==Screen.Moves);
                bool fallback=allPpEmpty && i==0;
                button.interactable=canAct && (fallback || move && individual.Data.pp[i]>0);
                var label=button.GetComponentInChildren<Text>();
                if(label) label.text=fallback?"あがく（PP不要）":move?move.displayName+"\n"+TypeName(move.element)+"・"+(move.special?"特殊":"物理")+"　威力"+move.power+"　PP "+individual.Data.pp[i]+"/"+move.maxPp:"―";
            }
            bool commands=ongoing && CurrentScreen==Screen.Commands;
            Show(fight,commands,canAct); Show(items,commands,canAct); Show(party,commands,canAct);
            Show(back,ongoing && (CurrentScreen==Screen.Moves || CurrentScreen==Screen.Items || CurrentScreen==Screen.Party || CurrentScreen==Screen.CaptureConfirm || CurrentScreen==Screen.ItemTarget),canAct);
            Show(previousBattleItems,ongoing && CurrentScreen==Screen.Items,canAct);
            Show(nextBattleItems,ongoing && CurrentScreen==Screen.Items,canAct);
            foreach(var button in captureButtons) if(button) button.gameObject.SetActive(ongoing && CurrentScreen==Screen.Items);
            Show(captureUse,ongoing && CurrentScreen==Screen.CaptureConfirm,canAct);
            Show(next,false,false);
            if(details)
            {
                details.gameObject.SetActive(ongoing && (CurrentScreen==Screen.Items || CurrentScreen==Screen.Party));
                if(details.gameObject.activeSelf)
                    details.text=CurrentScreen==Screen.Items?"使う道具を選んでください。":player.displayName+" Lv."+individual.Level+"　HP "+individual.Data.currentHp+" / "+individual.Hp+"\n性格："+Progression.NatureDescription(individual.Data.nature)+"　次のLvまで："+(individual.Level==100?"最大":(Progression.ExperienceAt(individual.Level+1)-individual.Data.experience).ToString())+"\n素質："+string.Join(" / ",individual.Data.iv)+"\n鍛錬値："+string.Join(" / ",individual.Data.ev);
            }
            escape.gameObject.SetActive(commands);
            escape.interactable=canAct;
            close.gameObject.SetActive(!ongoing && ready);
        }
        static void Show(Button button,bool visible,bool enabled)
        { if(button) { button.gameObject.SetActive(visible); button.interactable=enabled; } }
    }
}



