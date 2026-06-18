using System.Collections.Generic;

namespace WildlifeAdventure
{
    /// <summary>
    /// Lightweight registry of every interactable currently in the world.
    /// Avoids per-frame scene scans and physics colliders: the player just
    /// asks this list for the nearest available target.
    /// </summary>
    public static class InteractableRegistry
    {
        public static readonly List<IInteractable> All = new List<IInteractable>();

        public static void Register(IInteractable i)
        {
            if (!All.Contains(i)) All.Add(i);
        }

        public static void Unregister(IInteractable i)
        {
            All.Remove(i);
        }

        public static void Clear()
        {
            All.Clear();
        }
    }
}
