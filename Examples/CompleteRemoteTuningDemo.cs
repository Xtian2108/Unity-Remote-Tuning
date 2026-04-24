using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Host.Server;
using RemoteTuning.Host.QRGeneration;

namespace RemoteTuning.Examples
{
    /// <summary>
    /// Complete Remote Tuning demo with integrated host.
    /// Add this component to a GameObject to test the full system.
    /// </summary>
    public class CompleteRemoteTuningDemo : MonoBehaviour
    {
        [Header("Game Variables")]
        [SerializeField] private float playerSpeed = 10f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private bool enableParticles = true;
        [SerializeField] private string difficulty = "Normal";
        
        [Header("Host Configuration")]
        [SerializeField] private string gameId = "demo-game-v1";
        [SerializeField] private string gameName = "Remote Tuning Demo";
        [SerializeField] private int port = 8080;

        [Header("Status")]
        [SerializeField] private bool hostRunning;
        [SerializeField] private string serverUrl;
        [SerializeField] private int connectedClients;

        private RemoteTuningHost _host;

        void Start()
        {
            // Create host automatically
            CreateHost();
            
            // Register variables
            RegisterVariables();
            
            // Start host
            StartHost();
        }

        void CreateHost()
        {
            // Find or create host
            _host = FindObjectOfType<RemoteTuningHost>();
            
            if (_host == null)
            {
                var hostObj = new GameObject("RemoteTuningHost");
                _host = hostObj.AddComponent<RemoteTuningHost>();
                Debug.Log("[Demo] Created RemoteTuningHost");
            }
            
            // Subscribe to events
            _host.OnClientConnectedEvent += OnClientConnected;
            _host.OnClientDisconnectedEvent += OnClientDisconnected;
        }

        void StartHost()
        {
            _host.StartHost();
            hostRunning = _host.IsRunning;
            serverUrl = _host.ConnectionInfo?.GetWebSocketUrl();
            
            Debug.Log($"[Demo] Host started at {serverUrl}");
            Debug.Log($"[Demo] Game: {gameName} ({gameId})");
        }

        void RegisterVariables()
        {
            var registry = RemoteTuningRegistry.Instance;

            // Player Speed
            registry.RegisterFloat(
                id: "player.speed",
                label: "Player Speed",
                getter: () => playerSpeed,
                setter: (val) =>
                {
                    playerSpeed = val;
                    Debug.Log($"<color=green>[Demo] Player Speed → {val}</color>");
                },
                min: 0f,
                max: 20f,
                step: 0.5f
            );

            // Jump Force
            registry.RegisterFloat(
                id: "player.jumpForce",
                label: "Jump Force",
                getter: () => jumpForce,
                setter: (val) =>
                {
                    jumpForce = val;
                    Debug.Log($"<color=green>[Demo] Jump Force → {val}</color>");
                },
                min: 0f,
                max: 15f,
                step: 0.5f
            );

            // Max Health
            registry.RegisterInt(
                id: "player.maxHealth",
                label: "Max Health",
                getter: () => maxHealth,
                setter: (val) =>
                {
                    maxHealth = val;
                    Debug.Log($"<color=green>[Demo] Max Health → {val}</color>");
                },
                min: 10,
                max: 200,
                step: 10
            );

            // Enable Particles
            registry.RegisterBool(
                id: "game.enableParticles",
                label: "Enable Particles",
                getter: () => enableParticles,
                setter: (val) =>
                {
                    enableParticles = val;
                    Debug.Log($"<color=green>[Demo] Enable Particles → {val}</color>");
                }
            );

            // Difficulty
            registry.RegisterEnum(
                id: "game.difficulty",
                label: "Difficulty",
                getter: () => difficulty,
                setter: (val) =>
                {
                    difficulty = val;
                    Debug.Log($"<color=green>[Demo] Difficulty → {val}</color>");
                },
                options: new string[] { "Easy", "Normal", "Hard", "Extreme" }
            );

            Debug.Log($"[Demo] Registered 5 variables");
        }

        void OnClientConnected(string clientId)
        {
            connectedClients++;
            Debug.Log($"<color=cyan>[Demo] Client connected: {clientId}</color>");
            Debug.Log($"<color=cyan>[Demo] Total clients: {connectedClients}</color>");
        }

        void OnClientDisconnected(string clientId)
        {
            connectedClients--;
            Debug.Log($"<color=yellow>[Demo] Client disconnected: {clientId}</color>");
            Debug.Log($"<color=yellow>[Demo] Total clients: {connectedClients}</color>");
        }

        void Update()
        {
            // Test keys to simulate gameplay
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SimulateJump();
            }

            if (Input.GetKey(KeyCode.W))
            {
                SimulateMove();
            }
        }

        void SimulateJump()
        {
            Debug.Log($"[Demo] Jump! (Force: {jumpForce})");
        }

        void SimulateMove()
        {
            // Simulate movement
            transform.position += Vector3.forward * playerSpeed * Time.deltaTime;
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 750));
            
            GUILayout.Label("<b><size=16>REMOTE TUNING DEMO</size></b>");
            GUILayout.Space(10);
            
            // Status
            GUILayout.Label($"<b>Server Status:</b> {(hostRunning ? "<color=green>RUNNING</color>" : "<color=red>STOPPED</color>")}");
            GUILayout.Label($"<b>URL:</b> {serverUrl}");
            GUILayout.Label($"<b>Connected Clients:</b> {connectedClients}");
            
            GUILayout.Space(10);
            GUILayout.Label("<b>Current Values:</b>");
            GUILayout.Label($"  Player Speed: {playerSpeed:F1}");
            GUILayout.Label($"  Jump Force: {jumpForce:F1}");
            GUILayout.Label($"  Max Health: {maxHealth}");
            GUILayout.Label($"  Particles: {enableParticles}");
            GUILayout.Label($"  Difficulty: {difficulty}");
            
            GUILayout.Space(10);
            GUILayout.Label("<b>Test Controls:</b>");
            GUILayout.Label("  [SPACE] Simulate Jump");
            GUILayout.Label("  [W] Simulate Move");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Show Connection JSON"))
            {
                string connectionJson = _host.GetConnectionJson();
                Debug.Log($"<color=yellow>CONNECTION JSON:</color>\n{connectionJson}");
            }
            
            if (GUILayout.Button("Show Schema JSON"))
            {
                var schema = RemoteTuningRegistry.Instance.GenerateSchema(gameId, gameName);
                Debug.Log($"<color=yellow>SCHEMA JSON:</color>\n{JsonUtility.ToJson(schema, true)}");
            }
            
            if (GUILayout.Button("Generate QR Code"))
            {
                GenerateAndShowQR();
            }
            
            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            // Unregister variables
            var registry = RemoteTuningRegistry.Instance;
            registry.Unregister("player.speed");
            registry.Unregister("player.jumpForce");
            registry.Unregister("player.maxHealth");
            registry.Unregister("game.enableParticles");
            registry.Unregister("game.difficulty");

            // Stop host
            if (_host != null)
            {
                _host.StopHost();
            }
        }
        
        void GenerateAndShowQR()
        {
            var connectionInfo = _host.ConnectionInfo;
            if (connectionInfo == null)
            {
                Debug.LogWarning("Host not running or ConnectionInfo not available");
                return;
            }

            // Use improved manual generator (avoids duplication)
            string json = connectionInfo.ToJson();
            var qrTexture = QRCodeGenerator.GenerateQRManual(json, 512);
            
            if (qrTexture != null)
            {
                // Save as PNG
                string path = System.IO.Path.Combine(Application.dataPath, "..", "RemoteTuning_QR.png");
                QRCodeGenerator.SaveQRAsPNG(qrTexture, path);
                
                Debug.Log($"<color=green>QR Code generated and saved to: {path}</color>");
                Debug.Log($"<color=cyan>Connection JSON: {json}</color>");
                Debug.Log($"<color=cyan>Texture Size: {qrTexture.width}x{qrTexture.height}</color>");
                
                // Open folder in Windows Explorer
                #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                System.Diagnostics.Process.Start("explorer.exe", "/select," + path.Replace("/", "\\"));
                #endif
            }
            else
            {
                Debug.LogError("<color=red>Failed to generate QR texture</color>");
            }
        }
    }
}

