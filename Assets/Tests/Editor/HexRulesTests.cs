using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Colonization.Tests
{
    public sealed class HexRulesTests
    {
        [TestCase(0,0)] [TestCase(3,7)] [TestCase(12,8)]
        public void HexDistanceMatchesTheSixAdjacentCells(int x,int y)
        {
            var center=new Vector2Int(x,y);int offset=y&1;
            Vector2Int[] neighbors={new Vector2Int(x-1,y),new Vector2Int(x+1,y),new Vector2Int(x+offset-1,y+1),new Vector2Int(x+offset,y+1),new Vector2Int(x+offset-1,y-1),new Vector2Int(x+offset,y-1)};
            Assert.That(HexRules.Distance(center,center),Is.Zero);
            foreach(var next in neighbors)
            {
                Assert.That(HexRules.Distance(center,next),Is.EqualTo(1));
                Assert.That(HexRules.Distance(next,center),Is.EqualTo(1));
                Assert.That(Vector3.Distance(HexRules.Position(center),HexRules.Position(next)),Is.EqualTo(2.6f).Within(.001f));
            }
        }
        [Test]
        public void ImportedCatalogHasCompleteAndDistinctReferences()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Data/Colonization/GameCatalog.asset");
            Assert.That(catalog,Is.Not.Null);Editor.ProjectSetup.ValidateCatalog(catalog);
            var sprites=new System.Collections.Generic.HashSet<Sprite>();
            foreach(var hex in catalog.terrains){sprites.Add(hex.terrain.sprite);if(hex.forest!=null&&hex.forest.sprite!=null)sprites.Add(hex.forest.sprite);}
            foreach(var unit in catalog.landUnits)sprites.Add(unit.art.sprite);
            foreach(var unit in catalog.ships)sprites.Add(unit.art.sprite);
            foreach(var nation in catalog.nativeNations){sprites.Add(nation.portrait);sprites.Add(nation.emblem);sprites.Add(nation.village.sprite);}
            Assert.That(sprites.Count,Is.EqualTo(68));
        }
        [Test]
        public void ShipsAndLandUnitsUseTheirOwnTerrain()
        {
            var unit=ScriptableObject.CreateInstance<UnitData>();
            try
            {
                Assert.That(HexRules.CanEnter(unit,TerrainKind.Ocean),Is.False);
                Assert.That(HexRules.CanEnter(unit,TerrainKind.Hills),Is.True);
                unit.naval=true;
                Assert.That(HexRules.CanEnter(unit,TerrainKind.Ocean),Is.True);
                Assert.That(HexRules.CanEnter(unit,TerrainKind.Plain),Is.False);
            }
            finally {Object.DestroyImmediate(unit);}
        }
    }
}
