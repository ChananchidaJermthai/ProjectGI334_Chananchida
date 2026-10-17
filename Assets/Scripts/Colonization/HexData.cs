using UnityEngine;

namespace Colonization
{
    [CreateAssetMenu(menuName = "Colonization/Terrain")]
    public sealed class HexData : ScriptableObject
    {
        public TerrainKind kind;
        public ArtSprite terrain;
        public ArtSprite forest;
        public int moveCost = 1;
        public int foodYield = 2;
    }
}
