using UnityEngine;

namespace MeadowQuest
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(GridMover), typeof(SpriteRenderer))]
    public sealed class WalkSpriteView : MonoBehaviour
    {
        [SerializeField]
        Sprite[] frames = new Sprite[12];
        GridMover mover;
        SpriteRenderer view;
        float previousDistance, cycleDistance;
        static readonly int[] Cycle =
        {
            1,
            0,
            1,
            2
        };
        public void Configure(Sprite[] value) => frames = value;
        void Awake()
        {
            mover = GetComponent<GridMover>();
            view = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            float delta = mover.DistanceTravelled - previousDistance;
            previousDistance = mover.DistanceTravelled;
            cycleDistance = delta > 0 ? cycleDistance + delta : 0;
            int row = mover.Facing.y < 0 ? 0 : mover.Facing.y > 0 ? 2 : mover.Facing.x < 0 ? 3 : 1;
            int frame = delta > 0 ? Cycle[(int)(cycleDistance * 2) % 4] : 1;
            int index = row * 3 + frame;
            if (frames != null && index < frames.Length && frames[index])
                view.sprite = frames[index];
            view.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 2);
        }
    }
}
