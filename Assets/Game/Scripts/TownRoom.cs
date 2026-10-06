using UnityEngine;

namespace MeadowQuest
{
    public sealed class TownRoom : MonoBehaviour
    {
        public string roomId, displayName;
        public Transform arrival, exit;
        public Vector2 size = new Vector2(13, 11);
        public bool Contains(Vector3 point) => point.x >= transform.position.x && point.x < transform.position.x + size.x && point.y >= transform.position.y && point.y < transform.position.y + size.y;
        public Vector3 Center => transform.position + new Vector3(size.x * .5f, size.y * .5f, 0);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Center, new Vector3(size.x, size.y, 0));
        }
    }
}
