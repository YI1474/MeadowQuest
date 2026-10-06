using UnityEngine;

namespace MeadowQuest
{
    [RequireComponent(typeof(KeyboardMoveInput))]
    public sealed class GridMover : MonoBehaviour
    {
        [SerializeField, Min(.1f)]
        float tilesPerSecond = 4;
        [SerializeField]
        GridMap map;
        KeyboardMoveInput input;
        Vector3 target;
        Vector3Int cell;
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        public bool IsMoving { get; private set; }
        public bool MovementLocked { get; set; }
        public float DistanceTravelled { get; private set; }

        public event System.Action<Vector3Int> TileReached;
        public void Configure(GridMap value) => map = value;
        public bool Teleport(Vector3Int destination)
        {
            if (!map || !map.IsWalkable(destination))
            {
                Debug.LogError("移動先が通行できません: " + destination, this);
                return false;
            }

            cell = destination;
            target = map.CenterOf(cell);
            transform.position = target;
            IsMoving = false;
            return true;
        }

        void Start()
        {
            input = GetComponent<KeyboardMoveInput>();
            if (!map)
            {
                Debug.LogError("GridMover requires a GridMap reference.", this);
                enabled = false;
                return;
            }

            cell = map.CellAt(transform.position);
            target = map.CenterOf(cell);
            transform.position = target;
        }

        // LateUpdate consumes this frame's input after all Update methods finish.
        void LateUpdate()
        {
            if (MovementLocked)
                return;
            float remaining = Mathf.Max(.1f, tilesPerSecond) * Mathf.Min(Time.deltaTime, .1f);
            while (remaining > 0 && !MovementLocked)
            {
                if (!IsMoving)
                {
                    var direction = input.Direction;
                    if (direction == Vector2Int.zero)
                        break;
                    Facing = direction;
                    var next = cell + new Vector3Int(direction.x, direction.y, 0);
                    if (!map.IsWalkable(next))
                        break;
                    target = map.CenterOf(next);
                    IsMoving = true;
                }

                float distance = Mathf.Min(remaining, Vector3.Distance(transform.position, target));
                transform.position = Vector3.MoveTowards(transform.position, target, distance);
                DistanceTravelled += distance;
                remaining -= distance;
                if ((transform.position - target).sqrMagnitude < .000001f)
                {
                    transform.position = target;
                    cell = map.CellAt(target);
                    IsMoving = false;
                    TileReached?.Invoke(cell);
                }
            }
        }
    }
}
