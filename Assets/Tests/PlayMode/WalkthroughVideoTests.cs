#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Colonization.Walkthrough
{
    public sealed class WalkthroughVideoTests
    {
        private const int Fps=20, Width=1920, Height=1080;
        private GameManager game;
        private Camera camera;
        private Canvas canvas;
        private RenderTexture renderTarget;
        private Texture2D frameTexture;
        private MediaEncoder encoder;
        private UnityEngine.UI.Text caption;
        private RectTransform pointer;
        private int frameCount;
        private string outputDirectory;
        private readonly List<string> chapters=new List<string>();

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator RecordFullGameplayAndTownDragWalkthrough()
        {
            yield return SceneManager.LoadSceneAsync("Map01");yield return null;
            game=UnityEngine.Object.FindAnyObjectByType<GameManager>();camera=Camera.main;
            Assert.That(game.Hexes.Values.All(h=>h.Explored),Is.True,"Prefab Hex must show every tile.");
            Unit ship=game.Units.Single(u=>u.Data.naval);
            Assert.That(ship.Passengers.Count,Is.EqualTo(2));
            canvas=UnityEngine.Object.FindAnyObjectByType<Canvas>();
            outputDirectory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Deliverables");Directory.CreateDirectory(outputDirectory);
            renderTarget=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);renderTarget.Create();
            frameTexture=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
            camera.targetTexture=renderTarget;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            canvas.GetComponent<CanvasScaler>().enabled=false;canvas.scaleFactor=1;
            BuildRecordingOverlay();
            var attributes=new VideoTrackAttributes {width=Width,height=Height,frameRate=new MediaRational(Fps),includeAlpha=false};
            encoder=new MediaEncoder(Path.Combine(outputDirectory,"Colonization_Gameplay_Walkthrough.mp4"),attributes);
            try
            {
                yield return Hold(4,"01 / Start game - the Hex prefab reveals all 384 tiles.");
                Click("Begin expedition");
                Vector3 initial=camera.transform.position;
                yield return Hold(4,"02 / Full map overview - no hidden hexes.",t=>{
                    camera.transform.position=Vector3.Lerp(initial,new Vector3(30,17,-10),t);camera.orthographicSize=Mathf.Lerp(9,22,t);
                });
                Vector3 overview=camera.transform.position;
                yield return Hold(3,"03 / Caravel starts at sea with a Colonist and Pioneer aboard.",t=>{
                    camera.transform.position=Vector3.Lerp(overview,ship.transform.position+new Vector3(0,0,-9),t);camera.orthographicSize=Mathf.Lerp(22,9,t);
                });
                SelectUnit(ship);
                for(int x=21;x>=18;x--)
                {
                    Hex next=game.Hexes[new Vector2Int(x,8)];MarkWorld(next);game.MoveSelected(next);
                    Assert.That(ship.CurrentHex,Is.SameAs(next));
                    Vector3 start=camera.transform.position,finish=ship.transform.position+new Vector3(0,0,-9);
                    yield return Hold(2,"04 / Sail west toward the coastline.",t=>camera.transform.position=Vector3.Lerp(start,finish,t));
                }
                game.EndTurn();
                yield return Hold(2,"05 / End turn to restore movement before landing.");
                Hex shore=game.Hexes[new Vector2Int(17,8)];
                MarkWorld(shore);SelectUnit(ship);game.Disembark(shore);
                Unit colonist=game.SelectedUnit;
                Assert.That(colonist.Data.colonist,Is.True);
                yield return Hold(3,"06 / Disembark the Free Colonist onto the shore.");
                SelectUnit(ship);MarkWorld(shore);game.Disembark(shore);
                Unit pioneer=game.SelectedUnit;
                Assert.That(pioneer.Data.pioneer,Is.True);Assert.That(ship.Passengers.Count,Is.Zero);
                yield return Hold(3,"07 / Disembark the Hardy Pioneer onto the same forest hex.");
                game.EndTurn();SelectUnit(pioneer);game.ClearForest();
                yield return Hold(3,"08 / Pioneer: Clear forest [P]. The work completes next turn.");
                game.EndTurn();Assert.That(shore.HasForest,Is.False);
                yield return Hold(4,"09 / Forest cleared - the underlying Plain is now visible.");
                SelectUnit(colonist);game.FoundTown();
                yield return Hold(3,"10 / Colonist: Found colony [B]. End the turn to finish construction.");
                game.EndTurn();Assert.That(game.Towns.Count,Is.EqualTo(1));
                yield return Hold(4,"11 / Colony founded. Select it and choose Open town [T].");
                game.Select(shore);game.OpenTown();yield return null;
                Town town=game.Towns[0];Assert.That(town.Residents.Count,Is.EqualTo(2));
                MovePointer(new Vector2(155,210));
                yield return Hold(5,"12 / Town view: six work hexes at upper-right; unemployed residents at lower-left.");
                Town.Resident first=town.Residents[0],second=town.Residents[1];
                yield return Drag(first,town.WorkArea[0],"13 / Drag the Colonist from Unemployed to work hex 1.");
                yield return Hold(3,"Assigned: the icon and production preview update immediately.");
                yield return Drag(second,town.WorkArea[1],"14 / Drag the Pioneer to work hex 2.");
                yield return Hold(3,"Both residents now work outside the city.");
                for(int i=2;i<6;i++)
                {
                    yield return Drag(first,town.WorkArea[i],$"15 / Move the Colonist to work hex {i+1} of 6.");
                    yield return Hold(2,"Workers can move between all six surrounding hexes.");
                }
                yield return Drag(first,null,"16 / Drag the Colonist back to the lower-left Unemployed area.");
                yield return Hold(3,"The resident is now unemployed and contributes no tile production.");
                yield return Drag(second,null,"17 / Return the Pioneer to Unemployed as well.");
                yield return Hold(3,"Both icons remain available in the unemployment area.");
                Hex forest=town.WorkArea.First(h=>h.HasForest);
                yield return Drag(first,forest,"18 / Assign a resident to a forest hex to collect Food and Lumber.");
                int food=town.Food,lumber=town.Lumber,foodYield=town.FoodPerTurn,lumberYield=town.LumberPerTurn;
                yield return Hold(3,"The Production panel previews the next turn's output.");
                Click("End turn","TownPanel");
                Assert.That(town.Food,Is.EqualTo(food+foodYield));Assert.That(town.Lumber,Is.EqualTo(lumber+lumberYield));
                yield return Hold(5,"19 / End turn: the resource totals increase by the displayed production.");
                SaveFrame("Town_Workers_Final.png");
                Click("Exit town");yield return null;
                yield return Hold(4,"20 / Return to the world map. Landing, forest clearing, colony, and drag-and-drop complete.");
                SaveFrame("Walkthrough_Map_Final.png");
                File.WriteAllLines(Path.Combine(outputDirectory,"Walkthrough_Chapters.txt"),chapters);
                string completed=$"WALKTHROUGH_RECORDED: {frameCount} frames / {Fps} fps / {(float)frameCount/Fps:F1} seconds";
                LogAssert.Expect(LogType.Log,completed);Debug.Log(completed);
            }
            finally
            {
                encoder?.Dispose();encoder=null;camera.targetTexture=null;RenderTexture.active=null;
                renderTarget.Release();UnityEngine.Object.Destroy(renderTarget);UnityEngine.Object.Destroy(frameTexture);
            }
            Assert.That(new FileInfo(Path.Combine(outputDirectory,"Colonization_Gameplay_Walkthrough.mp4")).Length,Is.GreaterThan(100000));
            LogAssert.NoUnexpectedReceived();
        }
        private IEnumerator Hold(float seconds,string text,Action<float> update=null)
        {
            caption.text="AUTOMATED GAMEPLAY  |  "+text;
            chapters.Add(TimeSpan.FromSeconds((float)frameCount/Fps).ToString(@"mm\:ss")+"  "+text);
            int frames=Mathf.RoundToInt(seconds*Fps);
            for(int i=0;i<frames;i++) {update?.Invoke(Mathf.SmoothStep(0,1,(i+1f)/frames));CaptureFrame();yield return null;}
        }
        private IEnumerator Drag(Town.Resident resident,Hex hex,string text)
        {
            caption.text="AUTOMATED GAMEPLAY  |  "+text;
            chapters.Add(TimeSpan.FromSeconds((float)frameCount/Fps).ToString(@"mm\:ss")+"  "+text);
            TownDropSlot target=UnityEngine.Object.FindObjectsByType<TownDropSlot>().Single(s=>s.Hex==hex);
            yield return Tests.TownDragDriver.Drag(resident,target,Fps*2,MovePointer,CaptureFrame);
        }
        private void SelectUnit(Unit unit)
        {
            for(int i=0;i<game.Units.Count+1 && game.SelectedUnit!=unit;i++) game.Select(unit.CurrentHex);
            Assert.That(game.SelectedUnit,Is.SameAs(unit));
        }
        private void MarkWorld(Hex hex) => MovePointer(camera.WorldToScreenPoint(hex.transform.position));
        private void MovePointer(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)canvas.transform,screenPosition,camera,out Vector3 position);
            pointer.position=position;
        }
        private static void Click(string name,string parent=null)
        {
            var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==name&&(parent==null||b.transform.parent.name==parent));
            button.onClick.Invoke();
        }
        private void CaptureFrame()
        {
            caption.transform.parent.SetAsLastSibling();pointer.SetAsLastSibling();
            foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>()) text.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=renderTarget};
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=renderTarget;
            frameTexture.ReadPixels(new Rect(0,0,Width,Height),0,0);frameTexture.Apply();
            Assert.That(encoder.AddFrame(frameTexture),Is.True,"Video encoder rejected frame "+frameCount);
            frameCount++;
        }
        private void SaveFrame(string name) => File.WriteAllBytes(Path.Combine(outputDirectory,name),frameTexture.EncodeToPNG());
        private void BuildRecordingOverlay()
        {
            RectTransform line=Rect("Recording captions",(RectTransform)canvas.transform,28,1042,1864,36);
            line.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.015f,.04f,.055f,.97f);
            RectTransform textRect=Rect("Caption",line,10,0,1844,36);
            caption=textRect.gameObject.AddComponent<UnityEngine.UI.Text>();caption.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.fontSize=20;caption.color=new Color(1,.84f,.5f);caption.alignment=TextAnchor.MiddleLeft;caption.raycastTarget=false;
            line.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            pointer=Rect("Recorded pointer",(RectTransform)canvas.transform,1080,500,20,20);pointer.pivot=new Vector2(.5f,.5f);
            var image=pointer.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(1,.82f,.2f);image.raycastTarget=false;
            pointer.localRotation=Quaternion.Euler(0,0,45);
        }
        private static RectTransform Rect(string name,RectTransform parent,float x,float y,float width,float height)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;
        }
    }
}
#endif
