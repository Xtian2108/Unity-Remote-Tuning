using UnityEngine;
using UnityEngine.UI;
using RemoteTuning.Client.UI;

namespace RemoteTuning.Examples
{
    /// <summary>
    /// Example showing how to use the 3 profiles/presets in RemoteTuning.
    /// Demonstrates the correct flow for creating and using independent profiles.
    /// </summary>
    public class PresetUsageExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PresetUIManager presetManager;

        [Header("Example Buttons")]
        [SerializeField] private Button setupProfile1Button;
        [SerializeField] private Button setupProfile2Button;
        [SerializeField] private Button setupProfile3Button;
        [SerializeField] private Button testSwitchButton;

        private void Start()
        {
            if (presetManager == null)
            {
                presetManager = FindObjectOfType<PresetUIManager>();
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (setupProfile1Button != null)
            {
                setupProfile1Button.onClick.AddListener(SetupProfile1Example);
            }

            if (setupProfile2Button != null)
            {
                setupProfile2Button.onClick.AddListener(SetupProfile2Example);
            }

            if (setupProfile3Button != null)
            {
                setupProfile3Button.onClick.AddListener(SetupProfile3Example);
            }

            if (testSwitchButton != null)
            {
                testSwitchButton.onClick.AddListener(TestSwitchBetweenProfiles);
            }
        }

        /// <summary>
        /// Example: Set up Profile 1 with specific values.
        /// The client must be connected and UI values configured before saving.
        /// </summary>
        private void SetupProfile1Example()
        {
            Debug.Log("[PresetExample] Setting up Profile 1...");
            
            // Step 1: Load/select Profile 1 (marks it as active)
            presetManager.LoadProfile(1);
            
            // Step 2: User should modify values in the UI as desired
            Debug.Log("[PresetExample] Modify the values in the UI, then save.");
            
            // Step 3: Save to Profile 1
            // presetManager.SaveToProfile(1);
            // Or use SaveCurrentProfile() if Profile 1 is the active one
        }

        /// <summary>
        /// Example: Set up Profile 2 with different values.
        /// </summary>
        private void SetupProfile2Example()
        {
            Debug.Log("[PresetExample] Setting up Profile 2...");
            
            // Load Profile 2 (marks it as active)
            presetManager.LoadProfile(2);
            
            Debug.Log("[PresetExample] Modify the values for Profile 2, then save.");
        }

        /// <summary>
        /// Example: Set up Profile 3.
        /// </summary>
        private void SetupProfile3Example()
        {
            Debug.Log("[PresetExample] Setting up Profile 3...");
            
            // Load Profile 3 (marks it as active)
            presetManager.LoadProfile(3);
            
            Debug.Log("[PresetExample] Modify the values for Profile 3, then save.");
        }

        /// <summary>
        /// Example: Switch between profiles to verify they retain independent values.
        /// </summary>
        private void TestSwitchBetweenProfiles()
        {
            Debug.Log("[PresetExample] === TEST: Switching between profiles ===");
            
            StartCoroutine(SwitchProfilesRoutine());
        }

        private System.Collections.IEnumerator SwitchProfilesRoutine()
        {
            Debug.Log("[PresetExample] Loading Profile 1...");
            presetManager.LoadProfile(1);
            yield return new WaitForSeconds(2);

            Debug.Log("[PresetExample] Loading Profile 2...");
            presetManager.LoadProfile(2);
            yield return new WaitForSeconds(2);

            Debug.Log("[PresetExample] Loading Profile 3...");
            presetManager.LoadProfile(3);
            yield return new WaitForSeconds(2);

            Debug.Log("[PresetExample] Returning to Profile 1...");
            presetManager.LoadProfile(1);

            Debug.Log("[PresetExample] Test complete. Verify each profile retains its unique values.");
        }

        /// <summary>
        /// Example: Save current configuration to a specific profile.
        /// </summary>
        [ContextMenu("Save Current to Profile 1")]
        public void SaveCurrentToProfile1()
        {
            Debug.Log("[PresetExample] Saving current values to Profile 1...");
            presetManager.SaveToProfile(1);
        }

        [ContextMenu("Save Current to Profile 2")]
        public void SaveCurrentToProfile2()
        {
            Debug.Log("[PresetExample] Saving current values to Profile 2...");
            presetManager.SaveToProfile(2);
        }

        [ContextMenu("Save Current to Profile 3")]
        public void SaveCurrentToProfile3()
        {
            Debug.Log("[PresetExample] Saving current values to Profile 3...");
            presetManager.SaveToProfile(3);
        }

        /// <summary>
        /// Example: Reset all profiles (useful for testing).
        /// </summary>
        [ContextMenu("Reset All Profiles")]
        public void ResetAllProfilesExample()
        {
            Debug.Log("[PresetExample] Resetting ALL profiles...");
            presetManager.ResetAllProfiles();
            Debug.Log("[PresetExample] All profiles deleted. You can create new ones from scratch.");
        }

        /// <summary>
        /// Example: Reset a specific profile.
        /// </summary>
        [ContextMenu("Reset Profile 1")]
        public void ResetProfile1Example()
        {
            Debug.Log("[PresetExample] Resetting Profile 1...");
            presetManager.ResetProfile(1);
        }

        #region Full Example Flow

        /// <summary>
        /// Full example: Create 3 profiles with different configurations.
        /// Note: This method is for demonstration only. In practice, the user
        /// will set the values manually in the UI before saving.
        /// </summary>
        [ContextMenu("Demo: Create 3 Independent Profiles")]
        public void DemoCreateThreeProfiles()
        {
            Debug.Log("[PresetExample] === DEMO: Creating 3 Independent Profiles ===");
            
            // Profile 1: "Easy Configuration"
            Debug.Log("[PresetExample] 1. Setting up Profile 1 (Easy)...");
            presetManager.LoadProfile(1);
            Debug.Log("[PresetExample]    -> Configure values for EASY mode in the UI");
            Debug.Log("[PresetExample]    -> Then run 'Save Current to Profile 1' from the context menu");
            
            // Profile 2: "Normal Configuration"
            Debug.Log("[PresetExample] 2. Setting up Profile 2 (Normal)...");
            Debug.Log("[PresetExample]    -> After saving Profile 1, click the Profile 2 button");
            Debug.Log("[PresetExample]    -> Configure values for NORMAL mode");
            Debug.Log("[PresetExample]    -> Run 'Save Current to Profile 2'");
            
            // Profile 3: "Hard Configuration"
            Debug.Log("[PresetExample] 3. Setting up Profile 3 (Hard)...");
            Debug.Log("[PresetExample]    -> Click the Profile 3 button");
            Debug.Log("[PresetExample]    -> Configure values for HARD mode");
            Debug.Log("[PresetExample]    -> Run 'Save Current to Profile 3'");
            
            Debug.Log("[PresetExample] Once complete, you will have 3 independent profiles.");
        }

        #endregion
    }
}

