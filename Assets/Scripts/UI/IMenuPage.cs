/// <summary>
/// Contract for any page hosted by <see cref="PageManager"/>. Implementers
/// declare their <see cref="PageType"/>, register with the manager on enable,
/// and receive OnPageEnter/OnPageExit callbacks when the active page changes.
/// </summary>
public interface IMenuPage
{
    PageType PageType { get; }

    /// <summary>Called after the manager finishes transitioning TO this page.</summary>
    void OnPageEnter();

    /// <summary>Called before the manager transitions AWAY from this page.</summary>
    void OnPageExit();
}
