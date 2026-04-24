using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
namespace RemoteTuning.Host.Server
{
    /// <summary>
    /// WebSocket server wrapper using websocket-sharp.
    /// Manages multiple connected clients.
    /// </summary>
    public class RTWebSocketServer
    {
        private WebSocketServer _server;
        private Dictionary<string, RemoteTuningBehavior> _clients;
        private bool _isRunning;
        private int _port;
        public bool IsRunning => _isRunning;
        public int Port => _port;
        public int ConnectedClients => _clients?.Count ?? 0;
        public event Action<string, string> OnMessageReceived;  // clientId, message
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public RTWebSocketServer(int port)
        {
            _port = port;
            _clients = new Dictionary<string, RemoteTuningBehavior>();
        }
        /// <summary>
        /// Inicia el servidor WebSocket
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[RTWebSocketServer] Server already running");
                return;
            }
            try
            {
                _server = new WebSocketServer(_port);
                // Disable internal websocket-sharp logs
                _server.Log.Level = WebSocketSharp.LogLevel.Error;
                
                _server.AddWebSocketService<RemoteTuningBehavior>("/", (behavior) =>
                {
                    behavior.OnMessageReceived += (clientId, message) =>
                    {
                        OnMessageReceived?.Invoke(clientId, message);
                    };
                    
                    behavior.OnClientConnected += (clientId) =>
                    {
                        lock (_clients)
                        {
                            if (!_clients.ContainsKey(clientId))
                            {
                                _clients[clientId] = behavior;
                                Debug.Log($"[RTWebSocketServer] Client {clientId} connected. Total: {_clients.Count}");
                            }
                            else
                            {
                                Debug.LogWarning($"[RTWebSocketServer] Client {clientId} already registered");
                            }
                        }
                        OnClientConnected?.Invoke(clientId);
                    };
                    
                    behavior.OnClientDisconnected += (clientId) =>
                    {
                        lock (_clients)
                        {
                            if (_clients.ContainsKey(clientId))
                            {
                                _clients.Remove(clientId);
                                Debug.Log($"[RTWebSocketServer] Client {clientId} disconnected. Total: {_clients.Count}");
                            }
                        }
                        OnClientDisconnected?.Invoke(clientId);
                    };
                });
                
                // Start server
                _server.Start();
                _isRunning = true;
                Debug.Log($"[RTWebSocketServer] Server started on port {_port}");
                Debug.Log($"[RTWebSocketServer] Listening on ws://0.0.0.0:{_port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RTWebSocketServer] Failed to start server: {e.Message}\n{e.StackTrace}");
            }
        }
        /// <summary>
        /// Detiene el servidor
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            try
            {
                _server?.Stop();
                lock (_clients)
                {
                    _clients.Clear();
                }
                _isRunning = false;
                Debug.Log("[RTWebSocketServer] Server stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RTWebSocketServer] Error stopping server: {e.Message}");
            }
        }
        /// <summary>
        /// Envía un mensaje a un cliente específico
        /// </summary>
        public void SendToClient(string clientId, string message)
        {
            if (!_isRunning)
            {
                Debug.LogWarning("[RTWebSocketServer] Server not running");
                return;
            }
            RemoteTuningBehavior behavior = null;
            lock (_clients)
            {
                _clients.TryGetValue(clientId, out behavior);
            }
            if (behavior != null)
            {
                behavior.SendMessage(message);
            }
            else
            {
                Debug.LogWarning($"[RTWebSocketServer] Client {clientId} not found");
            }
        }

        /// <summary>
        /// Envía un mensaje a todos los clientes conectados
        /// </summary>
        public void BroadcastMessage(string message)
        {
            if (!_isRunning)
            {
                Debug.LogWarning("[RTWebSocketServer] Server not running");
                return;
            }
            int count = 0;
            RemoteTuningBehavior[] behaviors;
            lock (_clients)
            {
                behaviors = _clients.Values.ToArray();
            }
            foreach (var behavior in behaviors)
            {
                behavior.SendMessage(message);
                count++;
            }
        }
        /// <summary>
        /// Obtiene la lista de IDs de clientes conectados
        /// </summary>
        public string[] GetConnectedClientIds()
        {
            lock (_clients)
            {
                return _clients.Keys.ToArray();
            }
        }
    }
}
