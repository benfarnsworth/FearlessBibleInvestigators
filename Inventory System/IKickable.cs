using UnityEngine;

/// <summary>
/// Attach to any script (crates, doors, switches, hazard barrels) 
/// that needs custom logic when kicked by the player.
/// </summary>
public interface IKickable
{
    /// <summary>
    /// Triggered when the player's kick connects with this collider.
    /// </summary>
    /// <param name="kickDirection">Normalized direction vector of the kick.</param>
    /// <param name="chargeRatio">Charge level from 0.0 (quick tap) to 1.0 (fully charged).</param>
    void OnKicked(Vector2 kickDirection, float chargeRatio);
}