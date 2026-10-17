using UnityEngine;

namespace Colonization
{
    [CreateAssetMenu(menuName = "Colonization/Native Nation")]
    public sealed class NativeNationData : ScriptableObject
    {
        public string displayName;
        public string leader;
        public string date;
        [TextArea(3, 8)] public string biography;
        public string region;
        public string[] terrain;
        public string[] goods;
        public string[] jobs;
        public string rival;
        public string scenarioAggression;
        [TextArea] public string contextNote;
        public string[] sources;
        public ArtSprite village;
        public Sprite portrait;
        public Sprite emblem;
        public Sprite profile;
    }
}
