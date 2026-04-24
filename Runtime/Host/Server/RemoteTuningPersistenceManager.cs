using System.Linq;
using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Core.Persistence;

namespace RemoteTuning.Host.Server
{
    /// <summary>
    /// MonoBehaviour that handles automatic persistence for Remote Tuning.
    /// Saves values when they change and loads them on startup.
    /// </summary>
    public class RemoteTuningPersistenceManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoLoad = true;
        [SerializeField] private bool autoSaveOnChange = true;
        [SerializeField] private bool saveOnApplicationQuit = true;

        [Header("Status")]
        [SerializeField] private int valoresGuardados = 0;
        [SerializeField] private int valoresCargados = 0;

        private void Start()
        {
            if (autoLoad)
            {
                LoadAll();
            }

            if (autoSaveOnChange)
            {
                SubscribeToChanges();
            }
        }

        private void SubscribeToChanges()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            
            foreach (var variable in variables)
            {
                variable.OnValueChanged += (value) =>
                {
                    RemoteTuningPersistence.SaveValue(variable.Id, value, variable.Definition.valueType);
                    valoresGuardados++;
                };
            }

            Debug.Log($"[PersistenceManager] Subscribed to {variables.Count()} variables");
        }

        /// <summary>
        /// Loads all saved values.
        /// </summary>
        public void LoadAll()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            valoresCargados = 0;

            foreach (var variable in variables)
            {
                if (RemoteTuningPersistence.LoadValue(variable.Id, variable.Definition.valueType, out object savedValue))
                {
                    variable.SetValue(savedValue);
                    valoresCargados++;
                }
            }

            Debug.Log($"[PersistenceManager] Loaded {valoresCargados} values");
        }

        /// <summary>
        /// Saves all current values.
        /// </summary>
        public void SaveAll()
        {
            RemoteTuningPersistence.SaveAll();
            Debug.Log("[PersistenceManager] All values saved");
        }

        /// <summary>
        /// Deletes all saved values.
        /// </summary>
        public void DeleteAll()
        {
            RemoteTuningPersistence.DeleteAll();
            valoresGuardados = 0;
            valoresCargados = 0;
            Debug.Log("[PersistenceManager] All values deleted");
        }

        private void OnApplicationQuit()
        {
            if (saveOnApplicationQuit)
            {
                SaveAll();
            }
        }

        private void OnDestroy()
        {
            if (saveOnApplicationQuit)
            {
                SaveAll();
            }
        }
    }
}
