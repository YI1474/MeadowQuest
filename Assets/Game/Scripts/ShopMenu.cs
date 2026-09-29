using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace MeadowQuest
{
    public sealed class ShopMenu : MonoBehaviour
    {
        public BattleController battle;
        public GridMover player;
        public GameObject panel;
        public ScrollRect list;
        public Button rowTemplate,buy,sell,back,close,minus,plus,confirm;
        public Text heading,details;
        readonly List<Button> rows=new List<Button>();
        bool opened,buying=true;
        ItemDefinition selected;
        int quantity=1;
        public static int ClosedFrame { get; private set; }=-1;
        void Start()
        {
            panel.SetActive(false);
            buy.onClick.AddListener(()=>Browse(true)); sell.onClick.AddListener(()=>Browse(false));
            back.onClick.AddListener(()=>Browse(buying)); close.onClick.AddListener(Close);
            minus.onClick.AddListener(()=> {quantity=Mathf.Max(1,quantity-1); ShowDetails();});
            plus.onClick.AddListener(()=> {quantity=Mathf.Min(MaxQuantity(),quantity+1); ShowDetails();});
            confirm.onClick.AddListener(()=>
            {
                if(!selected) return;
                string result=battle.TradeItem(selected.id,quantity,buying);
                Browse(buying); details.text=result;
            });
        }
        public bool Open()
        {
            if(opened || !battle.ProgressionReady || battle.IsActive || player.MovementLocked || player.IsMoving) return false;
            opened=true; player.MovementLocked=true; panel.SetActive(true); Browse(true); return true;
        }
        void Update()
        {
            if(!opened || !Application.isFocused || Keyboard.current==null) return;
            if(Keyboard.current.escapeKey.wasPressedThisFrame) {if(selected) Browse(buying); else Close();}
        }
        void Browse(bool purchase)
        {
            buying=purchase; selected=null; quantity=1;
            foreach(var row in rows) {row.gameObject.SetActive(false); Destroy(row.gameObject);} rows.Clear();
            list.gameObject.SetActive(true); Controls(false);
            heading.text="道具屋　所持金 "+battle.Money+" コイン";
            details.text=buying?"買う道具を選んでください。":"売る道具を選んでください。";
            details.rectTransform.anchorMin=new Vector2(.06f,.14f); details.rectTransform.anchorMax=new Vector2(.94f,.23f);
            foreach(var item in battle.Items)
            {
                if(buying?(!ShopRules.Stocked(item) || item.buyPrice<=0):(battle.ItemCount(item.id)==0 || item.sellPrice<=0)) continue;
                var row=Instantiate(rowTemplate,list.content); row.gameObject.SetActive(true);
                row.GetComponentInChildren<Text>().text=item.displayName+"　"+(buying?item.buyPrice:item.sellPrice)+"コイン　所持 "+battle.ItemCount(item.id);
                row.onClick.AddListener(()=> {selected=item; quantity=1; ShowDetails();}); rows.Add(row);
            }
            if(rows.Count==0) details.text="取引できる道具がありません。";
            Canvas.ForceUpdateCanvases(); list.StopMovement(); list.verticalNormalizedPosition=1;
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(rows.Count>0?rows[0].gameObject:close.gameObject);
        }
        int MaxQuantity()
        {
            if(!selected) return 0;
            return buying?Mathf.Min(999-battle.ItemCount(selected.id),battle.Money/Mathf.Max(1,selected.buyPrice)):battle.ItemCount(selected.id);
        }
        void Controls(bool visible)
        {
            back.gameObject.SetActive(visible); minus.gameObject.SetActive(visible); plus.gameObject.SetActive(visible); confirm.gameObject.SetActive(visible);
        }
        void ShowDetails()
        {
            list.gameObject.SetActive(false); Controls(true);
            details.rectTransform.anchorMin=new Vector2(.06f,.20f); details.rectTransform.anchorMax=new Vector2(.94f,.72f);
            quantity=Mathf.Max(1,quantity);
            int max=MaxQuantity(); minus.interactable=quantity>1; plus.interactable=quantity<max; confirm.interactable=quantity<=max;
            details.text=selected.displayName+"\n"+selected.description+"\n\n所持："+battle.ItemCount(selected.id)+"個\n個数："+quantity+"個　合計："+((long)(buying?selected.buyPrice:selected.sellPrice)*quantity)+"コイン\n\n"+(max==0?(buying?"所持金不足、または所持上限です。":"売れる道具がありません。"):buying?"この内容で購入しますか？":"この内容で売却しますか？");
            confirm.GetComponentInChildren<Text>().text=buying?"購入する":"売却する";
        }
        void Close()
        {
            if(!opened) return; opened=false; selected=null; panel.SetActive(false); player.MovementLocked=false; ClosedFrame=Time.frameCount;
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        void OnDisable() { Close(); }
    }
}
