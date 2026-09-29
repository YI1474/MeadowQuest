using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MeadowQuest
{
    [DefaultExecutionOrder(100)]
    public sealed class TitleMenu : MonoBehaviour
    {
        public TownWorld town;
        public GameObject panel;
        public Text description;
        public Button newGame,continueGame,help,confirm,back;
        bool starting;
        void Awake() { town.player.MovementLocked=true; }
        void Start()
        {
            panel.SetActive(true);
            newGame.onClick.AddListener(NewGame);
            continueGame.onClick.AddListener(()=>Begin(true));
            help.onClick.AddListener(()=>ShowDetails("WASD / 矢印：移動\nE / Enter：話す\nM / Tab / Esc：メニュー\nマウス：項目を選択\n\nセーブはメニューから手動で行います。\n洞窟を抜け、次の街の試練場に挑もう！",false));
            confirm.onClick.AddListener(()=>Begin(false));
            back.onClick.AddListener(Home);
            Home();
        }
        void Select(Button button)
        { if(EventSystem.current) EventSystem.current.SetSelectedGameObject(button.gameObject); }
        void Home()
        {
            newGame.gameObject.SetActive(true); continueGame.gameObject.SetActive(true); help.gameObject.SetActive(true);
            confirm.gameObject.SetActive(false); back.gameObject.SetActive(false);
            continueGame.interactable=File.Exists(ProgressSave.PathName);
            description.text="仲間と歩く、小さな冒険。\n保存はゲーム内メニューの「セーブ」から。";
            Select(continueGame.interactable?continueGame:newGame);
        }
        void ShowDetails(string text,bool canConfirm)
        {
            newGame.gameObject.SetActive(false); continueGame.gameObject.SetActive(false); help.gameObject.SetActive(false);
            confirm.gameObject.SetActive(canConfirm); back.gameObject.SetActive(true); description.text=text; Select(back);
        }
        void NewGame()
        {
            if(File.Exists(ProgressSave.PathName))
                ShowDetails("新しい冒険を始めますか？\n\n現在のセーブデータは残ります。\n新しい冒険で手動セーブすると、\n以前の冒険に上書きされます。",true);
            else Begin(false);
        }
        void Begin(bool loadSave)
        {
            if(starting) return;
            if(loadSave && !File.Exists(ProgressSave.PathName)) { Home(); return; }
            var error=town.battle.StartAdventure(loadSave);
            if(error!=null) { ShowDetails(error+"\n\n保存ファイルは変更していません。",false); return; }
            var saved=town.battle.SavedPosition;
            town.player.Teleport(saved.HasValue && town.map.IsWalkable(saved.Value)?saved.Value:town.start);
            starting=true; StartCoroutine(EnterWorld());
        }
        IEnumerator EnterWorld()
        {
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            panel.SetActive(false);
            // Keep the title's submit press from also talking to an NPC or moving.
            yield return null;
            var input=town.player.GetComponent<KeyboardMoveInput>();
            while(input.Direction!=Vector2Int.zero || input.InteractPressed) yield return null;
            town.player.MovementLocked=false;
        }
    }
}

