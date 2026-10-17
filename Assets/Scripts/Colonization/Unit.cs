using System.Collections.Generic;
using UnityEngine;

namespace Colonization
{
    public sealed class Unit : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer unitRenderer;
        public UnitData Data { get; private set; }
        public Hex CurrentHex { get; private set; }
        public int MovePoints { get; private set; }
        public Unit Carrier { get; private set; }
        public List<Unit> Passengers { get; } = new List<Unit>();
        public void ConfigureRenderer(SpriteRenderer value) => unitRenderer = value;
        public void Initialize(UnitData data, Hex hex)
        {
            Data = data; MovePoints = data.movement; name = data.displayName;
            data.art.Apply(unitRenderer, data.naval ? new Vector2(1.65f, 1.5f) : new Vector2(.85f, 1.1f));
            Place(hex);
        }
        public void Place(Hex hex)
        {
            CurrentHex = hex; transform.position = hex.transform.position + new Vector3(0, .2f, -1);
            unitRenderer.sortingOrder = 2000 - hex.Cell.y * 10;
        }
        public void SpendMovement(int amount) => MovePoints = Mathf.Max(0, MovePoints - amount);
        public void NewTurn() => MovePoints = Data.movement;
        public bool Board(Unit ship)
        {
            if (Data.naval || Carrier != null || !ship.Data.naval || ship.Passengers.Count >= ship.Data.capacity || MovePoints == 0) return false;
            Carrier = ship; ship.Passengers.Add(this); CurrentHex = null; gameObject.SetActive(false); SpendMovement(Data.movement); return true;
        }
        public void Disembark(Hex hex)
        {
            Carrier.Passengers.Remove(this); Carrier = null; gameObject.SetActive(true); Place(hex); SpendMovement(Data.movement);
        }
    }
}
