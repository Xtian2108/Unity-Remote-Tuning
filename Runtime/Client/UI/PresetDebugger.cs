using UnityEngine;
using RemoteTuning.Client.Connection;

namespace RemoteTuning.Client.UI
{
    /// <summary>
    /// Debug helper to verify that profiles are working correctly.
    /// Attach this script to any GameObject in the client scene.
    /// </summary>
    public class PresetDebugger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PresetUIManager presetManager;
        [SerializeField] private DynamicUIBuilder uiBuilder;
        [SerializeField] private RemoteTuningClient client;

        [Header("Debug Controls")]
        [SerializeField] private bool showDebugButtons = true;

        private void Start()
        {
            // Auto-find components if not assigned
            if (presetManager == null)
                presetManager = FindObjectOfType<PresetUIManager>();
            
            if (uiBuilder == null)
                uiBuilder = FindObjectOfType<DynamicUIBuilder>();
            
            if (client == null)
                client = FindObjectOfType<RemoteTuningClient>();

            LogStatus();
        }

        private void LogStatus()
        {
            Debug.Log("========== PRESET DEBUGGER ==========");
            Debug.Log($"PresetUIManager: {(presetManager != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"DynamicUIBuilder: {(uiBuilder != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"RemoteTuningClient: {(client != null ? "Found" : "NOT FOUND")}");
            
            if (client != null)
            {
                Debug.Log($"Client Connected: {client.IsConnected}");
                Debug.Log($"Schema Loaded: {(client.Schema != null ? $"Yes ({client.Schema.controls.Length} controls)" : "No schema")}");
            }

            Debug.Log("=====================================");
        }

        private void OnGUI()
        {
            if (!showDebugButtons) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== PRESET DEBUGGER ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Space(10);

            GUILayout.Label($"Client: {(client != null && client.IsConnected ? "CONNECTED" : "Disconnected")}", 
                client != null && client.IsConnected ? new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } } : GUI.skin.label);
            
            if (client != null && client.Schema != null)
            {
                GUILayout.Label($"Controls: {client.Schema.controls.Length}");
            }

            GUILayout.Space(10);

            GUILayout.Label("--- Profiles ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            if (GUILayout.Button("Load Profile 1"))
            {
                Debug.Log("[MANUAL] Loading Profile 1");
                if (presetManager != null)
                    presetManager.LoadProfile(1);
                else
                    Debug.LogError("PresetUIManager not found!");
            }

            if (GUILayout.Button("Load Profile 2"))
            {
                Debug.Log("[MANUAL] Loading Profile 2");
                if (presetManager != null)
                    presetManager.LoadProfile(2);
                else
                    Debug.LogError("PresetUIManager not found!");
            }

            if (GUILayout.Button("Load Profile 3"))
            {
                Debug.Log("[MANUAL] Loading Profile 3");
                if (presetManager != null)
                    presetManager.LoadProfile(3);
                else
                    Debug.LogError("PresetUIManager not found!");
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Debug ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            if (GUILayout.Button("Force UI Refresh"))
            {
                Debug.Log("[MANUAL] Forcing UI refresh");
                if (uiBuilder != null)
                    uiBuilder.RefreshUIValues();
                else
                    Debug.LogError("DynamicUIBuilder not found!");
            }

            if (GUILayout.Button("Save Current Profile"))
            {
                Debug.Log("[MANUAL] Saving current profile");
                if (presetManager != null)
                    presetManager.SaveCurrentProfile();
                else
                    Debug.LogError("PresetUIManager not found!");
            }

            if (GUILayout.Button("Show Status"))
            {
                LogStatus();
            }

            if (GUILayout.Button("Reset All Profiles"))
            {
                Debug.Log("[MANUAL] Resetting ALL profiles");
                if (presetManager != null)
                    presetManager.ResetAllProfiles();
                else
                    Debug.LogError("PresetUIManager not found!");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

