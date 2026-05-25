using UnityEngine;

public enum NotificationKind
{
    Reward,
    PowerupSelected,
    LevelComplete,
    Generic,
}

public struct NotificationRequest
{
    public NotificationKind kind;
    public string headline;
    public string description;
    public Sprite icon;
    public int amount;
    public string currency;
    public Color bgTint;
    public Color fgTint;
    public bool hasTint;
    public float holdOverride;
    public PowerUpIndicator indicatorPrefabOverride;
    public object payload;
}

public interface INotificationIndicator
{
    void Bind(NotificationRequest request);
    void Hide();
}

public interface ICooldownIndicator : INotificationIndicator
{
    void StartCooldown(float duration);
}
