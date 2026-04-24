using UnityEngine;
using RemoteTuning.Core.Registry;
namespace RemoteTuning.Examples
{
    /// <summary>
    /// Example game that exposes variables for Remote Tuning.
    /// Shows how to register different control types.
    /// </summary>
    public class SoccerGameExample : MonoBehaviour
    {
        [Header("Ball Physics")]
        [SerializeField] private float kickForce = 50f;
        [SerializeField] private float curveAmount = 0f;
        [Header("Game Settings")]
        [SerializeField] private bool enableSpin = true;
        [SerializeField] private string difficulty = "Normal";
        private void Start()
        {
            // Register variables in Remote Tuning Registry
            RegisterVariables();
        }
        private void RegisterVariables()
        {
            var registry = RemoteTuningRegistry.Instance;
            // Register kickForce (float slider)
            registry.RegisterFloat(
                id: "ball.kickForce",
                label: "Kick Force",
                getter: () => kickForce,
                setter: (value) => {
                    kickForce = value;
                    Debug.Log($"[SoccerGame] Kick Force changed to {value}");
                },
                min: 0f,
                max: 100f,
                step: 0.5f
            );
            // Register curveAmount (float slider with negative range)
            registry.RegisterFloat(
                id: "ball.curveAmount",
                label: "Curve Amount",
                getter: () => curveAmount,
                setter: (value) => {
                    curveAmount = value;
                    Debug.Log($"[SoccerGame] Curve Amount changed to {value}");
                },
                min: -10f,
                max: 10f,
                step: 0.1f
            );
            // Register enableSpin (bool toggle)
            registry.RegisterBool(
                id: "game.enableSpin",
                label: "Enable Spin",
                getter: () => enableSpin,
                setter: (value) => {
                    enableSpin = value;
                    Debug.Log($"[SoccerGame] Enable Spin changed to {value}");
                }
            );
            // Register difficulty (enum dropdown)
            registry.RegisterEnum(
                id: "game.difficulty",
                label: "Difficulty",
                getter: () => difficulty,
                setter: (value) => {
                    difficulty = value;
                    Debug.Log($"[SoccerGame] Difficulty changed to {value}");
                },
                options: new string[] { "Easy", "Normal", "Hard" }
            );
            Debug.Log("[SoccerGame] Remote Tuning variables registered!");
        }
        private void OnDestroy()
        {
            // Clean up registrations on destroy
            var registry = RemoteTuningRegistry.Instance;
            registry.Unregister("ball.kickForce");
            registry.Unregister("ball.curveAmount");
            registry.Unregister("game.enableSpin");
            registry.Unregister("game.difficulty");
        }
        // Example methods using the variables
        public void KickBall()
        {
            Debug.Log($"Kicking ball with force {kickForce} and curve {curveAmount}");
            if (enableSpin)
            {
                Debug.Log("Applying spin to the ball!");
            }
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Current Settings:");
            GUILayout.Label($"Kick Force: {kickForce:F1}");
            GUILayout.Label($"Curve Amount: {curveAmount:F1}");
            GUILayout.Label($"Enable Spin: {enableSpin}");
            GUILayout.Label($"Difficulty: {difficulty}");
            if (GUILayout.Button("Test Kick"))
            {
                KickBall();
            }
            GUILayout.EndArea();
        }
    }
}
