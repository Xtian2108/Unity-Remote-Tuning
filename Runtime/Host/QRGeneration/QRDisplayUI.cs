using UnityEngine;
using UnityEngine.UI;
using RemoteTuning.Host.Server;
using Sirenix.OdinInspector;
using TMPro;

namespace RemoteTuning.Host.QRGeneration
{
    /// <summary>
    /// UI component to display the QR code on screen.
    /// Generates and updates the QR automatically when the host starts.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class QRDisplayUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RemoteTuningHost host;
        [SerializeField] private bool autoGenerate = true;
        [SerializeField] private int qrSize = 512;
        
        [Header("UI")]
        [SerializeField] private RawImage qrImage;
        [SerializeField] private TextMeshProUGUI infoText;
        
        [Header("Status")]
        [SerializeField] private bool qrGenerated;
        
        private Texture2D _qrTexture;

        private void Start()
        {
            // Get components if not assigned
            if (qrImage == null)
            {
                qrImage = GetComponent<RawImage>();
            }

            // Configure RawImage to prevent display issues
            ConfigureRawImage();

            // Find host if not assigned
            if (host == null)
            {
                host = FindObjectOfType<RemoteTuningHost>();
            }

            if (autoGenerate && host != null)
            {
                // Wait one frame for the host to initialize
                Invoke(nameof(GenerateQR), 0.5f);
            }
        }

        /// <summary>
        /// Configures the RawImage to prevent display issues (duplicates, cropping).
        /// </summary>
        private void ConfigureRawImage()
        {
            if (qrImage == null) return;

            qrImage.uvRect = new Rect(0, 0, 1, 1);
            qrImage.rectTransform.sizeDelta = new Vector2(qrSize, qrSize);
        }

        /// <summary>
        /// Generates and displays the QR code.
        /// </summary>
        public void GenerateQR()
        {
            if (host == null)
            {
                Debug.LogError("[QRDisplayUI] RemoteTuningHost not found!");
                ShowError("Host not found");
                return;
            }

            if (!host.IsRunning)
            {
                Debug.LogWarning("[QRDisplayUI] Host is not running yet, retrying...");
                Invoke(nameof(GenerateQR), 1f);
                return;
            }

            var connectionInfo = host.ConnectionInfo;
            if (connectionInfo == null)
            {
                Debug.LogError("[QRDisplayUI] ConnectionInfo is null!");
                ShowError("No connection info");
                return;
            }

            // Generate QR using improved manual method (avoids duplication)
            string json = connectionInfo.ToJson();
            _qrTexture = QRCodeGenerator.GenerateQRManual(json, qrSize);
            
            if (_qrTexture != null)
            {
                // Display in UI
                qrImage.texture = _qrTexture;
                
                // IMPORTANT: Set UV Rect so the image is displayed fully and correctly.
                // UV Rect defines which portion of the texture is shown.
                // (0,0,1,1) = full texture, no tiling
                qrImage.uvRect = new Rect(0, 0, 1, 1);
                
                // Ensure the texture is not tiled or cropped
                qrImage.rectTransform.sizeDelta = new Vector2(qrSize, qrSize);
                
                qrGenerated = true;

                if (infoText != null)
                {
                    infoText.text = $"Scan to connect\n{connectionInfo.gameName}\n{connectionInfo.host}:{connectionInfo.port}";
                }

                Debug.Log($"[QRDisplayUI] QR generated ({_qrTexture.width}x{_qrTexture.height})");
            }
            else
            {
                ShowError("QR generation failed");
            }
        }

        /// <summary>
        /// Regenerates the QR (useful if the IP changed).
        /// </summary>
        public void RefreshQR()
        {
            if (_qrTexture != null)
            {
                Destroy(_qrTexture);
                _qrTexture = null;
            }
            
            qrGenerated = false;
            GenerateQR();
        }

        /// <summary>
        /// Displays an error message in the UI.
        /// </summary>
        private void ShowError(string message)
        {
            if (infoText != null)
            {
                infoText.text = $"ERROR\n{message}";
                infoText.color = Color.red;
            }
        }

        /// <summary>
        /// Saves the QR as PNG (for debugging purposes).
        /// </summary>
        public void SaveQRToDisk()
        {
            if (_qrTexture == null)
            {
                Debug.LogWarning("[QRDisplayUI] No QR to save");
                return;
            }

            string path = System.IO.Path.Combine(Application.dataPath, "..", "RemoteTuning_QR.png");
            QRCodeGenerator.SaveQRAsPNG(_qrTexture, path);
        }

        private void OnDestroy()
        {
            if (_qrTexture != null)
            {
                Destroy(_qrTexture);
            }
        }

        // Test buttons in Inspector
        [Button("Generate QR Now")]
        private void GenerateQRMenu()
        {
            GenerateQR();
        }

        [Button("Refresh QR")]
        private void RefreshQRMenu()
        {
            RefreshQR();
        }

        [Button("Save QR to Disk")]
        private void SaveQRMenu()
        {
            SaveQRToDisk();
        }
    }
}
