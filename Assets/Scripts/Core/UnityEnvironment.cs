using UnityEngine;

/// <summary>
/// Single source of truth for the Unity Services (UGS) environment name.
/// Value is data-driven from Resources/environment_config.json so a build can
/// select it without recompiling. Falls back to "production".
/// The editor build prompt (BuildEnvironmentPrompt) writes the config before a build.
/// </summary>
public static class UnityEnvironment
{
    public const string Default = "production";

    [System.Serializable]
    private class Config { public string environmentName; }

    private static string _cached;

    public static string Name
    {
        get
        {
            if (string.IsNullOrEmpty(_cached))
            {
                var ta = Resources.Load<TextAsset>("environment_config");
                if (ta != null)
                {
                    var cfg = JsonUtility.FromJson<Config>(ta.text);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.environmentName))
                        _cached = cfg.environmentName;
                }
                if (string.IsNullOrEmpty(_cached)) _cached = Default;
            }
            return _cached;
        }
    }

    public static bool IsProduction => Name == Default;
}
