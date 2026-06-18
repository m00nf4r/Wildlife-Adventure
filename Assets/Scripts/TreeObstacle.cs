using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Attached to a tree that Wira cannot fly through. It registers a narrow
    /// "trunk" rectangle (much slimmer than the leafy canopy, so the level is
    /// still navigable) with the <see cref="ObstacleRegistry"/>. The
    /// PlayerController checks that registry every frame; touching a trunk ends
    /// the run. A faint danger tint helps the player read which trees are solid.
    /// </summary>
    public class TreeObstacle : MonoBehaviour
    {
        SpriteRenderer sr;

        /// <summary>
        /// Registers the deadly column for this tree. The caller passes the exact
        /// vertical span so the same component works for trees growing UP from the
        /// ground and trees hanging DOWN from the top of the play area.
        /// </summary>
        /// <param name="trunkWidth">World-unit width of the deadly trunk.</param>
        /// <param name="yMin">Bottom of the deadly span (world Y).</param>
        /// <param name="yMax">Top of the deadly span (world Y).</param>
        public void Configure(float trunkWidth, float yMin, float yMax)
        {
            sr = GetComponent<SpriteRenderer>();

            float x = sr != null ? sr.bounds.center.x : transform.position.x;
            float height = Mathf.Max(0.5f, yMax - yMin);

            var trunk = new Rect(x - trunkWidth * 0.5f, yMin, trunkWidth, height);
            ObstacleRegistry.Register(trunk);
        }
    }
}
