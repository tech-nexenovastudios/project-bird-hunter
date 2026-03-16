

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Gameplay.PowerUps;

// ════════════════════════════════════════════════════════════════════════════
// 1. Custom Inspector on PowerupCardController
//    Per-card Lock / Unlock / Toggle + live state badge + config gate info.
// ════════════════════════════════════════════════════════════════════════════
[CustomEditor(typeof(PowerupCardController))]
public class PowerupCardControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var card = (PowerupCardController)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test.", MessageType.Info);
            return;
        }

        var mgr = PowerupLockManager.Instance;
        if (mgr == null)
        {
            EditorGUILayout.HelpBox("PowerupLockManager not found in scene.", MessageType.Warning);
            return;
        }

        bool unlocked = mgr.IsUnlocked(card.PowerupId);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("── Card Test ──", EditorStyles.boldLabel);

        // State badge
        var badgeStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        var prev = GUI.color;
        GUI.color = unlocked ? new Color(0.25f, 0.90f, 0.45f) : new Color(0.90f, 0.35f, 0.35f);
        EditorGUILayout.LabelField(unlocked ? "🔓  UNLOCKED" : "🔒  LOCKED", badgeStyle, GUILayout.Height(26));
        GUI.color = prev;

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("🔒  Lock")) { card.TestLock(); EditorUtility.SetDirty(card); }
            if (GUILayout.Button("🔓  Unlock")) { card.TestUnlock(); EditorUtility.SetDirty(card); }
            if (GUILayout.Button("⇄  Toggle")) { card.TestToggle(); EditorUtility.SetDirty(card); }
        }

        // Config gate info
        var db = mgr.GetDatabase();
        var cfg = db?.GetPowerupById(card.PowerupId);
        if (cfg != null)
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Config Gate", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("ID", cfg.id);
                EditorGUILayout.LabelField("Rarity", cfg.rarity.ToString());
                EditorGUILayout.LabelField("Initially Unlocked", cfg.initiallyUnlocked.ToString());
                EditorGUILayout.LabelField("Unlock From Chapter", cfg.unlockFromChapter.ToString());
                EditorGUILayout.LabelField("Spin Unlock Level", cfg.spinUnlockLevel.ToString());
                EditorGUILayout.LabelField("Reset Rule", cfg.resetRule);
            }
        }

        Repaint();
    }
}

// ════════════════════════════════════════════════════════════════════════════
// 2. Standalone Editor Window  →  Window / PowerUP Lock Tester
//    Full card list + chapter simulator + search filter.
// ════════════════════════════════════════════════════════════════════════════
public class PowerupLockTesterWindow : EditorWindow
{
    // ── Chapter simulator ─────────────────────────────────────────────────
    private int _simChapter = 1;
    private int _simChapterLevel = 0;

    // ── Filter ────────────────────────────────────────────────────────────
    private Vector2 _scroll;
    private string _search = "";
    private int _rarityFilter = 0;   // 0 = All
    private bool _showLocked = true;
    private bool _showUnlocked = true;

    private static readonly string[] RarityOptions = { "All", "Common", "Rare", "Epic", "Legendary" };

    [MenuItem("Window/PowerUP Lock Tester")]
    public static void Open() => GetWindow<PowerupLockTesterWindow>("PowerUP Lock Tester");

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test lock states.", MessageType.Info);
            return;
        }

        var mgr = PowerupLockManager.Instance;
        if (mgr == null)
        {
            EditorGUILayout.HelpBox("PowerupLockManager not found in scene.", MessageType.Warning);
            return;
        }

        var db = mgr.GetDatabase();
        if (db == null)
        {
            EditorGUILayout.HelpBox(
                "Database not loaded — place PowerupDatabase.asset inside a Resources/ folder.",
                MessageType.Warning);
            return;
        }

        DrawGlobalControls(mgr, db);
        EditorGUILayout.Space(4);
        DrawChapterSimulator(mgr);
        EditorGUILayout.Space(4);
        DrawFilters();
        EditorGUILayout.Space(4);
        DrawCardList(mgr, db);

        Repaint();
    }

    // ── Sections ─────────────────────────────────────────────────────────────

    private void DrawGlobalControls(PowerupLockManager mgr, PowerupDatabase db)
    {
        EditorGUILayout.LabelField("── Global ──", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("🔓  Unlock ALL"))
                foreach (var c in db.allPowerups) mgr.UnlockPowerup(c.id);

            if (GUILayout.Button("🔒  Lock ALL"))
                foreach (var c in db.allPowerups) mgr.LockPowerup(c.id);

            if (GUILayout.Button("🔄  Chapter Reset"))
                mgr.ResetForNewChapter();

            if (GUILayout.Button("↺  Refresh"))
                mgr.RefreshAllCards();
        }
    }

    private void DrawChapterSimulator(PowerupLockManager mgr)
    {
        EditorGUILayout.LabelField("Chapter Gate Simulator", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Chapter", GUILayout.Width(70));
                _simChapter = EditorGUILayout.IntField(_simChapter, GUILayout.Width(40));
                EditorGUILayout.LabelField("Chapter Level", GUILayout.Width(90));
                _simChapterLevel = EditorGUILayout.IntField(_simChapterLevel, GUILayout.Width(40));
            }

            string resetNote = _simChapterLevel == 0
                ? "Level 0 will RESET gated cards first, then unlock eligible ones."
                : "Unlocks cards where unlockFromChapter <= " + _simChapter + " AND spinUnlockLevel <= " + _simChapterLevel;
            EditorGUILayout.HelpBox(resetNote, MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Simulate Level Complete"))
                    mgr.OnLevelCompleted(_simChapter, _simChapterLevel);

                EditorGUILayout.LabelField("Quick:", GUILayout.Width(38));
                if (GUILayout.Button("L0", GUILayout.Width(28))) { _simChapterLevel = 0; mgr.OnLevelCompleted(_simChapter, 0); }
                if (GUILayout.Button("L6", GUILayout.Width(28))) { _simChapterLevel = 6; mgr.OnLevelCompleted(_simChapter, 6); }
                if (GUILayout.Button("L11", GUILayout.Width(32))) { _simChapterLevel = 11; mgr.OnLevelCompleted(_simChapter, 11); }
                if (GUILayout.Button("L16", GUILayout.Width(32))) { _simChapterLevel = 16; mgr.OnLevelCompleted(_simChapter, 16); }
            }
        }
    }

    private void DrawFilters()
    {
        EditorGUILayout.LabelField("── Filters ──", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Search", GUILayout.Width(46));
            _search = EditorGUILayout.TextField(_search).ToLower();
            if (GUILayout.Button("✕", GUILayout.Width(22))) _search = "";
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Rarity", GUILayout.Width(46));
            _rarityFilter = GUILayout.Toolbar(_rarityFilter, RarityOptions);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            _showLocked = EditorGUILayout.ToggleLeft("Show Locked", _showLocked, GUILayout.Width(110));
            _showUnlocked = EditorGUILayout.ToggleLeft("Show Unlocked", _showUnlocked, GUILayout.Width(110));
        }
    }

    private void DrawCardList(PowerupLockManager mgr, PowerupDatabase db)
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        PowerupRarity? lastRarity = null;

        foreach (var cfg in db.allPowerups)
        {
            bool unlocked = mgr.IsUnlocked(cfg.id);

            // ── Filters ───────────────────────────────────────────────────
            if (!_showLocked && !unlocked) continue;
            if (!_showUnlocked && unlocked) continue;

            if (_rarityFilter != 0 && (int)cfg.rarity != _rarityFilter - 1) continue;

            if (!string.IsNullOrEmpty(_search) &&
                !cfg.displayName.ToLower().Contains(_search) &&
                !cfg.id.ToLower().Contains(_search) &&
                !cfg.rarity.ToString().ToLower().Contains(_search))
                continue;

            // ── Rarity group header ───────────────────────────────────────
            if (cfg.rarity != lastRarity)
            {
                lastRarity = cfg.rarity;
                EditorGUILayout.Space(4);
                var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                var ph = GUI.color;
                GUI.color = GuiRarityColor(cfg.rarity);
                EditorGUILayout.LabelField($"── {cfg.rarity} ──", headerStyle);
                GUI.color = ph;
            }

            // ── Card row ──────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Rarity swatch
                    var p = GUI.color;
                    GUI.color = GuiRarityColor(cfg.rarity);
                    GUILayout.Label("■", GUILayout.Width(14));
                    GUI.color = p;

                    // Name + ID
                    using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(150)))
                    {
                        EditorGUILayout.LabelField(cfg.displayName, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(cfg.id, EditorStyles.miniLabel);
                    }

                    GUILayout.FlexibleSpace();

                    // State badge
                    var badge = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
                    badge.normal.textColor = unlocked
                        ? new Color(0.25f, 0.85f, 0.40f)
                        : new Color(0.85f, 0.35f, 0.35f);
                    GUILayout.Label(unlocked ? "🔓 UNLOCKED" : "🔒 LOCKED", badge, GUILayout.Width(92));

                    // Per-card button
                    if (unlocked)
                    { if (GUILayout.Button("Lock", GUILayout.Width(48))) mgr.LockPowerup(cfg.id); }
                    else
                    { if (GUILayout.Button("Unlock", GUILayout.Width(56))) mgr.UnlockPowerup(cfg.id); }
                }

                // Gate info row (small, always visible)
                using (new EditorGUILayout.HorizontalScope())
                {
                    var mini = EditorStyles.miniLabel;
                    bool initUnlocked = cfg.initiallyUnlocked;

                    GUILayout.Label(initUnlocked
                        ? "  ✔ Initially Unlocked"
                        : $"  Chapter ≥ {cfg.unlockFromChapter}  |  Level ≥ {cfg.spinUnlockLevel}  |  {cfg.resetRule}",
                        mini);

                    // Highlight if the simulator meets this gate
                    bool gateMet = mgr.MeetsChapterGate(cfg, _simChapter, _simChapterLevel);
                    if (!initUnlocked)
                    {
                        var gateStyle = new GUIStyle(mini) { fontStyle = FontStyle.Bold };
                        gateStyle.normal.textColor = gateMet
                            ? new Color(0.3f, 0.85f, 0.4f)
                            : new Color(0.7f, 0.7f, 0.7f);
                        GUILayout.Label(gateMet ? "✔ Gate Met" : "✘ Gate Locked", gateStyle, GUILayout.Width(90));
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ── Utility ──────────────────────────────────────────────────────────────

    private static Color GuiRarityColor(PowerupRarity r) => r switch
    {
        PowerupRarity.Common => new Color(0.60f, 0.95f, 0.69f),
        PowerupRarity.Rare => new Color(0.97f, 0.90f, 0.29f),
        PowerupRarity.Epic => new Color(0.92f, 0.70f, 1.00f),
        PowerupRarity.Legendary => new Color(1.00f, 0.61f, 0.58f),
        _ => Color.white
    };
}
#endif
