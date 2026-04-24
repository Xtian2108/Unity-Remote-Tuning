using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RemoteTuning.Core.Protocol;
using RemoteTuning.Core.Models;
using RemoteTuning.Host.Server;

// NativeWebSocket solo disponible si el paquete com.endel.nativewebsocket esta instalado
#if NATIVE_WEBSOCKET
using NativeWebSocket;
#endif

namespace RemoteTuning.Client.Connection
{
    /// <summary>
    /// Cliente de Remote Tuning para Android/iOS
    /// Usa NativeWebSocket para conexiones reales en dispositivos móviles
    /// </summary>
    public class RemoteTuningClient : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string serverHost = "192.168.0.243";
        [SerializeField] private int serverPort = 8080;
        [SerializeField] private bool autoConnect = false;

        [Header("Status")]
        [SerializeField] private bool isConnected;
        [SerializeField] private string gameId;
        [SerializeField] private string gameName;
        [SerializeField] private int controlsCount;

        private RemoteTuningSchema _schema;
        private ConnectionInfo _connectionInfo;
        
        public bool IsConnected => isConnected;
        public RemoteTuningSchema Schema => _schema;
        public ConnectionInfo ConnectionInfo => _connectionInfo;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<RemoteTuningSchema> OnSchemaReceived;
        public event Action<string> OnError;
        public event Action OnValuesUpdated;

#if NATIVE_WEBSOCKET
        private WebSocket _websocket;

        private void Start()
        {
            if (autoConnect)
            {
                Connect(serverHost, serverPort);
            }
        }

        /// <summary>
        /// Conecta al servidor usando host y puerto
        /// </summary>
        public async void Connect(string host, int port)
        {
            if (isConnected)
            {
                Debug.LogWarning("[RemoteTuningClient] Already connected");
                return;
            }

            try
            {
                string url = $"ws://{host}:{port}";
                Debug.Log($"[RemoteTuningClient] Connecting to {url}...");

                _websocket = new WebSocket(url);

                _websocket.OnOpen += () =>
                {
                    isConnected = true;
                    Debug.Log("[RemoteTuningClient] Connected");
                    OnConnected?.Invoke();
                    SendHello();
                };

                _websocket.OnMessage += (bytes) =>
                {
                    string message = System.Text.Encoding.UTF8.GetString(bytes);
                    ProcessMessage(message);
                };

                _websocket.OnError += (errorMsg) =>
                {
                    Debug.LogError($"[RemoteTuningClient] Error: {errorMsg}");
                    OnError?.Invoke(errorMsg);
                };

                _websocket.OnClose += (code) =>
                {
                    isConnected = false;
                    Debug.Log($"[RemoteTuningClient] Disconnected (Code: {code})");
                    OnDisconnected?.Invoke();
                };

                await _websocket.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteTuningClient] Failed to connect: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Conecta usando ConnectionInfo (desde QR)
        /// </summary>
        public void Connect(ConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
            Connect(connectionInfo.host, connectionInfo.port);
        }

        /// <summary>
        /// Desconecta del servidor
        /// </summary>
        public async void Disconnect()
        {
            if (_websocket != null)
            {
                await _websocket.Close();
                _websocket = null;
            }

            isConnected = false;
        }

        private void Update()
        {
            // NativeWebSocket requires manual dispatch on the main thread.
            // Without this, messages are not processed on Android.
#if !UNITY_WEBGL
            if (_websocket != null && isConnected)
            {
                try
                {
                    _websocket.DispatchMessageQueue();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[RemoteTuningClient] Error dispatching messages: {ex.Message}");
                }
            }
#endif
        }

        private void SendHello()
        {
            var hello = new HelloMessage
            {
                clientId = SystemInfo.deviceUniqueIdentifier,
                clientName = $"Android-{SystemInfo.deviceName}"
            };
            SendMessage(hello.ToJson());
        }

        private void ProcessMessage(string messageJson)
        {
            try
            {
                var baseMsg = JsonUtility.FromJson<RTMessage>(messageJson);

                switch (baseMsg.type)
                {
                    case "schema":
                        HandleSchema(messageJson);
                        break;
                    case "values":
                        HandleValues(messageJson);
                        break;
                    case "pong":
                        break;
                    case "error":
                        var errorMsg = JsonUtility.FromJson<ErrorMessage>(messageJson);
                        Debug.LogError($"[RemoteTuningClient] Server error: {errorMsg.errorMessage}");
                        OnError?.Invoke(errorMsg.errorMessage);
                        break;
                    default:
                        Debug.LogWarning($"[RemoteTuningClient] Unknown message type: {baseMsg.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RemoteTuningClient] Error processing message: {e.Message}");
            }
        }

        private void HandleSchema(string messageJson)
        {
            var schemaMsg = JsonUtility.FromJson<SchemaMessage>(messageJson);
            
            _schema = new RemoteTuningSchema
            {
                gameId = schemaMsg.gameId,
                gameName = schemaMsg.gameName,
                version = schemaMsg.version,
                controls = schemaMsg.controls
            };

            gameId = _schema.gameId;
            gameName = _schema.gameName;
            controlsCount = _schema.controls.Length;

            Debug.Log($"[RemoteTuningClient] Schema received: {_schema.gameName} ({_schema.controls.Length} controls)");

            OnSchemaReceived?.Invoke(_schema);
        }

        private void HandleValues(string messageJson)
        {
            var valuesMsg = JsonUtility.FromJson<ValuesMessage>(messageJson);

            if (_schema != null && valuesMsg.values != null)
            {
                foreach (var value in valuesMsg.values)
                {
                    UpdateControlValue(value);
                }
                OnValuesUpdated?.Invoke();
            }
        }

        private void UpdateControlValue(ValueData value)
        {
            if (_schema == null) return;

            for (int i = 0; i < _schema.controls.Length; i++)
            {
                if (_schema.controls[i].id == value.id)
                {
                    // Actualizar el valor según el tipo
                    switch (_schema.controls[i].valueType)
                    {
                        case RemoteTuning.Core.Models.ValueType.Float:
                            _schema.controls[i].floatValue = value.floatValue;
                            break;
                        case RemoteTuning.Core.Models.ValueType.Int:
                            _schema.controls[i].intValue = value.intValue;
                            break;
                        case RemoteTuning.Core.Models.ValueType.Bool:
                            _schema.controls[i].boolValue = value.boolValue;
                            break;
                        case RemoteTuning.Core.Models.ValueType.String:
                        case RemoteTuning.Core.Models.ValueType.Enum:
                            _schema.controls[i].stringValue = value.stringValue;
                            break;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Envía un cambio de valor al servidor
        /// </summary>
        public void SendValueChange(string controlId, object value, RemoteTuning.Core.Models.ValueType valueType)
        {
            var setMsg = new SetMessage
            {
                id = controlId,
                valueType = valueType.ToString()
            };

            switch (valueType)
            {
                case RemoteTuning.Core.Models.ValueType.Float:
                    setMsg.floatValue = Convert.ToSingle(value);
                    break;
                case RemoteTuning.Core.Models.ValueType.Int:
                    setMsg.intValue = Convert.ToInt32(value);
                    break;
                case RemoteTuning.Core.Models.ValueType.Bool:
                    setMsg.boolValue = Convert.ToBoolean(value);
                    break;
                case RemoteTuning.Core.Models.ValueType.String:
                case RemoteTuning.Core.Models.ValueType.Enum:
                    setMsg.stringValue = value.ToString();
                    break;
            }

            SendMessage(setMsg.ToJson());
        }

        /// <summary>
        /// Solicita los valores actuales del servidor
        /// </summary>
        public void RequestValues()
        {
            var request = new RequestValuesMessage();
            SendMessage(request.ToJson());
        }

        /// <summary>
        /// Envía ping al servidor
        /// </summary>
        public void SendPing()
        {
            var ping = new PingMessage();
            SendMessage(ping.ToJson());
        }

        private void SendMessage(string message)
        {
            if (!isConnected || _websocket == null)
            {
                Debug.LogWarning("[RemoteTuningClient] Not connected");
                return;
            }

            _websocket.SendText(message);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

#else
        // Stub activo cuando NativeWebSocket (com.endel.nativewebsocket) no esta instalado.
        // El cliente no puede conectarse, pero el proyecto compila sin errores.
        private void Start()
        {
            Debug.LogWarning("[RemoteTuningClient] NativeWebSocket is not installed. Install com.endel.nativewebsocket to enable mobile client connections.");
        }

        public void Connect(string host, int port)
        {
            Debug.LogWarning("[RemoteTuningClient] NativeWebSocket is not installed. Install com.endel.nativewebsocket.");
        }

        public void Connect(ConnectionInfo connectionInfo)
        {
            Debug.LogWarning("[RemoteTuningClient] NativeWebSocket is not installed. Install com.endel.nativewebsocket.");
        }

        public void Disconnect() { }
        public void SendValueChange(string controlId, object value, RemoteTuning.Core.Models.ValueType valueType) { }
        public void RequestValues() { }
        public void SendPing() { }
#endif
    }
}

