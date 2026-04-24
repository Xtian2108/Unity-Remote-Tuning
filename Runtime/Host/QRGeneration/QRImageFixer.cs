using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace RemoteTuning.Host.QRGeneration
{
    /// <summary>
    /// Utility script to fix QR display issues.
    /// Useful when the QR appears duplicated, cropped or misscaled.
    /// </summary>
    [ExecuteAlways]
    public class QRImageFixer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage rawImage;

        [Header("Settings")]
        [SerializeField] private int targetSize = 512;
        [SerializeField] private bool maintainAspectRatio = true;

        [Header("Debug Info")]
        [SerializeField, ReadOnly] private Vector2 currentSize;
        [SerializeField, ReadOnly] private Rect currentUVRect;
        [SerializeField, ReadOnly] private Vector2 textureSize;

        private void OnValidate()
        {
            if (rawImage == null)
                rawImage = GetComponent<RawImage>();

            UpdateDebugInfo();
        }

        private void Start()
        {
            if (rawImage == null)
                rawImage = GetComponent<RawImage>();

            FixRawImage();
        }

        [Button("Fix RawImage", ButtonSizes.Large)]
        public void FixRawImage()
        {
            if (rawImage == null)
            {
                Debug.LogError("[QRImageFixer] RawImage not assigned!");
                return;
            }

            // 1. Set UV Rect correctly (full texture, no tiling)
            rawImage.uvRect = new Rect(0, 0, 1, 1);

            // 2. Set square size
            rawImage.rectTransform.sizeDelta = new Vector2(targetSize, targetSize);

            // 3. Set anchors to center
            rawImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 4. Reset local position
            rawImage.rectTransform.anchoredPosition = Vector2.zero;

            UpdateDebugInfo();

            Debug.Log($"<color=green>[QRImageFixer] RawImage fixed!</color>");
            Debug.Log($"  UV Rect: {rawImage.uvRect}");
            Debug.Log($"  Size: {rawImage.rectTransform.sizeDelta}");
        }

        [Button("Reset to Defaults")]
        public void ResetToDefaults()
        {
            if (rawImage == null) return;

            rawImage.uvRect = new Rect(0, 0, 1, 1);
            rawImage.rectTransform.sizeDelta = new Vector2(512, 512);
            rawImage.rectTransform.anchoredPosition = Vector2.zero;
            rawImage.rectTransform.localScale = Vector3.one;

            UpdateDebugInfo();

            Debug.Log("<color=yellow>[QRImageFixer] Values reset</color>");
        }

        [Button("Center in Canvas")]
        public void CenterInCanvas()
        {
            if (rawImage == null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[QRImageFixer] Parent Canvas not found");
                return;
            }

            rawImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rawImage.rectTransform.anchoredPosition = Vector2.zero;

            Debug.Log("<color=cyan>[QRImageFixer] Centered in canvas</color>");
        }

        [Button("Fit to Texture")]
        public void FitToTexture()
        {
            if (rawImage == null || rawImage.texture == null)
            {
                Debug.LogWarning("[QRImageFixer] No texture assigned");
                return;
            }

            int texWidth = rawImage.texture.width;
            int texHeight = rawImage.texture.height;

            if (maintainAspectRatio)
            {
                // Keep 1:1 aspect ratio (square)
                int size = Mathf.Min(texWidth, texHeight);
                rawImage.rectTransform.sizeDelta = new Vector2(size, size);
            }
            else
            {
                rawImage.rectTransform.sizeDelta = new Vector2(texWidth, texHeight);
            }

            UpdateDebugInfo();

            Debug.Log($"<color=cyan>[QRImageFixer] Fitted to texture: {texWidth}x{texHeight}</color>");
        }

        private void UpdateDebugInfo()
        {
            if (rawImage == null) return;

            currentSize = rawImage.rectTransform.sizeDelta;
            currentUVRect = rawImage.uvRect;
            
            if (rawImage.texture != null)
            {
                textureSize = new Vector2(rawImage.texture.width, rawImage.texture.height);
            }
        }

        [Button("Show Detailed Info")]
        public void ShowDetailedInfo()
        {
            if (rawImage == null)
            {
                Debug.LogError("[QRImageFixer] RawImage not assigned!");
                return;
            }

            Debug.Log("=== QR IMAGE INFO ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"RawImage: {rawImage.name}");
            Debug.Log($"UV Rect: {rawImage.uvRect}");
            Debug.Log($"RectTransform Size: {rawImage.rectTransform.sizeDelta}");
            Debug.Log($"Anchored Position: {rawImage.rectTransform.anchoredPosition}");
            Debug.Log($"Local Scale: {rawImage.rectTransform.localScale}");
            Debug.Log($"Pivot: {rawImage.rectTransform.pivot}");
            Debug.Log($"Anchors: Min={rawImage.rectTransform.anchorMin}, Max={rawImage.rectTransform.anchorMax}");
            
            if (rawImage.texture != null)
            {
                Debug.Log($"Texture: {rawImage.texture.name}");
                Debug.Log($"Texture Size: {rawImage.texture.width}x{rawImage.texture.height}");
                Debug.Log($"Texture Format: {rawImage.texture.graphicsFormat}");
            }
            else
            {
                Debug.LogWarning("No texture assigned");
            }
            
            Debug.Log("=====================");
        }
    }
}
