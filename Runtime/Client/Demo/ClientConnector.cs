using UnityEngine;
using RemoteTuning.Client.QRScanning;
using RemoteTuning.Client.Connection;
using RemoteTuning.Host.Server;

public class ClientConnector : MonoBehaviour
{
    public QRCodeScanner      scanner;
    public RemoteTuningClient client;
    public GameObject         qrPanel;
    public GameObject         controlsPanel;

    private bool _isConnecting = false;

    void Awake()
    {
        Debug.Log("[ClientConnector] ==========================================");
        Debug.Log("[ClientConnector] AWAKE called");
        Debug.Log($"[ClientConnector] GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"[ClientConnector] Component enabled: {enabled}");
        Debug.Log("[ClientConnector] ==========================================");
    }

    void Start()
    {
        Debug.Log("[ClientConnector] ==========================================");
        Debug.Log("[ClientConnector] START called");
        Debug.Log($"[ClientConnector] Scanner: {(scanner != null ? "Assigned" : "NULL")}");
        Debug.Log($"[ClientConnector] Client: {(client != null ? "Assigned" : "NULL")}");
        Debug.Log($"[ClientConnector] QR Panel: {(qrPanel != null ? "Assigned" : "NULL")}");
        Debug.Log($"[ClientConnector] Controls Panel: {(controlsPanel != null ? "Assigned" : "NULL")}");
        Debug.Log("[ClientConnector] ==========================================");
        
        if (scanner == null || client == null)
        {
            Debug.LogError("[ClientConnector] REFERENCES NOT ASSIGNED IN INSPECTOR!");
            Debug.LogError("[ClientConnector] Events will NOT work!");
        }
    }

    void OnEnable()
    {
        Debug.Log("[ClientConnector] ==========================================");
        Debug.Log("[ClientConnector] ONENABLE called - Subscribing to events...");
        
        if (scanner != null)
        {
            scanner.OnQRScanned += OnQRScanned;
            Debug.Log("[ClientConnector] Subscribed to scanner.OnQRScanned");
        }
        else
            Debug.LogError("[ClientConnector] Scanner is NULL!");
        
        if (client != null)
        {
            client.OnSchemaReceived += OnSchemaReceived;
            client.OnConnected += OnClientConnected;
            client.OnDisconnected += OnClientDisconnected;
            client.OnError += OnClientError;
            Debug.Log("[ClientConnector] Subscribed to all client events");
        }
        else
            Debug.LogError("[ClientConnector] Client is NULL!");
        
        Debug.Log("[ClientConnector] ==========================================");
    }

    void OnDisable()
    {
        if (scanner != null)
            scanner.OnQRScanned -= OnQRScanned;
        
        if (client != null)
        {
            client.OnSchemaReceived -= OnSchemaReceived;
            client.OnConnected -= OnClientConnected;
            client.OnDisconnected -= OnClientDisconnected;
            client.OnError -= OnClientError;
        }
    }

    void OnQRScanned(string qrData)
    {
        Debug.Log($"[ClientConnector] QR Scanned! Length: {qrData.Length}");
        
        if (_isConnecting)
        {
            Debug.LogWarning("[ClientConnector] Already connecting, ignoring...");
            return;
        }
        
        try
        {
            var connectionInfo = JsonUtility.FromJson<ConnectionInfo>(qrData);
            Debug.Log($"[ClientConnector] [OK] Parsed: {connectionInfo.host}:{connectionInfo.port}");
            
            _isConnecting = true;
            client.Connect(connectionInfo);
            
            if (qrPanel != null) qrPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(true);
            
            Debug.Log("[ClientConnector] UI panels switched");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ClientConnector] Error parsing QR: {ex.Message}");
            _isConnecting = false;
        }
    }

    void OnClientConnected()
    {
        Debug.Log("[ClientConnector] CLIENT CONNECTED!");
        _isConnecting = false;
    }

    void OnClientDisconnected()
    {
        Debug.LogWarning("[ClientConnector] CLIENT DISCONNECTED");
        _isConnecting = false;
        
        if (qrPanel != null) qrPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    void OnClientError(string error)
    {
        Debug.LogError($"[ClientConnector] ERROR: {error}");
        _isConnecting = false;
    }

    void OnSchemaReceived(RemoteTuning.Core.Models.RemoteTuningSchema schema)
    {
        if (schema == null)
        {
            Debug.LogError("[ClientConnector] Schema is NULL!");
            return;
        }
        
        Debug.Log($"[ClientConnector] Schema received!");
        Debug.Log($"[ClientConnector]   Game: {schema.gameName}");
        Debug.Log($"[ClientConnector]   Controls: {schema.controls?.Length ?? 0}");
    }

    public void StartScanning()
    {
        Debug.Log("[ClientConnector] Starting QR scanner...");
        if (scanner != null)
            scanner.StartScanning();
        else
            Debug.LogError("[ClientConnector] Cannot start - scanner is NULL");
    }
}

