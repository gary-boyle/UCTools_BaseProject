using UnityEngine;
using GameFramework.Components.Controllers.Enum;
using System.Collections.Generic;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Manages player prefab selection and automatic loading based on PlayerPrefabType enum.
    /// Automatically finds prefabs in the Prefabs/Player folder based on naming conventions.
    /// </summary>
    [System.Serializable]
    public class PlayerPrefabSelector
    {
        #region Serialized Fields
        [Header("Player Prefab Selection")]
        [SerializeField] private PlayerPrefabType _selectedPlayerType = PlayerPrefabType.ThirdPerson;
        
        [Header("Prefab References (Auto-populated)")]
        [SerializeField] private GameObject _fpsPrefab;
        [SerializeField] private GameObject _thirdPersonPrefab;
        [SerializeField] private GameObject _rtsPrefab;
        [SerializeField] private GameObject _isometricPrefab;
        #endregion

        #region Private Fields
        private Dictionary<PlayerPrefabType, GameObject> _prefabLookup;
        private bool _prefabsLoaded = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the currently selected player prefab type.
        /// </summary>
        public PlayerPrefabType SelectedPlayerType
        {
            get => _selectedPlayerType;
            set => _selectedPlayerType = value;
        }

        /// <summary>
        /// Gets the currently selected player prefab GameObject.
        /// </summary>
        public GameObject SelectedPrefab
        {
            get
            {
                LoadPrefabsIfNeeded();
                return _prefabLookup?.GetValueOrDefault(_selectedPlayerType);
            }
        }

        /// <summary>
        /// Gets whether prefabs have been successfully loaded.
        /// </summary>
        public bool PrefabsLoaded => _prefabsLoaded;
        #endregion

        #region Public Methods
        /// <summary>
        /// Builds the prefab lookup dictionary from the assigned prefab references.
        /// Called automatically when accessing SelectedPrefab for the first time.
        /// </summary>
        public void LoadPrefabs()
        {
            _prefabLookup = new Dictionary<PlayerPrefabType, GameObject>();

            // Populate lookup dictionary with assigned prefabs
            _prefabLookup[PlayerPrefabType.FPS] = _fpsPrefab;
            _prefabLookup[PlayerPrefabType.ThirdPerson] = _thirdPersonPrefab;
            _prefabLookup[PlayerPrefabType.RTS] = _rtsPrefab;
            _prefabLookup[PlayerPrefabType.Isometric] = _isometricPrefab;

            _prefabsLoaded = true;

            // Log loading results
            LogLoadingResults();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Loads prefabs if they haven't been loaded yet.
        /// </summary>
        private void LoadPrefabsIfNeeded()
        {
            if (!_prefabsLoaded)
            {
                LoadPrefabs();
            }
        }

        /// <summary>
        /// Logs the results of prefab loading for debugging purposes.
        /// </summary>
        private void LogLoadingResults()
        {
            Debug.Log("[PlayerPrefabSelector] Player prefabs loaded:");
            Debug.Log($"  FPS: {(_fpsPrefab != null ? _fpsPrefab.name : "NOT FOUND")}");
            Debug.Log($"  ThirdPerson: {(_thirdPersonPrefab != null ? _thirdPersonPrefab.name : "NOT FOUND")}");
            Debug.Log($"  RTS: {(_rtsPrefab != null ? _rtsPrefab.name : "NOT FOUND")}");
            Debug.Log($"  Isometric: {(_isometricPrefab != null ? _isometricPrefab.name : "NOT FOUND")}");
        }
        #endregion
    }
}
