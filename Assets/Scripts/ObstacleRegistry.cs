using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Lightweight registry of every solid obstacle (tree trunk) currently in
    /// the world. Mirrors <see cref="InteractableRegistry"/> so the player can
    /// test for a deadly collision each frame without physics colliders.
    /// </summary>
    public static class ObstacleRegistry
    {
        public static readonly List<Rect> Trunks = new List<Rect>();

        public static void Register(Rect trunk)
        {
            Trunks.Add(trunk);
        }

        public static void Clear()
        {
            Trunks.Clear();
        }

        /// <summary>True if the given world point lies inside any solid trunk.</summary>
        public static bool Hits(Vector2 point)
        {
            for (int i = 0; i < Trunks.Count; i++)
                if (Trunks[i].Contains(point)) return true;
            return false;
        }
    }
}
