using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RemoteTuning.Client.UI;
using RemoteTuning.Client.Connection;

namespace RemoteTuning.Examples
{
    /// <summary>
    /// Ejemplo de configuración completa del sistema de perfiles
    /// Este script ayuda a crear la UI automáticamente si no existe
    /// </summary>
    public class ProfileUISetupExample : MonoBehaviour
    {
        [Header("Referencias de UI Existente")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Transform profilePanelParent;
        
        [Header("Referencias del Sistema")]
        [SerializeField] private RemoteTuningClient client;
        [SerializeField] private PresetUIManager presetManager;
        
        [Header("Configuración")]
        [SerializeField] private bool autoSetup = true;
        
        private void Start()
        {
            if (autoSetup && presetManager == null)
            {
                SetupProfileSystem();
            }
        }
        
        /// <summary>
        /// Configura el sistema de perfiles automáticamente
        /// </summary>
        [ContextMenu("Setup Profile System")]
        public void SetupProfileSystem()
        {
            Debug.Log("[ProfileUISetup] Setting up profile system...");
            
            // 1. Encontrar o crear Canvas
            if (mainCanvas == null)
            {
                mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError("[ProfileUISetup] Canvas not found. Please create one first.");
                    return;
                }
            }
            
            // 2. Encontrar o crear RemoteTuningClient
            if (client == null)
            {
                client = FindObjectOfType<RemoteTuningClient>();
                if (client == null)
                {
                    Debug.LogWarning("[ProfileUISetup] RemoteTuningClient not found. Make sure to add it.");
                }
            }
            
            // 3. Crear panel de perfiles si no existe
            if (profilePanelParent == null)
            {
                profilePanelParent = CreateProfilePanel();
            }
            
            // 4. Crear botones de perfiles
            Button profile1Btn = CreateProfileButton("Profile1Button", "Perfil 1");
            Button profile2Btn = CreateProfileButton("Profile2Button", "Perfil 2");
            Button profile3Btn = CreateProfileButton("Profile3Button", "Perfil 3");
            Button saveBtnComp = CreateSaveButton();
            
            // 5. Agregar PresetUIManager si no existe
            if (presetManager == null)
            {
                presetManager = gameObject.GetComponent<PresetUIManager>();
                if (presetManager == null)
                {
                    presetManager = gameObject.AddComponent<PresetUIManager>();
                }
            }
            
            // 6. Configurar referencias del PresetUIManager
            ConfigurePresetManager(profile1Btn, profile2Btn, profile3Btn, saveBtnComp);
            
            Debug.Log("[ProfileUISetup] Profile system configured successfully");
        }
        
        private Transform CreateProfilePanel()
        {
            GameObject panel = new GameObject("ProfilePanel");
            panel.transform.SetParent(mainCanvas.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(300, 400);
            
            // Fondo
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            
            // Layout vertical
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "PERFILES";
            title.fontSize = 24;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 40;
            
            return panel.transform;
        }
        
        private Button CreateProfileButton(string buttonName, string text)
        {
            GameObject btnObj = new GameObject(buttonName);
            btnObj.transform.SetParent(profilePanelParent, false);
            
            // Imagen del botón
            Image img = btnObj.AddComponent<Image>();
            img.color = Color.white;
            
            // Botón
            Button btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;
            
            // Layout
            LayoutElement layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 60;
            
            // Texto
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 18;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.black;
            
            return btn;
        }
        
        private Button CreateSaveButton()
        {
            GameObject btnObj = new GameObject("SaveButton");
            btnObj.transform.SetParent(profilePanelParent, false);
            
            // Imagen del botón
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.2f); // Verde
            
            // Botón
            Button btn = btnObj.AddComponent<Button>();
            
            // Layout
            LayoutElement layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 50;
            
            // Texto
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "SAVE CURRENT";
            tmpText.fontSize = 16;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            
            return btn;
        }
        
        private void ConfigurePresetManager(Button p1, Button p2, Button p3, Button save)
        {
            if (presetManager == null)
                return;
            
            // Usar reflexión para asignar las referencias privadas
            var type = typeof(PresetUIManager);
            
            // Client
            var clientField = type.GetField("client", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (clientField != null && client != null)
                clientField.SetValue(presetManager, client);
            
            // Botones
            var p1Field = type.GetField("profile1Button", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (p1Field != null)
                p1Field.SetValue(presetManager, p1);
                
            var p2Field = type.GetField("profile2Button", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (p2Field != null)
                p2Field.SetValue(presetManager, p2);
                
            var p3Field = type.GetField("profile3Button", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (p3Field != null)
                p3Field.SetValue(presetManager, p3);
                
            var saveField = type.GetField("saveCurrentButton", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (saveField != null)
                saveField.SetValue(presetManager, save);
            
            // Textos (buscar en los botones)
            var t1Field = type.GetField("profile1Text", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (t1Field != null)
                t1Field.SetValue(presetManager, p1.GetComponentInChildren<TextMeshProUGUI>());
                
            var t2Field = type.GetField("profile2Text", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (t2Field != null)
                t2Field.SetValue(presetManager, p2.GetComponentInChildren<TextMeshProUGUI>());
                
            var t3Field = type.GetField("profile3Text", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (t3Field != null)
                t3Field.SetValue(presetManager, p3.GetComponentInChildren<TextMeshProUGUI>());
            
            Debug.Log("[ProfileUISetup] PresetUIManager configured with references");
        }
        
        /// <summary>
        /// Muestra información del sistema
        /// </summary>
        [ContextMenu("Show System Info")]
        public void ShowSystemInfo()
        {
            Debug.Log("=== PROFILE SYSTEM INFO ===");
            Debug.Log($"Canvas: {(mainCanvas != null ? "found" : "missing")}");
            Debug.Log($"Client: {(client != null ? "found" : "missing")}");
            Debug.Log($"PresetManager: {(presetManager != null ? "found" : "missing")}");
            Debug.Log($"Profile Panel: {(profilePanelParent != null ? "found" : "missing")}");
            
            if (presetManager != null)
            {
                Debug.Log($"Active Profile: {PlayerPrefs.GetInt("ActiveProfile", 0)}");
                Debug.Log($"Profile 1: {(PlayerPrefs.HasKey("ClientProfile_1") ? "Saved" : "Empty")}");
                Debug.Log($"Profile 2: {(PlayerPrefs.HasKey("ClientProfile_2") ? "Saved" : "Empty")}");
                Debug.Log($"Profile 3: {(PlayerPrefs.HasKey("ClientProfile_3") ? "Saved" : "Empty")}");
            }
        }
    }
}

