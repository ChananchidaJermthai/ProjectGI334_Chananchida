using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Colonization
{
    public sealed class GameUI : MonoBehaviour
    {
        private GameManager game;
        private Font font;
        private RectTransform root, modal, content;
        private UnityEngine.UI.Text turnText, infoText, messageText, modalTitle;
        private GameObject modalShade;
        private UnityEngine.UI.Image selectedImage;
        private string category = "Land units";
        private int page;
        private readonly Color ink = new Color(.91f,.91f,.84f);
        private readonly Color gold = new Color(.89f,.71f,.37f);
        private readonly Color panel = new Color(.047f,.095f,.115f,.97f);

        public void Initialize(GameManager manager)
        {
            game = manager; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 5000;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = canvas.GetComponent<RectTransform>();
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform);
            RectTransform header = Panel(root,"Header",24,20,1872,80,panel);
            Label(header,"COLONIZATION",24,12,410,36,30,gold);
            Label(header,"NEW WORLD  /  PRODUCTION PROTOTYPE",25,49,580,22,15,ink);
            turnText = Label(header,"",760,19,300,35,25,ink);
            Button(header,"Asset collection",1120,16,210,48,ShowCatalog);
            Button(header,"Native nations",1344,16,210,48,ShowNations);
            Button(header,"Help",1570,16,100,48,ShowWelcome);
            Button(header,"End turn",1684,16,165,48,() => game.EndTurn());

            RectTransform sidebar = Panel(root,"Selection",1490,120,406,780,panel);
            Label(sidebar,"EXPEDITION",24,22,350,36,25,gold);
            selectedImage = Picture(sidebar,null,76,76,250,210);
            infoText = Label(sidebar,"",24,310,358,270,20,ink);
            Button(sidebar,"Next unit  [Tab]",24,592,358,48,() => game.NextUnit());
            Button(sidebar,"Open town [T] / Meet leader",24,652,358,48,() => {
                if(game.SelectedHex != null && game.SelectedHex.Town != null) game.OpenTown(); else game.InspectNative();
            });
            Button(sidebar,"Focus selected",24,712,358,44,() => {
                if(game.SelectedHex != null) Camera.main.GetComponent<CameraController>().Focus(game.SelectedHex.transform.position);
            });

            RectTransform footer = Panel(root,"Orders",24,920,1872,136,panel);
            Button(footer,"Found colony  [B]",20,16,270,46,() => game.FoundTown());
            Button(footer,"Clear forest  [P]",304,16,260,46,() => game.ClearForest());
            Button(footer,"Disembark",578,16,220,46,() => game.Disembark());
            Button(footer,"Recruit / build",812,16,240,46,ShowCatalog);
            Label(footer,"WASD / arrows: pan     Wheel: zoom     Left-click: select     Right-click: move",1072,14,775,50,18,ink);
            messageText = Label(footer,"",22,78,1828,44,21,gold);

            modalShade = new GameObject("Modal Backdrop",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            modalShade.transform.SetParent(root,false); Stretch(modalShade.GetComponent<RectTransform>(),0,0,0,0);
            modalShade.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,.82f);
            modal = Panel(modalShade.transform,"Collection",180,118,1560,780,panel);
            modalTitle = Label(modal,"",28,22,1380,45,32,gold);
            Button(modal,"Close",1420,20,112,48,CloseModal);
            content = Panel(modal,"Content",24,90,1512,664,new Color(0,0,0,0));
            modalShade.SetActive(false);
            game.Changed += Refresh; Refresh();
        }
        private void OnDestroy() { if(game != null) game.Changed -= Refresh; }
        private void Refresh()
        {
            turnText.text = $"TURN {game.Turn:00}   /   {1491 + game.Turn}";
            messageText.text = game.Message;
            Hex hex = game.SelectedHex; Unit unit = game.SelectedUnit;
            Sprite image = null;
            if (hex == null) infoText.text = "Select a discovered tile.";
            else
            {
                string description = $"{hex.Data.kind}{(hex.HasForest ? " / Forest" : "")}\nTile {hex.Cell.x}, {hex.Cell.y}\n";
                if (unit != null)
                {
                    description += $"\n{unit.Data.displayName}\nMovement {unit.MovePoints} / {unit.Data.movement}";
                    if(unit.Data.naval) description += $"\nPassengers {unit.Passengers.Count} / {unit.Data.capacity}";
                    image = unit.Data.art.sprite;
                }
                else if (hex.NativeNation != null)
                { description += "\n" + hex.NativeNation.displayName + "\n" + hex.NativeNation.leader; image = hex.NativeNation.emblem; }
                else image = (hex.HasForest ? hex.Data.forest : hex.Data.terrain).sprite;
                if(hex.Town != null)
                {
                    description += $"\n\n{hex.Town.TownName}\nFood {hex.Town.Food}  |  Lumber {hex.Town.Lumber}";
                }
                infoText.text = description;
            }
            selectedImage.sprite = image; selectedImage.enabled = image != null;
        }
        public void CloseModal() { modalShade.SetActive(false); game.SetModal(false); }
        public void ShowTown(Town town)
        {
            CloseModal(); gameObject.AddComponent<TownView>().Show(game,town,root);
        }
        private void OpenModal(string title)
        {
            foreach(Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            modalTitle.text = title; modalShade.SetActive(true); game.SetModal(true);
        }
        public void ShowWelcome()
        {
            OpenModal("COLONIZATION  /  Your New World");
            Picture(content,game.Catalog.nativeNations[0].village.sprite,30,45,440,420);
            Label(content,"BUILD YOUR FIRST COLONY",520,32,930,52,33,gold);
            Label(content,"Explore a hex world made with your own artwork.\n\n1. Sail west to the coast with your Colonist and Pioneer aboard.\n2. Disembark both passengers, then end the turn.\n3. Use Pioneer [P] to clear forest; Colonist [B] to found a town.\n4. Open town [T] and drag residents onto the six surrounding hexes.\n\nClick a stacked unit again to cycle. Right-click adjacent land\nfrom a loaded ship to disembark. Work completes next turn.",520,110,930,350,23,ink);
            Label(content,"Prototype: colony production and recruitment use simple editable rules.\nNative village art is temporarily reused for player colonies.",520,468,930,66,18,gold);
            Button(content,"Begin expedition",520,562,310,58,CloseModal);
            Button(content,"Browse your artwork",850,562,350,58,ShowCatalog);
        }
        public void ShowCatalog() { category = "Land units"; page = 0; DrawCatalog(); }
        private void DrawCatalog()
        {
            OpenModal("ASSET COLLECTION  /  " + category);
            string[] tabs = {"Land units","Ships","Terrain","Forests"};
            for(int i=0;i<tabs.Length;i++)
            {
                string tab = tabs[i]; Button(content,tab,8+i*240,0,224,44,() => {category=tab;page=0;DrawCatalog();});
            }
            int count = category == "Land units" ? game.Catalog.landUnits.Length : category == "Ships" ? game.Catalog.ships.Length : category == "Terrain" ? game.Catalog.terrains.Length : 7;
            int pages = Mathf.CeilToInt(count / 6f); page = Mathf.Clamp(page,0,pages-1);
            for(int index=page*6;index<Mathf.Min(count,page*6+6);index++)
            {
                int slot=index-page*6; float x=8+slot%3*500, y=66+slot/3*258;
                RectTransform card=Panel(content,"Asset Card",x,y,484,242,new Color(.09f,.16f,.18f));
                UnitData unit = category == "Land units" ? game.Catalog.landUnits[index] : category == "Ships" ? game.Catalog.ships[index] : null;
                Sprite sprite; string title;
                if(unit != null) {sprite=unit.art.sprite;title=unit.displayName;}
                else
                {
                    HexData terrain = category == "Forests" ? game.Catalog.Terrain((TerrainKind)(index+1)) : game.Catalog.terrains[index];
                    sprite = category == "Forests" ? terrain.forest.sprite : terrain.terrain.sprite;
                    title = category == "Forests" ? new[]{"Conifer Forest","Broadleaf Forest","Savanna Woodland","Mixed Forest","Boreal Forest / Taiga","Scrub Forest","Tropical Rainforest"}[index] : terrain.kind.ToString();
                }
                Picture(card,sprite,12,12,190,216);
                Label(card,title,214,25,258,70,23,gold);
                Label(card,unit != null ? $"Movement {unit.movement}\nStrength {unit.strength}"+(unit.naval ? $"\nCapacity {unit.capacity}" : "") : "Imported artwork\nPNG with transparency",214,95,258,76,19,ink);
                if(unit != null)
                {
                    UnitData chosen=unit;
                    Button(card,unit.naval ? "Build / 20 Lumber" : "Recruit / 20 Food",214,184,256,44,() => {game.Recruit(chosen);CloseModal();});
                }
            }
            Button(content,"Previous",8,602,180,46,() => {page--;DrawCatalog();});
            Label(content,$"{page+1} / {pages}    |    {count} assets",215,612,700,30,20,ink);
            Button(content,"Next",1324,602,180,46,() => {page++;DrawCatalog();});
        }
        public void ShowNations() => ShowNation(game.Catalog.nativeNations[0]);
        public void ShowNation(NativeNationData nation)
        {
            OpenModal("NATIVE NATIONS  /  " + nation.displayName);
            Picture(content,nation.portrait,10,10,290,580);
            Picture(content,nation.emblem,320,12,150,90);
            Label(content,nation.displayName,495,10,950,42,32,gold);
            Label(content,nation.leader+"  |  "+nation.date,495,58,950,42,22,ink);
            Label(content,nation.region+"\n\n"+nation.biography+"\n\nPreferred terrain: "+string.Join(", ",nation.terrain)+"\nGoods: "+string.Join(", ",nation.goods)+"\nSpecialists: "+string.Join(", ",nation.jobs)+"\nScenario rival: "+nation.rival+"\n\n"+nation.contextNote,325,125,1155,300,20,ink);
            Label(content,"Artwork is an interpretation; emblems are original game designs.\nThese leaders form a cast from different dates. AI traits are scenario settings.",325,448,880,85,18,gold);
            Picture(content,nation.village.sprite,1220,426,230,160);
            for(int i=0;i<game.Catalog.nativeNations.Length;i++)
            {
                NativeNationData target=game.Catalog.nativeNations[i];
                Button(content,target.displayName,8+i*188,602,178,46,() => ShowNation(target));
            }
            Button(content,"Profile artwork",325,540,240,44,() => ShowProfile(nation));
        }
        private void ShowProfile(NativeNationData nation)
        {
            OpenModal(nation.displayName+" / Original profile");
            Picture(content,nation.profile,12,0,1250,650);
            Button(content,"Back to nation",1284,14,212,48,() => ShowNation(nation));
        }
        private static void Stretch(RectTransform rect,float left,float top,float right,float bottom)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }
        private RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            RectTransform rect=Rect(parent,name,x,y,w,h);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=color.a>0;return rect;
        }
        private UnityEngine.UI.Text Label(Transform parent,string text,float x,float y,float w,float h,int size,Color color)
        {
            RectTransform rect=Rect(parent,"Text",x,y,w,h);var label=rect.gameObject.AddComponent<UnityEngine.UI.Text>();label.font=font;label.text=text;label.fontSize=size;
            label.color=color;label.raycastTarget=false;label.supportRichText=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
        }
        private UnityEngine.UI.Image Picture(Transform parent,Sprite sprite,float x,float y,float w,float h)
        {
            RectTransform rect=Rect(parent,"Artwork",x,y,w,h);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.enabled=sprite!=null;return image;
        }
        private void Button(Transform parent,string text,float x,float y,float w,float h,Action action)
        {
            RectTransform rect=Panel(parent,text,x,y,w,h,new Color(.17f,.27f,.28f));
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();
            ColorBlock colors=button.colors;colors.highlightedColor=new Color(1,.88f,.58f);colors.pressedColor=new Color(.74f,.65f,.44f);button.colors=colors;
            button.onClick.AddListener(() => action());
            UnityEngine.UI.Text label=Label(rect,text,8,0,w-16,h,20,ink);label.alignment=TextAnchor.MiddleCenter;
        }
    }
}
