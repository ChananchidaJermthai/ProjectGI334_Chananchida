using System;
using UnityEngine;

namespace Colonization
{
    [Serializable]
    public sealed class ArtSprite
    {
        public Sprite sprite;
        public Vector2 visibleCenter;
        public Vector2 visibleSize = Vector2.one;

        public void Apply(SpriteRenderer renderer, Vector2 size)
        {
            renderer.sprite = sprite;
            Vector2 scale = new Vector2(size.x / Mathf.Max(.01f, visibleSize.x), size.y / Mathf.Max(.01f, visibleSize.y));
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1);
            renderer.transform.localPosition = new Vector3(-visibleCenter.x * scale.x, -visibleCenter.y * scale.y, 0);
        }
    }
    public enum TerrainKind { Ocean, Grassland, Prairie, Savanna, Plain, Tundra, Desert, Swamp, Arctic, Hills, Mountains }
}
