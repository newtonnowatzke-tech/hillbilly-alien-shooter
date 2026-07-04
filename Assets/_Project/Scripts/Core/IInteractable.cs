using UnityEngine;

namespace HillbillyAlienShooter.Core
{
    /// <summary>
    /// Anything the hillbilly can walk up to and press the Interact key on.
    /// Introduced in Packet 1.2 for horse mounting; the same contract will serve
    /// tech pickups (Packet 2.3), doors, levers, and the alien ship boarding
    /// trigger (Packet 3.2) — the player-side scanner never changes.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short HUD prompt, e.g. "[E] Ride Buttercup".</summary>
        string Prompt { get; }

        /// <summary>Whether this interactor may use us right now.</summary>
        bool CanInteract(GameObject interactor);

        void Interact(GameObject interactor);
    }
}
