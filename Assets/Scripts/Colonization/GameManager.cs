using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Colonization
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Hex hexPrefab;
        [SerializeField] private Unit unitPrefab;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private GameUI ui;
        private const int Width = 24, Height = 16;
        private readonly Dictionary<Vector2Int, Hex> hexes = new Dictionary<Vector2Int, Hex>();
        private readonly List<Unit> units = new List<Unit>();
        private readonly List<Town> towns = new List<Town>();
        private readonly Dictionary<Unit, bool> orders = new Dictionary<Unit, bool>();
        private readonly Dictionary<Hex, GameObject> villages = new Dictionary<Hex, GameObject>();
        private Transform mapRoot, unitRoot, townRoot;
        private LineRenderer selection;
        private Material worldMaterial;
        public GameCatalog Catalog => catalog;
        public Unit SelectedUnit { get; private set; }
        public Hex SelectedHex { get; private set; }
        public int Turn { get; private set; } = 1;
        public string Message { get; private set; } = "Select a unit, then right-click a tile to move.";
        public bool ModalOpen { get; private set; }
        public IReadOnlyList<Unit> Units => units;
        public IReadOnlyList<Town> Towns => towns;
        public IReadOnlyDictionary<Vector2Int, Hex> Hexes => hexes;
        public event Action Changed;

        public void Configure(GameCatalog data, Hex hex, Unit unit, CameraController camera, GameUI view)
        { catalog = data; hexPrefab = hex; unitPrefab = unit; cameraController = camera; ui = view; }

        private void Start()
        {
            if (catalog == null || hexPrefab == null || unitPrefab == null || ui == null || cameraController == null)
                throw new InvalidOperationException("Open the generated Map01 scene. Colonization references are missing.");
            GenerateWorld();
            ui.Initialize(this);
            Select(units[0].CurrentHex);
            cameraController.Focus(units[0].transform.position);
            ui.ShowWelcome();
        }
        private void OnDestroy()
        {
            if (worldMaterial != null) Destroy(worldMaterial);
        }
        private void GenerateWorld()
        {
            mapRoot = new GameObject("Hex Map").transform;
            unitRoot = new GameObject("Player Units").transform;
            townRoot = new GameObject("Settlements").transform;
            mapRoot.SetParent(transform); unitRoot.SetParent(transform); townRoot.SetParent(transform);
            var random = new System.Random(334);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int coast = 18 + (int)(Math.Sin(y * .7) * 1.5);
                float elevation = Mathf.PerlinNoise(x * .19f + 33.4f, y * .19f + 17.2f);
                float moisture = Mathf.PerlinNoise(x * .16f + 77.1f, y * .16f + 6.5f);
                TerrainKind kind;
                if (x >= coast) kind = TerrainKind.Ocean;
                else if (y == Height - 1) kind = TerrainKind.Arctic;
                else if (y >= Height - 3) kind = TerrainKind.Tundra;
                else if (elevation > .69f) kind = TerrainKind.Mountains;
                else if (elevation > .59f) kind = TerrainKind.Hills;
                else if (moisture < .31f) kind = TerrainKind.Desert;
                else if (moisture > .66f) kind = TerrainKind.Swamp;
                else if (y < 4) kind = TerrainKind.Savanna;
                else if (moisture > .52f) kind = TerrainKind.Grassland;
                else kind = elevation < .45f ? TerrainKind.Prairie : TerrainKind.Plain;
                if (x == 17 && y == 8) kind = TerrainKind.Plain;
                if (x == 16 && y == 8) kind = TerrainKind.Grassland;
                if (x >= 18 && y == 8) kind = TerrainKind.Ocean;
                Hex hex = Instantiate(hexPrefab, mapRoot);
                hex.Initialize(new Vector2Int(x, y), catalog.Terrain(kind), ((x == 17 || x == 16) && y == 8) || random.NextDouble() < .38);
                hexes.Add(hex.Cell, hex);
            }
            for (int i = 0; i < catalog.nativeNations.Length; i++)
            {
                Hex hex = hexes[new Vector2Int(2 + i % 4 * 4, 3 + i / 4 * 8)];
                // Each village is placed on land; its preferred terrain remains editable in its data asset.
                hex.Initialize(hex.Cell, catalog.Terrain(TerrainKind.Prairie), false);
                hex.NativeNation = catalog.nativeNations[i];
                var village = new GameObject(hex.NativeNation.displayName + " Village");
                village.transform.SetParent(townRoot); village.transform.position = hex.transform.position + new Vector3(0, .15f, -1);
                var art = new GameObject("Village Artwork").AddComponent<SpriteRenderer>(); art.transform.SetParent(village.transform, false);
                hex.NativeNation.village.Apply(art, new Vector2(1.65f, 1.65f)); art.sortingOrder = 1000 - hex.Cell.y * 10;
                villages.Add(hex, village); village.SetActive(hex.Explored);
            }
            Unit ship = Spawn(catalog.ships[0], hexes[new Vector2Int(22, 8)]);
            Spawn(catalog.landUnits[0], ship.CurrentHex).Board(ship);
            Spawn(catalog.landUnits[2], ship.CurrentHex).Board(ship);
            worldMaterial = new Material(Shader.Find("Sprites/Default"));
            selection = new GameObject("Selected Hex Outline").AddComponent<LineRenderer>();
            selection.transform.SetParent(transform); selection.sharedMaterial = worldMaterial;
            selection.positionCount = 6; selection.loop = true; selection.widthMultiplier = .055f;
            selection.startColor = selection.endColor = new Color(.96f, .76f, .33f);
            selection.sortingOrder = 3000;
            cameraController.MapSize = new Vector2(Width * 2.6f, Height * 2.251666f);
        }
        private Unit Spawn(UnitData data, Hex hex)
        {
            Unit unit = Instantiate(unitPrefab, unitRoot); unit.Initialize(data, hex); units.Add(unit); Reveal(hex); return unit;
        }
        private void Update()
        {
            if (ModalOpen) return;
            var mouse = Mouse.current; var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) EndTurn();
                if (keyboard.bKey.wasPressedThisFrame) FoundTown();
                if (keyboard.pKey.wasPressedThisFrame) ClearForest();
                if (keyboard.tabKey.wasPressedThisFrame) NextUnit();
                if (keyboard.tKey.wasPressedThisFrame) OpenTown();
            }
            if (mouse == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
            if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame) return;
            Vector3 point = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(new Vector2(point.x, point.y));
            Hex hex = hit == null ? null : hit.GetComponent<Hex>();
            if (hex == null) return;
            if (mouse.leftButton.wasPressedThisFrame) Select(hex); else MoveSelected(hex);
        }
        public void SetModal(bool open) { ModalOpen = open; cameraController.Locked = open; }
        public void OpenTown()
        {
            Town town = SelectedHex == null ? null : SelectedHex.Town;
            if (town == null) { Notify("Select your colony, then choose Open town [T]."); return; }
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Unit unit = units[i];
                if (unit.CurrentHex != town.Hex || unit.Data.naval || unit.Carrier != null || orders.ContainsKey(unit)) continue;
                town.AddResident(unit); units.RemoveAt(i);
                if (SelectedUnit == unit) SelectedUnit = null;
            }
            ui.ShowTown(town);
        }
        public void Notify(string message) { Message = message; Changed?.Invoke(); }
        public void Select(Hex hex)
        {
            if (!hex.Explored) { Notify("Explore this area with a unit first."); return; }
            Unit previous = SelectedUnit; SelectedUnit = null;
            var occupants = units.FindAll(u => u.CurrentHex == hex && u.Carrier == null);
            if (occupants.Count > 0)
            {
                int index = occupants.IndexOf(previous);
                SelectedUnit = occupants[(index + 1) % occupants.Count];
            }
            SelectedHex = hex;
            Vector3 center = hex.transform.position + new Vector3(0, 0, -2);
            for (int i = 0; i < 6; i++)
            {
                float angle = (90 + i * 60) * Mathf.Deg2Rad;
                selection.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * 1.49f, Mathf.Sin(angle) * 1.49f));
            }
            Notify(SelectedUnit != null ? SelectedUnit.Data.displayName + " selected. Right-click to move. Click again to cycle units here." : hex.Data.kind + " selected.");
        }
        public void NextUnit()
        {
            var available = units.FindAll(u => u.Carrier == null);
            if (available.Count == 0) return;
            Unit target = available[(available.IndexOf(SelectedUnit) + 1) % available.Count];
            SelectedUnit = null; Select(target.CurrentHex); SelectedUnit = target;
            cameraController.Focus(target.transform.position); Changed?.Invoke();
        }
        public List<Hex> FindPath(Unit unit, Hex destination)
        {
            var path = new List<Hex>();
            if (unit == null || unit.CurrentHex == null || !HexRules.CanEnter(unit.Data, destination.Data.kind)) return path;
            var frontier = new List<Hex> { unit.CurrentHex };
            var costs = new Dictionary<Hex, int> { [unit.CurrentHex] = 0 };
            var previous = new Dictionary<Hex, Hex>();
            while (frontier.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < frontier.Count; i++) if (costs[frontier[i]] < costs[frontier[best]]) best = i;
                Hex current = frontier[best]; frontier.RemoveAt(best);
                if (current == destination) break;
                foreach (Hex next in Neighbors(current))
                {
                    if (!HexRules.CanEnter(unit.Data, next.Data.kind) || next.NativeNation != null) continue;
                    int cost = costs[current] + next.Data.moveCost + (next.HasForest ? 1 : 0);
                    if (costs.TryGetValue(next, out int known) && known <= cost) continue;
                    costs[next] = cost; previous[next] = current;
                    if (!frontier.Contains(next)) frontier.Add(next);
                }
            }
            if (!costs.ContainsKey(destination)) return path;
            for (Hex h = destination; h != unit.CurrentHex; h = previous[h]) path.Add(h);
            path.Reverse(); return path;
        }
        public void MoveSelected(Hex target)
        {
            Unit unit = SelectedUnit;
            if (unit == null) { Notify("Select one of your units first."); return; }
            if (orders.ContainsKey(unit)) { Notify("This unit is working until the next turn."); return; }
            if (!unit.Data.naval && target.Data.kind == TerrainKind.Ocean && HexRules.Distance(unit.CurrentHex.Cell, target.Cell) <= 1)
            {
                Unit ship = units.Find(u => u.Data.naval && u.CurrentHex == target);
                if (ship != null && unit.Board(ship)) { SelectedUnit = null; Select(target); Notify("Passenger boarded. Select the ship and use Disembark beside land."); return; }
            }
            if (unit.Data.naval && target.Data.kind != TerrainKind.Ocean && HexRules.Distance(unit.CurrentHex.Cell, target.Cell) == 1)
            { Disembark(target); return; }
            List<Hex> path = FindPath(unit, target);
            if (path.Count == 0) { Notify("No route for this unit. Ships sail on ocean; land units travel on land."); return; }
            int moved = 0;
            foreach (Hex step in path)
            {
                int cost = step.Data.moveCost + (step.HasForest ? 1 : 0);
                if (cost > unit.MovePoints) break;
                unit.Place(step); unit.SpendMovement(cost); Reveal(step); moved++;
            }
            if (moved == 0) { Notify("Not enough movement. End the turn to replenish it."); return; }
            Select(unit.CurrentHex); SelectedUnit = unit;
            Notify(unit.CurrentHex == target ? "Destination reached." : "Movement used up. Continue on the next turn.");
        }
        public List<Hex> Neighbors(Hex hex)
        {
            var result = new List<Hex>(6);
            int offset = (hex.Cell.y & 1) == 1 ? 1 : 0;
            Vector2Int[] directions = { new Vector2Int(-1,0),new Vector2Int(1,0),new Vector2Int(offset-1,1),new Vector2Int(offset,1),new Vector2Int(offset-1,-1),new Vector2Int(offset,-1) };
            foreach (Vector2Int direction in directions) if (hexes.TryGetValue(hex.Cell + direction, out Hex next)) result.Add(next);
            return result;
        }
        private void Reveal(Hex origin)
        {
            foreach (Hex hex in hexes.Values)
                if (HexRules.Distance(origin.Cell, hex.Cell) <= 3)
                {
                    hex.Reveal();
                    if (villages.TryGetValue(hex, out GameObject village)) village.SetActive(true);
                }
        }
        public void FoundTown()
        {
            Unit unit = SelectedUnit;
            if (unit == null || !unit.Data.colonist || unit.CurrentHex == null) { Notify("Select a Free Colonist to found a settlement."); return; }
            Hex hex = unit.CurrentHex;
            if (unit.MovePoints == 0 || hex.Town != null || hex.NativeNation != null || hex.Data.kind == TerrainKind.Mountains || orders.ContainsKey(unit))
            { Notify("This unit cannot found a settlement here this turn."); return; }
            foreach (Hex next in Neighbors(hex)) if (next.Town != null || next.NativeNation != null) { Notify("Choose a tile farther from an existing settlement."); return; }
            orders.Add(unit, true); unit.SpendMovement(unit.Data.movement); Notify("Settlement planned. It will be founded at the start of the next turn.");
        }
        public void ClearForest()
        {
            Unit unit = SelectedUnit;
            if (unit == null || !unit.Data.pioneer || unit.CurrentHex == null || !unit.CurrentHex.HasForest || unit.MovePoints == 0 || orders.ContainsKey(unit))
            { Notify("Select a Pioneer on a forest tile with movement remaining."); return; }
            orders.Add(unit, false); unit.SpendMovement(unit.Data.movement); Notify("Clearing forest. Work completes at the start of the next turn.");
        }
        public void Disembark(Hex destination = null)
        {
            Unit ship = SelectedUnit;
            if (ship == null || !ship.Data.naval || ship.Passengers.Count == 0) { Notify("Select a ship carrying passengers."); return; }
            if (ship.MovePoints == 0) { Notify("This ship has no movement remaining this turn."); return; }
            if (destination == null) destination = Neighbors(ship.CurrentHex).Find(h => h.Data.kind != TerrainKind.Ocean && h.NativeNation == null);
            if (destination == null || destination.NativeNation != null || destination.Data.kind == TerrainKind.Ocean || HexRules.Distance(ship.CurrentHex.Cell, destination.Cell) != 1)
            { Notify("Sail beside an unoccupied coastline to disembark."); return; }
            Unit passenger = ship.Passengers[0]; passenger.Disembark(destination); ship.SpendMovement(1); Reveal(destination);
            Select(destination); SelectedUnit = passenger; Notify("Passenger disembarked. Movement refreshes next turn.");
        }
        public void EndTurn()
        {
            if (ModalOpen) return;
            foreach (var order in orders)
            {
                Unit unit = order.Key; Hex hex = unit.CurrentHex;
                if (!order.Value) { hex.ClearForest(); continue; }
                var obj = new GameObject("New Colony " + (towns.Count + 1)); obj.transform.SetParent(townRoot);
                obj.transform.position = hex.transform.position + new Vector3(0, .1f, -1);
                var renderer = new GameObject("Temporary Colony Artwork").AddComponent<SpriteRenderer>(); renderer.transform.SetParent(obj.transform, false);
                catalog.nativeNations[0].village.Apply(renderer, new Vector2(1.6f, 1.6f)); renderer.sortingOrder = 1000 - hex.Cell.y * 10;
                Town town = obj.AddComponent<Town>(); town.Initialize("New Colony " + (towns.Count + 1), hex); town.ConfigureWorkArea(Neighbors(hex)); towns.Add(town);
                town.AddResident(unit); units.Remove(unit); if (SelectedUnit == unit) SelectedUnit = null;
            }
            orders.Clear();
            foreach (Unit unit in units) unit.NewTurn();
            foreach (Town town in towns) { List<Hex> area = Neighbors(town.Hex); area.Add(town.Hex); town.NewTurn(area.ToArray()); }
            Turn++; Notify("Turn " + Turn + ": movement restored, work completed, colony supplies collected.");
        }
        public void InspectNative()
        {
            if (SelectedHex != null && SelectedHex.NativeNation != null) ui.ShowNation(SelectedHex.NativeNation);
            else Notify("Select a discovered Native village to meet its leader.");
        }
        public void Recruit(UnitData data)
        {
            if (towns.Count == 0) { Notify("Found a colony first to recruit units and build ships."); return; }
            Town town = SelectedHex != null && SelectedHex.Town != null ? SelectedHex.Town : towns[0];
            Hex location = town.Hex;
            if (data.naval)
            {
                location = Neighbors(town.Hex).Find(h => h.Data.kind == TerrainKind.Ocean);
                if (location == null) { Notify("Ships require a coastal colony."); return; }
            }
            if (!town.TrySpend(data.naval ? 0 : 20, data.naval ? 20 : 0))
            { Notify(data.naval ? "Build a ship: 20 Lumber required." : "Recruit a unit: 20 Food required."); return; }
            Unit unit = Spawn(data, location); unit.SpendMovement(data.movement);
            Notify(data.displayName + " is ready at " + town.TownName + ". Movement refreshes next turn.");
        }
    }
}
