using UnityEngine;

namespace Colonization
{
    [CreateAssetMenu(menuName = "Colonization/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public HexData[] terrains;
        public UnitData[] landUnits;
        public UnitData[] ships;
        public NativeNationData[] nativeNations;
        public HexData Terrain(TerrainKind kind)
        {
            foreach (HexData data in terrains) if (data.kind == kind) return data;
            throw new System.InvalidOperationException("Missing terrain: " + kind);
        }
    }
}
