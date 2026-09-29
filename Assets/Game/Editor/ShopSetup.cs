using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest.Editor
{
    [InitializeOnLoad] public static class ShopSetup
    {
        static ShopSetup() { EditorApplication.delayCall+=Install; EditorSceneManager.sceneOpened+=(s,m)=>Install(); }
        [MenuItem("Meadow Quest/Setup/Shop")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            var town=Object.FindAnyObjectByType<TownWorld>(); var map=Object.FindAnyObjectByType<GridMap>();
            if(!town || !map || town.gameObject.scene.path!="Assets/Game/Scenes/Meadow.unity") return;
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Art/Fonts/NotoSansCJKjp-Regular.otf");
            if(!font) return;
            if(!Object.FindAnyObjectByType<ShopMenu>(FindObjectsInactive.Include))
            {
                var root=new GameObject("Shop Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(ShopMenu)); Undo.RegisterCreatedObjectUndo(root,"Setup Shop");
                var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=45;
                var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(960,600); scaler.matchWidthOrHeight=.5f;
                var shop=root.GetComponent<ShopMenu>(); shop.battle=town.battle; shop.player=town.player;
                var panel=Box(root.transform,"Shop Panel",.06f,.07f,.94f,.94f); shop.panel=panel.gameObject;
                shop.heading=Label(panel,font,"Title",.06f,.88f,.94f,.98f,26);
                shop.buy=Button(panel,font,"Buy","買う",.06f,.76f,.32f,.86f); shop.sell=Button(panel,font,"Sell","売る",.36f,.76f,.62f,.86f); shop.close=Button(panel,font,"Close","やめる",.68f,.76f,.94f,.86f);
                shop.details=Label(panel,font,"Details",.06f,.20f,.94f,.72f,21);
                var scrollRoot=Box(panel,"Shop List",.06f,.24f,.94f,.72f); var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>(); shop.list=scroll;
                var viewport=Box(scrollRoot,"Viewport",0,0,.95f,1); viewport.gameObject.AddComponent<RectMask2D>(); scroll.viewport=viewport;
                var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter)); content.transform.SetParent(viewport,false);
                var rect=content.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(0,1); rect.anchorMax=Vector2.one; rect.pivot=new Vector2(.5f,1); rect.sizeDelta=Vector2.zero;
                var layout=content.GetComponent<VerticalLayoutGroup>(); layout.padding=new RectOffset(6,6,6,6); layout.spacing=6; layout.childControlHeight=true; layout.childControlWidth=true; layout.childForceExpandHeight=false;
                content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                scroll.content=rect; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=28;
                shop.rowTemplate=Button(rect,font,"Row Template","",0,0,1,1); shop.rowTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight=48; shop.rowTemplate.gameObject.AddComponent<ScrollSelectionIntoView>(); shop.rowTemplate.gameObject.SetActive(false);
                var track=Box(scrollRoot,"Scrollbar",.96f,0,1,1); var bar=track.gameObject.AddComponent<Scrollbar>(); var handle=Box(track,"Handle",0,0,1,1); handle.GetComponent<Image>().color=new Color(.5f,.75f,.7f); bar.handleRect=handle; bar.targetGraphic=handle.GetComponent<Image>(); bar.direction=Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar=bar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
                shop.back=Button(panel,font,"Back","戻る",.06f,.035f,.25f,.13f); shop.minus=Button(panel,font,"Minus","－",.30f,.035f,.43f,.13f); shop.plus=Button(panel,font,"Plus","＋",.47f,.035f,.60f,.13f); shop.confirm=Button(panel,font,"Confirm","購入する",.65f,.035f,.94f,.13f);
                panel.gameObject.SetActive(false); EditorUtility.SetDirty(shop);
            }
            ConvertHouse(town,map);
            EditorSceneManager.MarkSceneDirty(town.gameObject.scene);
        }
        static void ConvertHouse(TownWorld town,GridMap map)
        {
            var room=town.rooms.FirstOrDefault(r=>r && r.roomId=="gardener");
            var building=town.buildings.FirstOrDefault(b=>b && b.roomId=="gardener");
            if(!room || !building) { Debug.LogWarning("道具屋にする民家が未設定です。Setup > Town Authoringを実行してください。"); return; }
            var cell=map.CellAt(room.transform.position)+new Vector3Int(6,7,0);
            Undo.RecordObject(town,"Convert House To Shop"); Undo.RecordObject(map,"Convert House To Shop"); Undo.RecordObject(room,"Convert House To Shop");
            var residents=new List<TownWorld.Resident>(town.residents);
            foreach(var old in residents.Where(r=>r.shop && r.name=="道具屋のミナ").ToArray())
            {
                map.RemoveOccupiedCell(old.cell); residents.Remove(old);
            }
            residents.RemoveAll(r=>r.cell==cell && (r.name=="庭師" || r.name=="道具屋のミナ"));
            var existing=room.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="庭師" || r.name=="道具屋のミナ");
            foreach(var old in town.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="道具屋のミナ" && r!=existing).ToArray()) Undo.DestroyObjectImmediate(old.gameObject);
            GameObject npc;
            if(existing) { npc=existing.gameObject; Undo.RecordObject(npc,"Rename Shopkeeper"); }
            else { npc=new GameObject("道具屋のミナ",typeof(SpriteRenderer)); npc.transform.SetParent(room.transform,false); Undo.RegisterCreatedObjectUndo(npc,"Create Shopkeeper"); }
            npc.name="道具屋のミナ"; Undo.RecordObject(npc.transform,"Place Shopkeeper"); npc.transform.position=map.CenterOf(cell);
            var renderer=npc.GetComponent<SpriteRenderer>(); Undo.RecordObject(renderer,"Shopkeeper Appearance");
            renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/Player_0_1.png"); renderer.color=new Color(1,.85f,.4f); renderer.sortingOrder=1000-Mathf.RoundToInt(npc.transform.position.y*2);
            residents.Add(new TownWorld.Resident{cell=cell,name=npc.name,text="いらっしゃい！",shop=true}); town.residents=residents.ToArray(); map.AddOccupiedCell(cell);
            room.displayName="道具屋"; Undo.RecordObject(room.gameObject,"Rename Shop Room"); room.gameObject.name="Room - 道具屋";
            Undo.RecordObject(building.gameObject,"Rename Shop Building"); building.gameObject.name="道具屋";
            foreach(var sign in building.GetComponentsInChildren<TextMesh>(true)) { Undo.RecordObject(sign,"Shop Sign"); sign.text="道具屋"; EditorUtility.SetDirty(sign); PrefabUtility.RecordPrefabInstancePropertyModifications(sign); }
            PrefabUtility.RecordPrefabInstancePropertyModifications(building.gameObject);
            EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(room); EditorUtility.SetDirty(map); EditorUtility.SetDirty(town);
        }
        static RectTransform Box(Transform parent,string name,float x,float y,float r,float t)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image)); obj.transform.SetParent(parent,false); var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(r,t); rect.offsetMin=rect.offsetMax=Vector2.zero; obj.GetComponent<Image>().color=new Color(.07f,.17f,.22f,.98f); return rect;
        }
        static Text Label(Transform parent,Font font,string name,float x,float y,float r,float t,int size)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false); var text=obj.GetComponent<Text>(); text.font=font; text.fontSize=size; text.color=Color.white; text.raycastTarget=false; text.rectTransform.anchorMin=new Vector2(x,y); text.rectTransform.anchorMax=new Vector2(r,t); text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero; return text;
        }
        static Button Button(Transform parent,Font font,string name,string caption,float x,float y,float r,float t)
        {
            var rect=Box(parent,name,x,y,r,t); rect.GetComponent<Image>().color=new Color(.16f,.32f,.4f); var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>(); var text=Label(rect,font,"Label",0,0,1,1,20); text.text=caption; text.alignment=TextAnchor.MiddleCenter; return button;
        }
    }
}
