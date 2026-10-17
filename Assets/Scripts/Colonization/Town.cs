using System.Collections.Generic;
using UnityEngine;

namespace Colonization
{
    public sealed class Town : MonoBehaviour
    {
        public string TownName { get; private set; }
        public Hex Hex { get; private set; }
        public int Food { get; private set; }
        public int Lumber { get; private set; }
        public sealed class Resident
        {
            public Unit Unit { get; }
            public Hex Workplace { get; internal set; }
            public Resident(Unit unit) { Unit = unit; }
        }
        private readonly List<Resident> residents = new List<Resident>();
        private readonly List<Hex> workArea = new List<Hex>();
        public IReadOnlyList<Resident> Residents => residents;
        public IReadOnlyList<Hex> WorkArea => workArea;
        public void ConfigureWorkArea(IEnumerable<Hex> hexes) { workArea.Clear(); workArea.AddRange(hexes); }
        public void AddResident(Unit unit)
        {
            if (unit == null || unit.Data.naval || unit.Carrier != null || residents.Exists(r => r.Unit == unit)) return;
            residents.Add(new Resident(unit)); unit.Place(Hex); unit.gameObject.SetActive(false);
        }
        public bool Assign(Resident resident, Hex workplace)
        {
            if (!residents.Contains(resident) || (workplace != null && (!workArea.Contains(workplace) || workplace.NativeNation != null || workplace.Town != null))) return false;
            if (resident.Workplace == workplace) return true;
            Resident occupant = workplace == null ? null : residents.Find(r => r.Workplace == workplace);
            if (occupant != null) occupant.Workplace = resident.Workplace;
            resident.Workplace = workplace; return true;
        }
        public int FoodPerTurn
        {
            get
            {
                int result = Hex.Data.foodYield;
                foreach (Resident resident in residents)
                    if (resident.Workplace != null) result += resident.Workplace.Data.foodYield * (resident.Unit.Data.displayName == "Farmer" || resident.Unit.Data.displayName == "Fisherman" ? 2 : 1);
                return result;
            }
        }
        public int LumberPerTurn
        {
            get
            {
                int result = 0;
                foreach (Resident resident in residents)
                    if (resident.Workplace != null && resident.Workplace.HasForest) result += resident.Unit.Data.displayName == "Lumberjack" ? 4 : 2;
                return result;
            }
        }
        public bool TrySpend(int food, int lumber)
        {
            if (Food < food || Lumber < lumber) return false;
            Food -= food; Lumber -= lumber; return true;
        }
        public void Initialize(string townName, Hex hex) { TownName = townName; Hex = hex; hex.Town = this; }
        public void NewTurn(Hex[] surrounding)
        {
            Food += FoodPerTurn;
            Lumber += LumberPerTurn;
        }
    }
}
