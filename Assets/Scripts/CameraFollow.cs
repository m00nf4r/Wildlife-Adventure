using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>Side-scroller camera that follows Wira within the level bounds.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 5f;
        public float minX = 0f, maxX = 40f;
        public float fixedY = 0.5f;

        void LateUpdate()
        {
            if (target == null) return;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            Vector3 goal = new Vector3(x, fixedY, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, goal, smooth * Time.deltaTime);
        }

        public void SetBounds(float minX, float maxX)
        {
            this.minX = minX; this.maxX = maxX;
        }

        public void SnapToPlayer()
        {
            if (target == null) return;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            transform.position = new Vector3(x, fixedY, transform.position.z);
        }
    }
}
