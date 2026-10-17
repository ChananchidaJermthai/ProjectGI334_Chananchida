using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Colonization.Tests
{
    public sealed class ExpeditionTests
    {
        private GameManager game;
        [UnitySetUp]
        public IEnumerator LoadWorld()
        {
            yield return SceneManager.LoadSceneAsync("Map01");
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game,Is.Not.Null);
            Object.FindAnyObjectByType<GameUI>().CloseModal();
        }
        private void SelectUnit(Unit unit)
        {
            for(int i=0;i<game.Units.Count+1 && game.SelectedUnit!=unit;i++) game.Select(unit.CurrentHex);
            Assert.That(game.SelectedUnit,Is.SameAs(unit));
        }
        [UnityTest]
        public IEnumerator WorldIncludesAllUserArtwork()
        {
            Assert.That(game.Hexes.Count,Is.EqualTo(384));
            Assert.That(game.Catalog.landUnits.Length,Is.EqualTo(20));
            Assert.That(game.Catalog.ships.Length,Is.EqualTo(6));
            Assert.That(game.Catalog.terrains.Count(t=>t.forest!=null&&t.forest.sprite!=null),Is.EqualTo(7));
            Assert.That(game.Hexes.Values.Count(h=>h.NativeNation!=null),Is.EqualTo(8));
            foreach(var data in game.Catalog.nativeNations) Assert.That(data.profile,Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Canvas>(),Is.Not.Null);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator BoardingSailingAndDisembarkingPreservePassenger()
        {
            Unit colonist=game.Units.First(u=>u.Data.colonist);
            Unit ship=game.Units.First(u=>u.Data.naval);
            SelectUnit(colonist);game.MoveSelected(ship.CurrentHex);
            Assert.That(colonist.Carrier,Is.SameAs(ship));
            Assert.That(ship.Passengers,Does.Contain(colonist));
            Assert.That(colonist.gameObject.activeSelf,Is.False);
            SelectUnit(ship);game.Disembark(game.Hexes[new Vector2Int(17,8)]);
            Assert.That(colonist.Carrier,Is.Null);
            Assert.That(ship.Passengers,Is.Empty);
            Assert.That(colonist.gameObject.activeSelf,Is.True);
            Assert.That(colonist.MovePoints,Is.Zero);
            game.EndTurn();Assert.That(colonist.MovePoints,Is.EqualTo(colonist.Data.movement));
            yield return null;
        }
        [UnityTest]
        public IEnumerator ColonyCompletesNextTurnAndCanRecruitWithSupplies()
        {
            Unit colonist=game.Units.First(u=>u.Data.colonist);SelectUnit(colonist);
            game.FoundTown();Assert.That(game.Towns.Count,Is.Zero);
            game.EndTurn();Assert.That(game.Towns.Count,Is.EqualTo(1));
            yield return null;
            Town town=game.Towns[0];Assert.That(town.Food,Is.GreaterThan(0));
            int before=game.Units.Count;
            game.Recruit(game.Catalog.landUnits[1]);Assert.That(game.Units.Count,Is.EqualTo(before));
            while(town.Food<20) game.EndTurn();
            int food=town.Food;game.Recruit(game.Catalog.landUnits[1]);
            Assert.That(game.Units.Count,Is.EqualTo(before+1));
            Assert.That(town.Food,Is.EqualTo(food-20));
            Assert.That(game.Units.Last().Data.displayName,Is.EqualTo("Soldier"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PioneerClearsForestOnNextTurn()
        {
            Unit pioneer=game.Units.First(u=>u.Data.pioneer);
            Hex forest=game.Hexes.Values.First(h=>h.HasForest&&h.NativeNation==null);
            pioneer.Place(forest);forest.Reveal();SelectUnit(pioneer);game.ClearForest();
            Assert.That(forest.HasForest,Is.True);Assert.That(pioneer.MovePoints,Is.Zero);
            game.EndTurn();Assert.That(forest.HasForest,Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator MovementRespectsTerrainAndTurnBudget()
        {
            Unit unit=game.Units.First(u=>u.Data.pioneer);SelectUnit(unit);
            Hex origin=unit.CurrentHex;
            Hex ocean=game.Hexes[new Vector2Int(23,8)];game.MoveSelected(ocean);
            Assert.That(unit.CurrentHex,Is.SameAs(origin));
            Hex target=game.Neighbors(origin).First(h=>h.Data.kind!=TerrainKind.Ocean&&h.NativeNation==null);
            var path=game.FindPath(unit,target);Assert.That(path.Count,Is.EqualTo(1));
            int cost=target.Data.moveCost+(target.HasForest?1:0);
            game.MoveSelected(target);
            Assert.That(unit.MovePoints,Is.EqualTo(Mathf.Max(0,unit.Data.movement-(cost<=unit.Data.movement?cost:0))));
            game.EndTurn();Assert.That(unit.MovePoints,Is.EqualTo(unit.Data.movement));
            yield return null;
        }
        [UnityTest]
        public IEnumerator AllCollectionPagesAndNationProfilesOpenWithoutErrors()
        {
            GameUI view=Object.FindAnyObjectByType<GameUI>();
            view.ShowCatalog();yield return null;Assert.That(game.ModalOpen,Is.True);
            foreach(NativeNationData nation in game.Catalog.nativeNations) {view.ShowNation(nation);yield return null;}
            view.CloseModal();Assert.That(game.ModalOpen,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator RenderGameplayCollectionAndNationPreviews()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Graphics device required for previews.");
            yield return null;
            Capture("01-map");
            GameUI view=Object.FindAnyObjectByType<GameUI>();view.ShowWelcome();yield return null;Capture("02-welcome");
            view.ShowCatalog();yield return null;Capture("03-land-units");
            Click("Ships");yield return null;Capture("04-ships");
            Click("Terrain");yield return null;Capture("05-terrain");
            Click("Forests");yield return null;Capture("06-forests");
            view.ShowNation(game.Catalog.nativeNations[0]);yield return null;Capture("07-native-nation");
            view.CloseModal();
            LogAssert.NoUnexpectedReceived();
        }
        private static void Click(string name)
        {
            var button=Object.FindObjectsByType<UnityEngine.UI.Button>().First(b=>b.name==name);
            button.onClick.Invoke();
        }
        private static void Capture(string name)
        {
            Camera camera=Camera.main;Canvas canvas=Object.FindAnyObjectByType<Canvas>();
            var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            RenderMode mode=canvas.renderMode;Camera previousCamera=canvas.worldCamera;
            float previousScale=canvas.scaleFactor;
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                canvas.scaleFactor=1;
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>()) text.SetAllDirty();
                Canvas.ForceUpdateCanvases();
                var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target};
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                string directory=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),"Logs","Previews");
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;canvas.renderMode=mode;canvas.worldCamera=previousCamera;
                canvas.scaleFactor=previousScale;
                target.Release();Object.Destroy(target);Object.Destroy(texture);
            }
        }
    }
}
