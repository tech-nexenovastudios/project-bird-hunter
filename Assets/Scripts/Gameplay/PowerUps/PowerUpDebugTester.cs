using System.Collections.Generic;
using Gameplay.Birds;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;

namespace Gameplay.PowerUps.Debug
{
    public class PowerUpDebugTester : MonoBehaviour
    {
        [Header("Power-ups to test")]
        public CannonPowerUp[] testPowerUps;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
        [SerializeField] private bool startOpen = true;

        private bool showPanel;
        private Vector2 scrollPos;
        private ICannon cannonRef;
        private BaseCannon cannonMb;
        private readonly List<string> logMessages = new();
        private const int MaxLogs = 25;

        private readonly List<CannonPowerUp> equipped = new();

        private void Start()
        {
            showPanel = startOpen;
            FindCannon();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                showPanel = !showPanel;

            for (int i = 0; i < equipped.Count; i++)
            {
                equipped[i].TickReactives();
                equipped[i].TickSummons();
                equipped[i].TickProjectiles(cannonRef);  
            }
        }

        private void FindCannon()
        {
            cannonMb = FindFirstObjectByType<BaseCannon>();
            cannonRef = cannonMb as ICannon;
            Log(cannonRef != null ? "Cannon found: " + cannonMb.name : "WARNING: No cannon in scene!");
        }

        private void Log(string msg)
        {
            logMessages.Insert(0, $"[{Time.time:F1}] {msg}");
            if (logMessages.Count > MaxLogs)
                logMessages.RemoveAt(logMessages.Count - 1);
            UnityEngine.Debug.Log("[PowerUpDebug] " + msg);
        }

        private void OnGUI()
        {
            if (!showPanel) return;

            float pw = 430;
            Rect panel = new(Screen.width - pw - 10, 20, pw, Screen.height - 40);
            GUI.Box(panel, "");

            GUILayout.BeginArea(panel);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("POWER-UP DEBUG TESTER", GUI.skin.box);
            GUILayout.Space(4);

            DrawCannonStatus();
            GUILayout.Space(6);
            DrawQuickActions();
            GUILayout.Space(6);
            DrawPowerUpList();
            GUILayout.Space(6);
            DrawEquippedList();
            GUILayout.Space(6);
            DrawLog();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawCannonStatus()
        {
            GUILayout.Label("--- Cannon Status ---", GUI.skin.box);

            if (cannonRef == null)
            {
                GUILayout.Label("No cannon found!");
                if (GUILayout.Button("Retry Find Cannon")) FindCannon();
                return;
            }

            GUILayout.Label($"HP: {cannonRef.CurrentHp} / {cannonRef.MaxHp}    Alive: {cannonRef.IsAlive}");
            GUILayout.Label($"ATK: {cannonRef.CurrentAttack} (base {cannonRef.BaseAttack})");
            GUILayout.Label($"Invincible: {cannonRef.IsInvincible}    Shield: {cannonRef.ShieldHits}    Hitbox: {cannonRef.HitboxScale:F2}");
            GUILayout.Label($"Revive: {cannonRef.HasRevive} ({cannonRef.ReviveHealthPercent:P0})");
        }

        private void DrawQuickActions()
        {
            GUILayout.Label("--- Quick Actions ---", GUI.skin.box);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dmg 10")) { cannonRef?.TakeDamage(10); Log("Dealt 10 damage"); }
            if (GUILayout.Button("Dmg 50")) { cannonRef?.TakeDamage(50); Log("Dealt 50 damage"); }
            if (GUILayout.Button("Heal 20")) { cannonRef?.Heal(20); Log("Healed 20"); }
            if (GUILayout.Button("Full Heal")) { if (cannonRef != null) { cannonRef.Heal(cannonRef.MaxHp); Log("Full heal"); } }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill Cannon"))
            {
                if (cannonRef != null) { cannonRef.TakeDamage(cannonRef.MaxHp + 100); Log("Killed cannon"); }
            }
            if (GUILayout.Button("Unequip All")) { UnequipAll(); Log("Unequipped all"); }
            GUILayout.EndHorizontal();
        }

        private void DrawPowerUpList()
        {
            GUILayout.Label("--- Power-ups ---", GUI.skin.box);

            if (testPowerUps == null || testPowerUps.Length == 0)
            {
                GUILayout.Label("Drag power-ups into testPowerUps array.");
                return;
            }

            for (int i = 0; i < testPowerUps.Length; i++)
            {
                var pu = testPowerUps[i];
                if (pu == null) continue;

                string name = pu.config != null ? pu.config.displayName : $"PowerUp [{i}]";
                string tags = GetTags(pu);
                bool hasEnemy = pu.HasEnemyEffects();
                bool hasCannon = pu.HasCannonEffects();

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{name} {tags}", GUILayout.Width(260));

                if (hasEnemy && GUILayout.Button("Cast", GUILayout.Width(50)))
                    TestCast(pu, name);

                if (hasCannon && GUILayout.Button("Equip", GUILayout.Width(55)))
                    TestEquip(pu, name);

                GUILayout.EndHorizontal();
            }
        }

        private void DrawEquippedList()
        {
            GUILayout.Label($"--- Equipped ({equipped.Count}) ---", GUI.skin.box);

            for (int i = equipped.Count - 1; i >= 0; i--)
            {
                string name = equipped[i].config != null ? equipped[i].config.displayName : $"[{i}]";
                GUILayout.BeginHorizontal();
                GUILayout.Label(name, GUILayout.Width(260));
                if (GUILayout.Button("Unequip", GUILayout.Width(65)))
                {
                    string n = name;
                    UnequipSingle(i);
                    Log($"UNEQUIP {n}");
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawLog()
        {
            GUILayout.Label("--- Log ---", GUI.skin.box);
            for (int i = 0; i < logMessages.Count; i++)
                GUILayout.Label(logMessages[i]);
        }

        private void TestCast(CannonPowerUp pu, string name)
        {
            var bird = FindFirstObjectByType<BaseBird>();
            if (bird == null) { Log($"CAST {name}: No bird in scene!"); return; }

            int hpBefore = bird.CurrentHp;
            pu.ExecuteOnEnemy(bird);
            int dealt = hpBefore - bird.CurrentHp;
            Log($"CAST {name} -> {bird.name}: {hpBefore}->{bird.CurrentHp} ({dealt} dmg)");
        }

        private void TestEquip(CannonPowerUp pu, string name)
        {
            if (cannonRef == null) { Log($"EQUIP {name}: No cannon!"); return; }

            int atkB = cannonRef.CurrentAttack;
            int hpB = cannonRef.CurrentHp;
            int maxB = cannonRef.MaxHp;

            pu.ActivateOnCannon(cannonRef);
            equipped.Add(pu);

            // Register projectile modifiers on cannon
            if (cannonMb != null)
                for (int i = 0; i < pu.effects.Count; i++)
                    if (pu.effects[i] is IProjectileModifier pm)
                        cannonMb.RegisterProjectileModifier(pm);

            var changes = new List<string>();
            if (cannonRef.CurrentAttack != atkB) changes.Add($"ATK {atkB}->{cannonRef.CurrentAttack}");
            if (cannonRef.MaxHp != maxB) changes.Add($"MaxHP {maxB}->{cannonRef.MaxHp}");
            if (cannonRef.CurrentHp != hpB) changes.Add($"HP {hpB}->{cannonRef.CurrentHp}");
            if (cannonRef.IsInvincible) changes.Add("INVINCIBLE");
            if (cannonRef.ShieldHits > 0) changes.Add($"Shield={cannonRef.ShieldHits}");
            if (cannonRef.HasRevive) changes.Add($"Revive={cannonRef.ReviveHealthPercent:P0}");

            Log($"EQUIP {name}: {(changes.Count > 0 ? string.Join(", ", changes) : "active (no immediate stat change)")}");
        }

        private void UnequipSingle(int index)
        {
            var pu = equipped[index];
            pu.DeactivateAll();

            if (cannonMb != null)
                for (int i = 0; i < pu.effects.Count; i++)
                    if (pu.effects[i] is IProjectileModifier pm)
                        cannonMb.UnregisterProjectileModifier(pm);

            equipped.RemoveAt(index);
        }

        private void UnequipAll()
        {
            for (int i = equipped.Count - 1; i >= 0; i--)
            {
                equipped[i].DeactivateAll();

                if (cannonMb != null)
                    for (int j = 0; j < equipped[i].effects.Count; j++)
                        if (equipped[i].effects[j] is IProjectileModifier pm)
                            cannonMb.UnregisterProjectileModifier(pm);
            }
            equipped.Clear();
        }

        private string GetTags(CannonPowerUp pu)
        {
            bool e = false, c = false, p = false, r = false, s = false;
            for (int i = 0; i < pu.effects.Count; i++)
            {
                var fx = pu.effects[i];
                if (fx is IEffect<IEntity>) e = true;
                else if (fx is ICannonModifier) c = true;
                else if (fx is IProjectileModifier) p = true;
                else if (fx is IReactiveEffect) r = true;
                else if (fx is ISummonEffect) s = true;
            }

            var tags = new List<string>(5);
            if (e) tags.Add("[E]");
            if (c) tags.Add("[C]");
            if (p) tags.Add("[P]");
            if (r) tags.Add("[R]");
            if (s) tags.Add("[S]");
            return string.Join("", tags);
        }
    }
}