using UnityEngine;

namespace MeadowQuest
{
    // Prefab-local authoring data. No references to objects in another room or scene.
    public sealed class TownBuilding : MonoBehaviour
    {
        public string roomId;
        public Transform entrance, returnPoint;
        public Vector2Int footprintOffset = new Vector2Int(-3, 0);
        public Vector2Int footprintSize = new Vector2Int(6, 4);
        SpriteRenderer facade, roof;
        int previousOrder = int.MinValue;
        void Awake()
        {
            CacheRenderers();
        }

        void OnTransformChildrenChanged()
        {
            CacheRenderers();
        }

        void CacheRenderers()
        {
            facade = GetComponent<SpriteRenderer>();
            var child = transform.Find("Roof - assembled");
            roof = child ? child.GetComponent<SpriteRenderer>() : null;
            previousOrder = int.MinValue;
        }

        void LateUpdate()
        {
            int order = 1000 - Mathf.RoundToInt(transform.position.y * 2);
            if (order == previousOrder)
                return;
            previousOrder = order;
            if (facade)
                facade.sortingOrder = order;
            if (roof)
                roof.sortingOrder = order + 1;
        }

        public bool Blocks(Vector3Int cell)
        {
            var origin = Vector3Int.FloorToInt(transform.position);
            int x = origin.x + footprintOffset.x, y = origin.y + footprintOffset.y;
            return cell.x >= x && cell.x < x + footprintSize.x && cell.y >= y && cell.y < y + footprintSize.y;
        }

        void OnDrawGizmosSelected()
        {
            var origin = Vector3Int.FloorToInt(transform.position);
            Gizmos.color = new Color(1, .3f, .2f, .7f);
            Gizmos.DrawWireCube(new Vector3(origin.x + footprintOffset.x + footprintSize.x / 2f, origin.y + footprintOffset.y + footprintSize.y / 2f, 0), new Vector3(footprintSize.x, footprintSize.y, 0));
            Gizmos.color = Color.green;
            if (entrance)
                Gizmos.DrawWireCube(entrance.position, Vector3.one * .5f);
        }
    }
}
