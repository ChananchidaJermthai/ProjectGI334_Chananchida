using UnityEngine;

namespace Colonization
{
    [CreateAssetMenu(menuName = "Colonization/Unit")]
    public sealed class UnitData : ScriptableObject
    {
        public string displayName;
        public ArtSprite art;
        public bool naval;
        public bool pioneer;
        public bool colonist;
        public int movement = 3;
        public int strength = 1;
        public int capacity;
    }
}
