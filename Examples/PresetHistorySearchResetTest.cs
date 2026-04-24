using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Core.Presets;
using RemoteTuning.Core.History;
using RemoteTuning.Host.Server;

namespace RemoteTuning.Examples
{
    /// <summary>
    /// Complete Remote Tuning usage example - Phase 4.
    /// Demonstrates: Presets, History, Reset, Search, etc.
    /// </summary>
    public class PresetHistorySearchResetTest : MonoBehaviour
    {
        [Header("Game Variables")]
        [SerializeField] private float playerSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private int playerHealth = 100;
        [SerializeField] private bool godMode = false;
        [SerializeField] private string difficulty = "Normal";

        [Header("References")]
        [SerializeField] private RemoteTuningAdvancedManager advancedManager;

        private void Start()
        {
            // 1. Register variables
            RegisterVariables();

            // 2. Demo Phase 4 features
            DemoPhase4Features();
        }

        private void RegisterVariables()
        {
            var registry = RemoteTuningRegistry.Instance;

            // Register variables (default values are captured automatically)
            registry.RegisterFloat(
                "player.speed",
                "Player Speed",
                () => playerSpeed,
                (val) => playerSpeed = val,
                min: 1f,
                max: 20f,
                step: 0.5f
            );

            registry.RegisterFloat(
                "player.jumpForce",
                "Jump Force",
                () => jumpForce,
                (val) => jumpForce = val,
                min: 1f,
                max: 30f,
                step: 1f
            );

            registry.RegisterInt(
                "player.health",
                "Player Health",
                () => playerHealth,
                (val) => playerHealth = val,
                min: 10,
                max: 500,
                step: 10
            );

            registry.RegisterBool(
                "player.godMode",
                "God Mode",
                () => godMode,
                (val) => godMode = val
            );

            registry.RegisterEnum(
                "game.difficulty",
                "Difficulty",
                () => difficulty,
                (val) => difficulty = val,
                options: new[] { "Easy", "Normal", "Hard", "Nightmare" }
            );

            Debug.Log("[Phase4Example] Variables registered");
        }

        private void DemoPhase4Features()
        {
            Invoke(nameof(RunDemo), 1f);
        }

        private void RunDemo()
        {
            Debug.Log("========== PHASE 4 DEMO ==========");

            Debug.Log("--- 1. Initial state ---");
            ShowCurrentValues();

            Debug.Log("--- 2. Modifying values ---");
            playerSpeed = 15f;
            jumpForce = 20f;
            godMode = true;
            Debug.Log("Values modified");
            ShowCurrentValues();

            Debug.Log("--- 3. Saving preset 'PowerMode' ---");
            PresetManager.SavePreset("PowerMode", "Increased power settings");

            Debug.Log("--- 4. Modifying more values ---");
            playerSpeed = 3f;
            jumpForce = 5f;
            playerHealth = 50;
            Debug.Log("Values modified again");
            ShowCurrentValues();

            Debug.Log("--- 5. Saving preset 'SlowMode' ---");
            PresetManager.SavePreset("SlowMode", "Slow settings for testing");

            Debug.Log("--- 6. Available presets ---");
            advancedManager.ListPresets();

            Debug.Log("--- 7. Loading preset 'PowerMode' ---");
            PresetManager.LoadPreset("PowerMode");
            ShowCurrentValues();

            Debug.Log("--- 8. Modified variables vs defaults ---");
            advancedManager.ShowModifiedVariables();

            Debug.Log("--- 9. Recent history ---");
            advancedManager.ShowRecentHistory(5);

            Debug.Log("--- 10. History stats ---");
            advancedManager.ShowHistoryStats();

            Debug.Log("--- 11. Search: 'player' ---");
            advancedManager.SearchAndPrint("player");

            Debug.Log("--- 12. Reset to defaults ---");
            advancedManager.ResetAllToDefaults();
            ShowCurrentValues();

            Debug.Log("--- 13. Export preset 'PowerMode' ---");
            string json = PresetManager.ExportPreset("PowerMode");
            Debug.Log($"Preset JSON:\n{json}");

            Debug.Log("========== END DEMO ==========");
        }

        private void ShowCurrentValues()
        {
            Debug.Log($"  Speed: {playerSpeed} | Jump: {jumpForce} | Health: {playerHealth} | GodMode: {godMode} | Difficulty: {difficulty}");
        }

        #region UI Buttons (connect from Inspector)

        [ContextMenu("Full Demo")]
        public void RunDemoFromMenu()
        {
            RunDemo();
        }

        [ContextMenu("Save Current Preset")]
        public void SaveCurrentAsPreset()
        {
            string presetName = $"Preset_{System.DateTime.Now:HHmmss}";
            PresetManager.SavePreset(presetName, "Manual save");
            Debug.Log($"Preset saved: {presetName}");
        }

        [ContextMenu("Reset All")]
        public void ResetEverything()
        {
            advancedManager.ResetAllToDefaults();
            Debug.Log("All values reset to defaults");
        }

        #endregion

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("[F1] Saving preset...");
                SaveCurrentAsPreset();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("[F2] Listing presets...");
                advancedManager.ListPresets();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log("[F3] Showing history...");
                advancedManager.ShowRecentHistory(10);
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                Debug.Log("[F4] Resetting to defaults...");
                advancedManager.ResetAllToDefaults();
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("[F5] Modified variables...");
                advancedManager.ShowModifiedVariables();
            }
        }
    }
}
