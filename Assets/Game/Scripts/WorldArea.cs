using UnityEngine;
namespace MeadowQuest
{
    public sealed class WorldArea : MonoBehaviour
    {
        public string displayName;
        public Vector2 size;
        public string recoveryRoom="";
        public Vector3Int recoveryCell;
        public bool Contains(Vector3 p) => p.x>=transform.position.x && p.y>=transform.position.y && p.x<transform.position.x+size.x && p.y<transform.position.y+size.y;
    }
}
