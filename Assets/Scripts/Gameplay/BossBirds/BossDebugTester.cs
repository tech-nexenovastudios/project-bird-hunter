//// BossDebugTester.cs
//using UnityEngine;
//using System.Collections.Generic;

//public class BossDebugTester : MonoBehaviour
//{
//    [Header("Setup")]
//    [Tooltip("The single boss prefab with Controller, HealthHandler, MovementHandler")]
//    [SerializeField] private GameObject bossPrefab;

//    [Tooltip("Drag all 13 BossBirdConfig assets here")]
//    [SerializeField] private List<BossBirdConfig> allBossConfigs;

//    [Header("Test Settings")]
//    [SerializeField] private bool spawnAsLevel20 = false;
//    [SerializeField] private Vector3 spawnPosition = new(0f, 3f, 0f);

//    [Header("Debug Damage")]
//    [SerializeField] private float debugDamageAmount = 50f;

//    // Runtime state
//    private int currentConfigIndex;
//    private GameObject activeBoss;
//    private BossBirdController activeController;
//    private BossHealthHandler activeHealth;

//    // GUI
//    private Vector2 scrollPos;
//    private bool showAttackDetails;
//    private readonly List<string> logMessages = new();
//    private const int MAX_LOG = 20;

//    private void OnEnable()
//    {
//        var bus = BossEventBus.Instance;
//        if (bus != null)
//        {
//            bus.OnBossSpawned += LogSpawned;
//            bus.OnBossDefeated += LogDefeated;
//            bus.OnHealthChanged += LogHealth;
//            bus.OnEnraged += LogEnraged;
//        }
//    }

//    private void OnDisable()
//    {
//        var bus = BossEventBus.Instance;
//        if (bus != null)
//        {
//            bus.OnBossSpawned -= LogSpawned;
//            bus.OnBossDefeated -= LogDefeated;
//            bus.OnHealthChanged -= LogHealth;
//            bus.OnEnraged -= LogEnraged;
//        }
//    }

//    private void Update()
//    {
//        HandleKeyboardShortcuts();
//    }

//    private void HandleKeyboardShortcuts()
//    {
//        if (Input.GetKeyDown(KeyCode.S)) SpawnCurrentBoss();
//        if (Input.GetKeyDown(KeyCode.K)) KillActiveBoss();
//        if (Input.GetKeyDown(KeyCode.D)) DealDebugDamage();
//        if (Input.GetKeyDown(KeyCode.RightArrow)) CycleConfig(1);
//        if (Input.GetKeyDown(KeyCode.LeftArrow)) CycleConfig(-1);
//        if (Input.GetKeyDown(KeyCode.P)) spawnAsLevel20 = !spawnAsLevel20;
//    }

//    // ── Spawn / Kill ─────────────────────────────────────────────────

//    private void SpawnCurrentBoss()
//    {
//        if (activeBoss != null)
//        {
//            Destroy(activeBoss);
//            activeBoss = null;
//        }

//        if (allBossConfigs == null || allBossConfigs.Count == 0)
//        {
//            AddLog("<color=red>ERROR: No boss configs assigned!</color>");
//            return;
//        }

//        if (bossPrefab == null)
//        {
//            AddLog("<color=red>ERROR: No boss prefab assigned!</color>");
//            return;
//        }

//        var config = allBossConfigs[currentConfigIndex];
//        activeBoss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
//        activeBoss.name = $"[DEBUG] {config.bossName}";

//        activeController = activeBoss.GetComponent<BossBirdController>();
//        activeHealth = activeBoss.GetComponent<BossHealthHandler>();

//        if (activeController == null)
//        {
//            AddLog("<color=red>ERROR: BossBirdController missing on prefab!</color>");
//            return;
//        }
//        if (activeHealth == null)
//        {
//            AddLog("<color=red>ERROR: BossHealthHandler missing on prefab!</color>");
//            return;
//        }
//        if (activeBoss.GetComponent<BossMovementHandler>() == null)
//        {
//            AddLog("<color=red>ERROR: BossMovementHandler missing on prefab!</color>");
//            return;
//        }

//        var issues = ValidateConfig(config);
//        foreach (var issue in issues)
//            AddLog($"<color=yellow>WARNING: {issue}</color>");

//        string phase = spawnAsLevel20 ? "Phase 1+2 (Level 20)" : "Phase 1 (Level 10)";
//        AddLog($"<color=cyan>Spawning: {config.bossName} — {phase}</color>");

//        activeController.Initialize(config, spawnAsLevel20);

//        var behaviours = activeBoss.GetComponents<BaseAttackBehaviour>();
//        AddLog($"  Attack behaviours attached: {behaviours.Length}");
//        foreach (var b in behaviours)
//            AddLog($"    - {b.GetType().Name}");
//    }

//    private void KillActiveBoss()
//    {
//        if (activeBoss == null)
//        {
//            AddLog("No active boss to kill.");
//            return;
//        }

//        AddLog("<color=red>Force-killing boss.</color>");
//        Destroy(activeBoss);
//        activeBoss = null;
//        activeController = null;
//        activeHealth = null;
//    }

//    private void DealDebugDamage()
//    {
//        if (activeHealth == null)
//        {
//            AddLog("No active boss to damage.");
//            return;
//        }

//        AddLog($"Dealing {debugDamageAmount} damage.");
//        activeHealth.TakeDamage(debugDamageAmount);
//    }

//    private void DealPercentDamage(float percent)
//    {
//        if (activeHealth == null)
//        {
//            AddLog("No active boss to damage.");
//            return;
//        }

//        var config = allBossConfigs[currentConfigIndex];
//        float hp = spawnAsLevel20
//            ? config.maxHealth * config.phase2HealthMultiplier
//            : config.maxHealth;
//        float amount = hp * percent;

//        AddLog($"Dealing {percent:P0} damage ({amount:F0} HP).");
//        activeHealth.TakeDamage(amount);
//    }

//    private void CycleConfig(int direction)
//    {
//        if (allBossConfigs == null || allBossConfigs.Count == 0) return;

//        currentConfigIndex = (currentConfigIndex + direction + allBossConfigs.Count)
//                             % allBossConfigs.Count;

//        var config = allBossConfigs[currentConfigIndex];
//        AddLog($"Selected: [{currentConfigIndex + 1}/{allBossConfigs.Count}] {config.bossName}");
//    }

//    // ── Validation ───────────────────────────────────────────────────

//    private List<string> ValidateConfig(BossBirdConfig config)
//    {
//        var issues = new List<string>();

//        if (string.IsNullOrEmpty(config.bossName))
//            issues.Add("bossName is empty");

//        if (config.maxHealth <= 0)
//            issues.Add($"maxHealth is {config.maxHealth} — should be > 0");

//        if (config.phase1Movement == null)
//            issues.Add("phase1Movement is NULL — boss won't move");

//        if (config.phase1Attack == null)
//            issues.Add("phase1Attack is NULL — boss has no attack at level 10");

//        if (config.phase2Attack == null)
//            issues.Add("phase2Attack is NULL — boss has no additional attack at level 20");

//        if (config.bossSprite == null)
//            issues.Add("bossSprite is NULL — boss will be invisible (unless using Spine)");

//        if (config.deathVFX == null)
//            issues.Add("deathVFX is NULL — no death effect will play");

//        if (config.scoreValue <= 0)
//            issues.Add($"scoreValue is {config.scoreValue}");

//        if (config.enrageThreshold > 0 && config.enrageSpeedMultiplier <= 1f)
//            issues.Add($"enrageSpeedMultiplier is {config.enrageSpeedMultiplier} — enrage won't feel different");

//        ValidateAttackConfig(config.phase1Attack, "Phase1", issues);
//        ValidateAttackConfig(config.phase2Attack, "Phase2", issues);

//        return issues;
//    }

//    private void ValidateAttackConfig(BaseAttackConfig atk, string phase, List<string> issues)
//    {
//        if (atk == null) return;

//        if (atk.cooldown <= 0)
//            issues.Add($"{phase}/{atk.name}: cooldown is {atk.cooldown}");
//        if (atk.duration <= 0)
//            issues.Add($"{phase}/{atk.name}: duration is {atk.duration}");
//        if (atk.damage <= 0)
//            issues.Add($"{phase}/{atk.name}: damage is {atk.damage} — attack does nothing");

//        // Type-specific checks
//        if (atk is BeamAttackConfig beam)
//        {
//            if (beam.beamWidth <= 0)
//                issues.Add($"{phase}/{atk.name}: beamWidth is {beam.beamWidth}");
//            if (beam.laserVisualPrefab == null)
//                issues.Add($"{phase}/{atk.name}: laserVisualPrefab is NULL — beam will be invisible");
//        }
//        else if (atk is ProjectileAttackConfig proj)
//        {
//            if (proj.projectilePrefab == null)
//                issues.Add($"{phase}/{atk.name}: projectilePrefab is NULL — nothing will fire");
//            if (proj.count <= 0)
//                issues.Add($"{phase}/{atk.name}: count is {proj.count}");
//            if (proj.speed <= 0)
//                issues.Add($"{phase}/{atk.name}: speed is {proj.speed}");
//        }
//        else if (atk is SpawnerAttackConfig spawn)
//        {
//            if (spawn.spawnPrefab == null)
//                issues.Add($"{phase}/{atk.name}: spawnPrefab is NULL — nothing will spawn");
//            if (spawn.spawnCount <= 0)
//                issues.Add($"{phase}/{atk.name}: spawnCount is {spawn.spawnCount}");
//        }
//        else if (atk is ForceFieldAttackConfig force)
//        {
//            if (force.forceStrength <= 0)
//                issues.Add($"{phase}/{atk.name}: forceStrength is {force.forceStrength}");
//        }
//        else if (atk is ShieldAttackConfig shield)
//        {
//            if (shield.shieldVisualPrefab == null)
//                issues.Add($"{phase}/{atk.name}: shieldVisualPrefab is NULL — shield won't be visible");
//        }
//        else if (atk is GroundHazardAttackConfig hazard)
//        {
//            if (hazard.hazardPrefab == null)
//                issues.Add($"{phase}/{atk.name}: hazardPrefab is NULL — hazard won't appear");
//        }
//    }

//    // ── Batch Validation ─────────────────────────────────────────────

//    private void ValidateAllConfigs()
//    {
//        AddLog("<color=cyan>=== BATCH VALIDATION: ALL BOSSES ===</color>");
//        int totalIssues = 0;

//        for (int i = 0; i < allBossConfigs.Count; i++)
//        {
//            var config = allBossConfigs[i];
//            if (config == null)
//            {
//                AddLog($"<color=red>[{i + 1}] NULL config slot!</color>");
//                totalIssues++;
//                continue;
//            }

//            var issues = ValidateConfig(config);
//            if (issues.Count == 0)
//            {
//                AddLog($"<color=green>[{i + 1}] {config.bossName} — PASS</color>");
//            }
//            else
//            {
//                AddLog($"<color=yellow>[{i + 1}] {config.bossName} — {issues.Count} issue(s)</color>");
//                foreach (var issue in issues)
//                    AddLog($"    {issue}");
//                totalIssues += issues.Count;
//            }
//        }

//        AddLog(totalIssues == 0
//            ? "<color=green>=== ALL BOSSES PASSED ===</color>"
//            : $"<color=yellow>=== {totalIssues} TOTAL ISSUES ===</color>");
//    }

//    // ── Logging ──────────────────────────────────────────────────────

//    private void AddLog(string msg)
//    {
//        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
//        logMessages.Insert(0, $"[{timestamp}] {msg}");
//        if (logMessages.Count > MAX_LOG)
//            logMessages.RemoveAt(logMessages.Count - 1);

//        Debug.Log($"[BossDebug] {msg}");
//    }

//    private void LogSpawned(string name) => AddLog($"EVENT: Boss spawned — {name}");
//    private void LogDefeated(string name, int score) => AddLog($"EVENT: Boss defeated — {name} (+{score} score)");
//    private void LogHealth(float n) => AddLog($"EVENT: Health changed — {n:P0}");
//    private void LogEnraged() => AddLog("EVENT: Boss ENRAGED!");

//    // ── GUI ──────────────────────────────────────────────────────────

//    private void OnGUI()
//    {
//        float panelW = 340f;
//        float panelH = Screen.height - 20f;

//        GUILayout.BeginArea(new Rect(10, 10, panelW, panelH));

//        var headerStyle = new GUIStyle(GUI.skin.label)
//        {
//            fontSize = 16,
//            fontStyle = FontStyle.Bold,
//            normal = { textColor = Color.white }
//        };
//        GUILayout.Label("BOSS DEBUG TESTER", headerStyle);
//        GUILayout.Space(4);

//        if (allBossConfigs != null && allBossConfigs.Count > 0)
//        {
//            var config = allBossConfigs[currentConfigIndex];
//            string bossName = config != null ? config.bossName : "NULL";

//            GUILayout.BeginHorizontal();
//            if (GUILayout.Button("<", GUILayout.Width(30))) CycleConfig(-1);
//            GUILayout.Label($"  [{currentConfigIndex + 1}/{allBossConfigs.Count}] {bossName}",
//                            GUILayout.ExpandWidth(true));
//            if (GUILayout.Button(">", GUILayout.Width(30))) CycleConfig(1);
//            GUILayout.EndHorizontal();

//            string phaseLabel = spawnAsLevel20 ? "Phase 1+2 (Lvl 20)" : "Phase 1 only (Lvl 10)";
//            if (GUILayout.Button($"Phase: {phaseLabel} [P]"))
//                spawnAsLevel20 = !spawnAsLevel20;

//            GUILayout.Space(4);

//            GUILayout.BeginHorizontal();
//            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
//            if (GUILayout.Button("SPAWN [S]")) SpawnCurrentBoss();
//            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
//            if (GUILayout.Button("KILL [K]")) KillActiveBoss();
//            GUI.backgroundColor = Color.white;
//            GUILayout.EndHorizontal();

//            GUILayout.BeginHorizontal();
//            GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
//            if (GUILayout.Button($"Damage {debugDamageAmount} [D]")) DealDebugDamage();
//            if (GUILayout.Button("Damage 25%")) DealPercentDamage(0.25f);
//            if (GUILayout.Button("Damage 50%")) DealPercentDamage(0.50f);
//            GUI.backgroundColor = Color.white;
//            GUILayout.EndHorizontal();

//            GUILayout.Space(4);

//            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
//            if (GUILayout.Button("Validate THIS Boss"))
//            {
//                var issues = ValidateConfig(config);
//                if (issues.Count == 0)
//                    AddLog($"<color=green>{config.bossName} — ALL CHECKS PASSED</color>");
//                else
//                    foreach (var issue in issues)
//                        AddLog($"<color=yellow>{issue}</color>");
//            }
//            if (GUILayout.Button("Validate ALL 13 Bosses"))
//                ValidateAllConfigs();
//            GUI.backgroundColor = Color.white;

//            GUILayout.Space(4);

//            if (config != null)
//            {
//                showAttackDetails = GUILayout.Toggle(showAttackDetails, "Show config details");
//                if (showAttackDetails)
//                {
//                    var boxStyle = new GUIStyle(GUI.skin.box)
//                    {
//                        fontSize = 11,
//                        alignment = TextAnchor.UpperLeft,
//                        normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
//                    };

//                    string p1AtkName = config.phase1Attack != null
//                        ? $"{config.phase1Attack.name} ({config.phase1Attack.GetType().Name})" +
//                          $"  cd:{config.phase1Attack.cooldown:F1}s  dur:{config.phase1Attack.duration:F1}s"
//                        : "NULL";

//                    string p2AtkName = config.phase2Attack != null
//                        ? $"{config.phase2Attack.name} ({config.phase2Attack.GetType().Name})" +
//                          $"  cd:{config.phase2Attack.cooldown:F1}s  dur:{config.phase2Attack.duration:F1}s"
//                        : "NULL";

//                    string details =
//                        $"HP: {config.maxHealth}  |  Phase2 mult: x{config.phase2HealthMultiplier}\n" +
//                        $"Contact dmg: {config.contactDamage}  |  Score: {config.scoreValue}\n" +
//                        $"Enrage at: {config.enrageThreshold:P0}  |  Speed mult: x{config.enrageSpeedMultiplier}\n" +
//                        $"Movement: {(config.phase1Movement != null ? config.phase1Movement.name : "NULL")}\n" +
//                        $"Phase1 attack: {p1AtkName}\n" +
//                        $"Phase2 attack: {p2AtkName}\n";

//                    GUILayout.Box(details, boxStyle);
//                }
//            }

//            if (activeBoss != null)
//            {
//                GUILayout.Space(4);
//                var behaviours = activeBoss.GetComponents<BaseAttackBehaviour>();
//                string status =
//                    $"ACTIVE: {activeBoss.name}\n" +
//                    $"Position: {activeBoss.transform.position:F1}\n" +
//                    $"Attack components: {behaviours.Length}";
//                var statusStyle = new GUIStyle(GUI.skin.box)
//                {
//                    fontSize = 11,
//                    alignment = TextAnchor.UpperLeft,
//                    normal = { textColor = new Color(0.5f, 1f, 0.5f) }
//                };
//                GUILayout.Box(status, statusStyle);
//            }
//        }
//        else
//        {
//            GUILayout.Label("No boss configs assigned!");
//        }

//        GUILayout.Space(8);

//        GUILayout.Label("Event Log:");
//        scrollPos = GUILayout.BeginScrollView(scrollPos, GUI.skin.box,
//                                               GUILayout.ExpandHeight(true));
//        var logStyle = new GUIStyle(GUI.skin.label)
//        {
//            fontSize = 10,
//            richText = true,
//            wordWrap = true
//        };
//        foreach (var msg in logMessages)
//            GUILayout.Label(msg, logStyle);

//        GUILayout.EndScrollView();

//        if (GUILayout.Button("Clear Log"))
//            logMessages.Clear();

//        GUILayout.EndArea();

//        GUI.Label(new Rect(Screen.width - 310, Screen.height - 30, 300, 25),
//                  "S=Spawn  K=Kill  D=Damage  P=Phase  ←→=Cycle",
//                  new GUIStyle(GUI.skin.label)
//                  {
//                      fontSize = 11,
//                      alignment = TextAnchor.MiddleRight,
//                      normal = { textColor = new Color(1f, 1f, 1f, 0.5f) }
//                  });
//    }
//}