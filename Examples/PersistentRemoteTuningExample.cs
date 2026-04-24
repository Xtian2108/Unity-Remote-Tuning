using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Host.Server;

namespace RemoteTuning.Examples
{
    /// <summary>
    /// Example with automatic persistence.
    /// Shows how to register variables that are saved automatically.
    /// </summary>
    public class PersistentRemoteTuningExample : MonoBehaviour
    {
        [Header("Game Variables")]
        [SerializeField] private float playerSpeed = 10f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private bool enableParticles = true;
        [SerializeField] private string difficulty = "Normal";
        
        [Header("References")]
        [SerializeField] private RemoteTuningHost host;
        [SerializeField] private RemoteTuningPersistenceManager persistenceManager;

        void Start()
        {
            // If references are missing, find them
            if (host == null)
                host = FindObjectOfType<RemoteTuningHost>();
            
            if (persistenceManager == null)
                persistenceManager = FindObjectOfType<RemoteTuningPersistenceManager>();

            // Check if persistence manager exists
            if (persistenceManager == null)
            {
                Debug.LogWarning("[Example] RemoteTuningPersistenceManager not found!");
                Debug.LogWarning("[Example] Add the component to the Host GameObject to enable persistence");
            }
            else
            {
                Debug.Log("[Example] Persistence enabled.");
            }

            // Register variables
            RegisterVariables();
        }

        void RegisterVariables()
        {
            var registry = RemoteTuningRegistry.Instance;

            // NOTE: Values will be loaded automatically from PlayerPrefs
            // if there are previously saved values.

            // Player Speed - Float range 0-20
            registry.RegisterFloat(
                id: "player.speed",
                label: "Player Speed",
                getter: () => playerSpeed,
                setter: (val) =>
                {
                    playerSpeed = val;
                    Debug.Log($"[Example] Speed changed: {val}");
                    // Value is saved automatically by the PersistenceManager
                },
                min: 0f,
                max: 20f,
                step: 0.5f
            );

            // Jump Force - Float range 0-15
            registry.RegisterFloat(
                id: "player.jumpForce",
                label: "Jump Force",
                getter: () => jumpForce,
                setter: (val) =>
                {
                    jumpForce = val;
                    Debug.Log($"[Example] Jump force: {val}");
                },
                min: 0f,
                max: 15f,
                step: 0.5f
            );

            // Max Health - Int range 10-200
            registry.RegisterInt(
                id: "player.maxHealth",
                label: "Max Health",
                getter: () => maxHealth,
                setter: (val) =>
                {
                    maxHealth = val;
                    Debug.Log($"[Example] Max health: {val}");
                },
                min: 10,
                max: 200,
                step: 10
            );

            // Enable Particles - Bool (Toggle)
            registry.RegisterBool(
                id: "graphics.enableParticles",
                label: "Enable Particles",
                getter: () => enableParticles,
                setter: (val) =>
                {
                    enableParticles = val;
                    Debug.Log($"[Example] Particles: {(val ? "ON" : "OFF")}");
                }
            );

            // Difficulty - Enum (Dropdown)
            registry.RegisterEnum(
                id: "game.difficulty",
                label: "Difficulty",
                getter: () => difficulty,
                setter: (val) =>
                {
                    difficulty = val;
                    Debug.Log($"[Example] Difficulty: {val}");
                    ApplyDifficultySettings(val);
                },
                options: new string[] { "Easy", "Normal", "Hard", "Extreme" }
            );

            Debug.Log("[Example] Registered 5 variables with automatic persistence.");
        }

        void ApplyDifficultySettings(string diff)
        {
            // Apply settings based on difficulty
            switch (diff)
            {
                case "Easy":
                    // Easy configuration
                    break;
                case "Normal":
                    // Normal configuration
                    break;
                case "Hard":
                    // Hard configuration
                    break;
                case "Extreme":
                    // Extreme configuration
                    break;
            }
        }

        void Update()
        {
            // Simulate variable usage
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"Jump with force: {jumpForce}");
            }

            if (Input.GetKey(KeyCode.W))
            {
                transform.position += Vector3.forward * playerSpeed * Time.deltaTime;
            }
        }

        void OnDestroy()
        {
            // Clean up registrations on destroy
            var registry = RemoteTuningRegistry.Instance;
            registry.Unregister("player.speed");
            registry.Unregister("player.jumpForce");
            registry.Unregister("player.maxHealth");
            registry.Unregister("graphics.enableParticles");
            registry.Unregister("game.difficulty");
        }

        [ContextMenu("Show Current Values")]
        void ShowCurrentValues()
        {
            Debug.Log("=== CURRENT VALUES ===");
            Debug.Log($"Speed: {playerSpeed}");
            Debug.Log($"Jump Force: {jumpForce}");
            Debug.Log($"Max Health: {maxHealth}");
            Debug.Log($"Particles: {enableParticles}");
            Debug.Log($"Difficulty: {difficulty}");
        }

        [ContextMenu("Reset to Defaults")]
        void ResetToDefaults()
        {
            playerSpeed = 10f;
            jumpForce = 5f;
            maxHealth = 100;
            enableParticles = true;
            difficulty = "Normal";
            
            Debug.Log("[Example] Values reset to defaults.");
        }
    }
}

