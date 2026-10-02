public interface IInteractable
{
    /// <summary>
    /// Called when the player presses the interaction button while facing this object.
    /// </summary>
    void Interact();

    /// <summary>
    /// Optional prompt text for UI floating popups (e.g., "Talk to Pete", "Pick up Apple").
    /// </summary>
    string GetInteractionPrompt();
}