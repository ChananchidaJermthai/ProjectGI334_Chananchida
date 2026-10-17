using UnityEngine;

namespace Colonization
{
    public sealed class Hex : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer terrainRenderer;
        public Vector2Int Cell { get; private set; }
        public HexData Data { get; private set; }
        public bool HasForest { get; private set; }
        public bool Explored { get; private set; }
        public NativeNationData NativeNation { get; set; }
        public Town Town { get; set; }
        public void ConfigureRenderer(SpriteRenderer value) => terrainRenderer = value;
        public void Initialize(Vector2Int cell, HexData data, bool forest)
        {
            Cell = cell; Data = data; HasForest = forest && data.forest != null && data.forest.sprite != null;
            transform.position = HexRules.Position(cell);
            name = $"Hex {cell.x},{cell.y} {data.kind}";
            RefreshArt();
        }
        private void RefreshArt()
        {
            (HasForest ? Data.forest : Data.terrain).Apply(terrainRenderer, new Vector2(2.64f, 3.05f));
            terrainRenderer.sortingOrder = -Cell.y * 10;
            terrainRenderer.color = Explored ? Color.white : new Color(.20f, .25f, .31f);
        }
        public void Reveal() { Explored = true; RefreshArt(); }
        public void ClearForest() { HasForest = false; RefreshArt(); }
    }
}
