using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Colonization
{
    public sealed class TownView : MonoBehaviour
    {
        private GameManager game;
        private Town town;
        private Font font;
        private RectTransform screen, unemployed;
        private UnityEngine.UI.Text summary, instructions;
        private readonly Dictionary<Hex, RectTransform> slots = new Dictionary<Hex, RectTransform>();
        private readonly List<GameObject> icons = new List<GameObject>();
        private bool refreshPending;
        public RectTransform DragLayer => screen;
        public Town CurrentTown => town;
        private static readonly Color Gold = new Color(.94f,.76f,.4f);
        private static readonly Color Ink = new Color(.92f,.94f,.9f);
        public void Show(GameManager manager, Town settlement, RectTransform canvas)
        {
            game=manager; town=settlement; font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen=Panel(canvas,"TownPanel",0,0,1920,1080,new Color(.025f,.07f,.085f));
            screen.SetAsLastSibling(); game.SetModal(true);
            Label(screen,town.TownName.ToUpperInvariant(),38,22,1000,50,36,Gold);
            summary=Label(screen,"",40,82,1500,40,24,Ink);
            Button(screen,"End turn",1515,30,170,52,AdvanceTurn);
            Button(screen,"Exit town",1700,30,175,52,Close);
            Panel(screen,"Colony",24,140,940,565,new Color(.055f,.12f,.14f));
            Label(screen,"COLONY",50,164,850,36,28,Gold);
            Picture(screen,game.Catalog.nativeNations[0].village.sprite,130,220,730,370);
            Label(screen,"Drag residents onto the six surrounding hexes to work.\nReturn them to the lower-left panel to leave them unemployed.",58,610,870,70,23,Ink);

            Panel(screen,"TerrainPanel",990,140,906,565,new Color(.045f,.10f,.12f));
            Label(screen,"SURROUNDING HEXES",1015,155,850,38,26,Gold);
            // Six neighbors surround the city, using the same odd-row hex ordering as the world map.
            Vector2[] positions={new Vector2(1236,362.5f),new Vector2(1556,362.5f),new Vector2(1316,235),new Vector2(1476,235),new Vector2(1316,490),new Vector2(1476,490)};
            for(int i=0;i<town.WorkArea.Count;i++)
            {
                Hex hex=town.WorkArea[i];Vector2 p=positions[i];
                RectTransform slot=Panel(screen,"TerrainSlot_"+i,p.x,p.y,148,170,Color.clear);
                HexPicture(slot,hex.HasForest?hex.Data.forest:hex.Data.terrain,148,170);
                Vector2 labelPosition=i==0?new Vector2(-206,60):i==1?new Vector2(163,60):i<4?new Vector2(-12,-43):new Vector2(-12,174);
                float labelWidth=i<2?190:172;
                var name=Label(slot,hex.Data.kind+(hex.HasForest?" / Forest":""),labelPosition.x,labelPosition.y,labelWidth,24,16,Gold);
                var yield=Label(slot,$"Food {hex.Data.foodYield} / Lumber {(hex.HasForest?2:0)}",labelPosition.x,labelPosition.y+24,labelWidth,22,15,Ink);
                name.alignment=yield.alignment=TextAnchor.MiddleCenter;
                slot.gameObject.AddComponent<TownDropSlot>().Initialize(this,hex);slots.Add(hex,slot);
            }
            Picture(screen,game.Catalog.nativeNations[0].village.sprite,1396,372,148,148);
            Label(screen,"CITY",1424,512,100,24,18,Gold);

            unemployed=Panel(screen,"OutsideSlot_Unemployed",24,730,940,310,new Color(.07f,.16f,.18f));
            unemployed.gameObject.AddComponent<TownDropSlot>().Initialize(this,null);
            Label(unemployed,"UNEMPLOYED / DROP UNITS HERE",24,16,890,35,25,Gold);
            Label(unemployed,"Residents here do not produce resources.",24,59,890,30,19,Ink);
            Panel(screen,"Production",990,730,906,310,new Color(.055f,.12f,.14f));
            Label(screen,"COLONY PRODUCTION",1018,753,850,35,25,Gold);
            instructions=Label(screen,"",1018,809,850,190,23,Ink);
            RefreshWorkers();
        }
        private void Update()
        {
            if(screen != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
        private void LateUpdate() { if(refreshPending) {refreshPending=false;RefreshWorkers();} }
        public bool Assign(Town.Resident resident, Hex hex)
        {
            bool accepted=town.Assign(resident,hex);
            if(accepted)
            {
                game.Notify(resident.Unit.Data.displayName+(hex==null?" is unemployed.":" now works on "+hex.Data.kind+"."));
                refreshPending=true;
            }
            return accepted;
        }
        private void RefreshWorkers()
        {
            foreach(GameObject icon in icons) {icon.SetActive(false);Destroy(icon);} icons.Clear();
            int outside=0;
            foreach(Town.Resident resident in town.Residents)
            {
                RectTransform parent=resident.Workplace==null?unemployed:slots[resident.Workplace];
                float x=resident.Workplace==null?22+(outside%7)*127:31;
                float y=resident.Workplace==null?100+(outside/7)*100:25;
                if(resident.Workplace==null) outside++;
                RectTransform rect=Panel(parent,"UnitDrag_"+resident.Unit.Data.displayName,x,y,resident.Workplace==null?112:86,resident.Workplace==null?100:128,new Color(.025f,.07f,.09f,.8f));
                Picture(rect,resident.Unit.Data.art.sprite,6,4,rect.sizeDelta.x-12,rect.sizeDelta.y-25);
                Label(rect,resident.Unit.Data.displayName,2,rect.sizeDelta.y-24,rect.sizeDelta.x-4,24,13,Ink).alignment=TextAnchor.MiddleCenter;
                rect.gameObject.AddComponent<TownUnitDrag>().Initialize(this,resident);icons.Add(rect.gameObject);
            }
            int working=town.Residents.Count-outside;
            summary.text=$"Year {1491+game.Turn}   |   Turn {game.Turn}   |   Population {town.Residents.Count}   |   Food {town.Food}   |   Lumber {town.Lumber}";
            instructions.text=$"Working residents: {working} / {town.Residents.Count}\nUnemployed residents: {outside}\n\nNext turn: +{town.FoodPerTurn} Food, +{town.LumberPerTurn} Lumber\n\nDrop onto an occupied slot to swap workers.";
        }
        private void AdvanceTurn()
        {
            game.SetModal(false);game.EndTurn();game.SetModal(true);RefreshWorkers();
        }
        public void Close()
        {
            game.SetModal(false);if(screen!=null)Destroy(screen.gameObject);Destroy(this);
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var r=Rect(parent,name,x,y,w,h);r.gameObject.AddComponent<UnityEngine.UI.Image>().color=color;return r;
        }
        private UnityEngine.UI.Text Label(Transform parent,string text,float x,float y,float w,float h,int size,Color color)
        {
            var t=Rect(parent,"Text",x,y,w,h).gameObject.AddComponent<UnityEngine.UI.Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.raycastTarget=false;return t;
        }
        private static void Picture(Transform parent,Sprite sprite,float x,float y,float w,float h)
        {
            var image=Rect(parent,"Artwork",x,y,w,h).gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
        }
        private static void HexPicture(Transform parent,ArtSprite art,float width,float height)
        {
            float fullWidth=art.sprite.rect.width/100,fullHeight=art.sprite.rect.height/100;
            float scaleX=width/art.visibleSize.x,scaleY=height/art.visibleSize.y;
            float left=(fullWidth*.5f+art.visibleCenter.x-art.visibleSize.x*.5f)*scaleX;
            float top=(fullHeight*.5f-art.visibleCenter.y-art.visibleSize.y*.5f)*scaleY;
            RectTransform rect=Rect(parent,"HexArtwork",-left,-top,fullWidth*scaleX,fullHeight*scaleY);
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=art.sprite;image.raycastTarget=false;
        }
        private void Button(Transform parent,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var r=Panel(parent,text,x,y,w,h,new Color(.17f,.28f,.29f));
            var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=r.GetComponent<UnityEngine.UI.Image>();button.onClick.AddListener(action);
            Label(r,text,0,0,w,h,23,Ink).alignment=TextAnchor.MiddleCenter;
        }
    }
}
