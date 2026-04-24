using System;
using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Core.Protocol;
using RemoteTuning.Core.Models;
using RemoteTuning.Core.Utils;

namespace RemoteTuning.Host.Server
{
    /// <summary>
    /// Main host MonoBehaviour for Remote Tuning.
    /// Starts the WebSocket server and manages client connections.
    /// </summary>
    public class RemoteTuningHost : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string gameId = "my-game-v1";
        [SerializeField] private string gameName = "My Game";
        [SerializeField] private int port = 8080;
        [SerializeField] private bool autoStart = true;
        [Header("Status")]
        [SerializeField] private bool isRunning;
        [SerializeField] private string localIP;
        private RTWebSocketServer _server;
        private ConnectionInfo _connectionInfo;
        public ConnectionInfo ConnectionInfo => _connectionInfo;
        public bool IsRunning => isRunning;
        public event Action<string> OnClientConnectedEvent;
        public event Action<string> OnClientDisconnectedEvent;
        private void Start()
        {
            // Ensure dispatcher exists before starting
            _ = UnityMainThreadDispatcher.Instance;
            
            if (autoStart)
            {
                StartHost();
            }
        }
        
        /// <summary>
        /// Starts the Remote Tuning host.
        /// </summary>
        public void StartHost()
        {
            if (isRunning)
            {
                Debug.LogWarning("[RemoteTuningHost] Host already running");
                return;
            }
            // Get local IP
            localIP = NetworkUtils.GetLocalIPAddress();
            // Build connection info
            _connectionInfo = new ConnectionInfo
            {
                host = localIP,
                port = port,
                gameId = gameId,
                gameName = gameName
            };
            // Create and start server
            _server = new RTWebSocketServer(port);
            _server.OnMessageReceived += HandleMessageReceived;
            _server.OnClientConnected += HandleClientConnected;
            _server.OnClientDisconnected += HandleClientDisconnected;
            _server.Start();
            isRunning = true;
            Debug.Log($"[RemoteTuningHost] Host started at {_connectionInfo.GetWebSocketUrl()}");
        }
        public void StopHost()
        {
            if (!isRunning) return;
            _server?.Stop();
            isRunning = false;
            Debug.Log("[RemoteTuningHost] Host stopped");
        }
        private void OnDestroy()
        {
            StopHost();
        }
        #region Message Handlers
        private void HandleClientConnected(string clientId)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[RemoteTuningHost] Client connected: {clientId}");
                OnClientConnectedEvent?.Invoke(clientId);
                SendSchema(clientId);
            });
        }

        private void HandleClientDisconnected(string clientId)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[RemoteTuningHost] Client disconnected: {clientId}");
                OnClientDisconnectedEvent?.Invoke(clientId);
            });
        }

        private void HandleMessageReceived(string clientId, string messageJson)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    var baseMsg = JsonUtility.FromJson<RTMessage>(messageJson);
                    
                    switch (baseMsg.type)
                    {
                        case "hello":
                            HandleHelloMessage(clientId, messageJson);
                            break;
                        case "set":
                            HandleSetMessage(clientId, messageJson);
                            break;
                        case "requestValues":
                            HandleRequestValuesMessage(clientId);
                            break;
                        case "ping":
                            HandlePingMessage(clientId);
                            break;
                        default:
                            Debug.LogWarning($"[RemoteTuningHost] Unknown message type: {baseMsg.type}");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RemoteTuningHost] Error handling message: {e.Message}");
                }
            });
        }

        private void HandleHelloMessage(string clientId, string messageJson)
        {
            SendSchema(clientId);
        }

        private void HandleSetMessage(string clientId, string messageJson)
        {
            var msg = JsonUtility.FromJson<SetMessage>(messageJson);
            bool success = RemoteTuningRegistry.Instance.SetValue(msg.id, msg.GetValue());
            if (!success)
            {
                Debug.LogWarning($"[RemoteTuningHost] Failed to set value for {msg.id}");
            }
        }

        private void HandleRequestValuesMessage(string clientId)
        {
            SendCurrentValues(clientId);
        }

        private void HandlePingMessage(string clientId)
        {
            var pong = new PongMessage();
            _server.SendToClient(clientId, pong.ToJson());
        }
        #endregion
        #region Send Methods
        private void SendSchema(string clientId)
        {
            var schema = RemoteTuningRegistry.Instance.GenerateSchema(gameId, gameName);
            var schemaMsg = new SchemaMessage
            {
                gameId = schema.gameId,
                gameName = schema.gameName,
                version = schema.version,
                controls = schema.controls
            };
            string json = JsonUtility.ToJson(schemaMsg);
            _server.SendToClient(clientId, json);
            Debug.Log($"[RemoteTuningHost] Schema sent to {clientId} ({schema.controls.Length} controls)");
        }
        private void SendCurrentValues(string clientId)
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            var valuesList = new System.Collections.Generic.List<ValueData>();
            foreach (var variable in variables)
            {
                var valueData = new ValueData { id = variable.Id };
                // Update value by type
                switch (variable.Definition.valueType)
                {
                    case RemoteTuning.Core.Models.ValueType.Float:
                        valueData.floatValue = (float)variable.GetValue();
                        break;
                    case RemoteTuning.Core.Models.ValueType.Int:
                        valueData.intValue = (int)variable.GetValue();
                        break;
                    case RemoteTuning.Core.Models.ValueType.Bool:
                        valueData.boolValue = (bool)variable.GetValue();
                        break;
                    case RemoteTuning.Core.Models.ValueType.String:
                    case RemoteTuning.Core.Models.ValueType.Enum:
                        valueData.stringValue = variable.GetValue()?.ToString();
                        break;
                }
                valuesList.Add(valueData);
            }
            var valuesMsg = new ValuesMessage
            {
                values = valuesList.ToArray()
            };
            _server.SendToClient(clientId, JsonUtility.ToJson(valuesMsg));
        }
        #endregion
        #region Public API
        /// <summary>
        /// Returns the connection JSON for the QR code.
        /// </summary>
        public string GetConnectionJson()
        {
            return _connectionInfo?.ToJson();
        }
        
        /// <summary>
        /// Broadcasts a single variable change to all connected clients.
        /// Used when a variable is modified locally (offline mode) so remote clients stay in sync.
        /// </summary>
        /// <param name="variableId">The id of the changed variable.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="valueType">The type of the value, used to fill the correct field.</param>
        public void BroadcastVariableChange(string variableId, object newValue, Core.Models.ValueType valueType)
        {
            if (_server == null || !isRunning)
            {
                return;
            }

            var valueData = new ValueData { id = variableId };

            switch (valueType)
            {
                case Core.Models.ValueType.Float:
                    valueData.floatValue = Convert.ToSingle(newValue);
                    break;
                case Core.Models.ValueType.Int:
                    valueData.intValue = Convert.ToInt32(newValue);
                    break;
                case Core.Models.ValueType.Bool:
                    valueData.boolValue = Convert.ToBoolean(newValue);
                    break;
                case Core.Models.ValueType.String:
                case Core.Models.ValueType.Enum:
                    valueData.stringValue = newValue?.ToString();
                    break;
            }

            var valuesMsg = new ValuesMessage
            {
                values = new[] { valueData }
            };

            _server.BroadcastMessage(JsonUtility.ToJson(valuesMsg));
            Debug.Log($"[RemoteTuningHost] Broadcast change: {variableId} = {newValue}");
        }
        #endregion
    }
}
