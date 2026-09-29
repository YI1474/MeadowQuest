using UnityEngine;
using UnityEngine.Tilemaps;

namespace MeadowQuest
{
    public sealed class GridMap : MonoBehaviour
    {
        [SerializeField] Tilemap ground;
        [SerializeField] Tilemap obstacles;
        [SerializeField] Vector3Int[] occupiedCells = new Vector3Int[0];
        [SerializeField] TownBuilding[] buildings=new TownBuilding[0];
        System.Collections.Generic.HashSet<Vector3Int> occupiedLookup;
        void OnValidate() { occupiedLookup=null; }
        public void SetBuildings(TownBuilding[] value) => buildings=value;
        public void RemoveOccupiedCell(Vector3Int cell)
        {
            occupiedCells=System.Array.FindAll(occupiedCells,c=>c!=cell);
            occupiedLookup=null;
        }
        public void AddOccupiedCell(Vector3Int cell)
        {
            var cells=new System.Collections.Generic.List<Vector3Int>(occupiedCells);
            if(!cells.Contains(cell)) cells.Add(cell);
            occupiedCells=cells.ToArray();
            occupiedLookup=null;
        }

        public void Configure(Tilemap floor, Tilemap walls) { ground = floor; obstacles = walls; }
        public Vector3Int CellAt(Vector3 world) => ground.WorldToCell(world);
        public Vector3 CenterOf(Vector3Int cell) => ground.GetCellCenterWorld(cell);
        public bool IsWalkable(Vector3Int cell)
        {
            if(occupiedLookup==null) occupiedLookup=new System.Collections.Generic.HashSet<Vector3Int>(occupiedCells);
            if(!ground || !ground.HasTile(cell) || obstacles && obstacles.HasTile(cell) || occupiedLookup.Contains(cell)) return false;
            foreach(var building in buildings) if(building && building.isActiveAndEnabled && building.Blocks(cell)) return false;
            return true;
        }
    }
}
