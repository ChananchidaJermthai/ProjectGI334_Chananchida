using UnityEngine;

namespace Colonization
{
    public sealed class Town : MonoBehaviour
    {
        public string TownName { get; private set; }
        public Hex Hex { get; private set; }
        public int Food { get; private set; }
        public int Lumber { get; private set; }
        public bool TrySpend(int food, int lumber)
        {
            if (Food < food || Lumber < lumber) return false;
            Food -= food; Lumber -= lumber; return true;
        }
        public void Initialize(string townName, Hex hex) { TownName = townName; Hex = hex; hex.Town = this; }
        public void NewTurn(Hex[] surrounding)
        {
            foreach (Hex hex in surrounding)
            {
                Food += hex.Data.foodYield;
                if (hex.HasForest) Lumber += 2;
            }
        }
    }
}
