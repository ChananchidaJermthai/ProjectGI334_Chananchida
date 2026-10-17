using UnityEngine;

namespace Colonization
{
    public static class HexRules
    {
        public static Vector3 Position(Vector2Int cell)
        {
            return new Vector3((cell.x + ((cell.y & 1) == 1 ? .5f : 0)) * 2.6f, cell.y * 2.251666f, 0);
        }
        public static Vector2Int Axial(Vector2Int cell) => new Vector2Int(cell.x - (cell.y - (cell.y & 1)) / 2, cell.y);
        public static int Distance(Vector2Int a, Vector2Int b)
        {
            a = Axial(a); b = Axial(b);
            int dx = a.x - b.x, dy = a.y - b.y;
            return (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dx + dy)) / 2;
        }
        public static bool CanEnter(UnitData unit, TerrainKind terrain) => unit.naval == (terrain == TerrainKind.Ocean);
    }
}
