using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Systems.SaveSystem.SaveData;

namespace Systems.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance {
            get; private set;
        }

        private string savePath;
        private GameData gameData = new GameData();
        private readonly List<ISaveable> saveables = new List<ISaveable>();
        private bool hasLoadedFromDisk;
        [SerializeField] private bool loadOnAwake = true;

        public void Awake()
        {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }

            savePath = Path.Combine(Application.persistentDataPath, "savegame.json");

            if (loadOnAwake)
                LoadGame();
        }

        public void Register(ISaveable saveable) {
            if (saveable == null || saveables.Contains(saveable))
                return;

            saveables.Add(saveable);

            if (hasLoadedFromDisk && gameData != null)
                saveable.OnLoad(gameData);
        }

        public void Unregister(ISaveable saveable) {
            saveables.Remove(saveable);
        }

        public void SaveGame()
        {
            CaptureAllState();
            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(savePath, json);
        }

        public void CaptureState()
        {
            CaptureSceneState(SceneManager.GetActiveScene());
        }

        public void CaptureAllState()
        {
            var snapshot = gameData ?? new GameData();
            foreach (var toSave in saveables)
            {
                toSave?.OnSave(snapshot);
            }

            gameData = snapshot;
            hasLoadedFromDisk = true;
        }

        public void CaptureSceneState(Scene scene)
        {
            var snapshot = gameData ?? new GameData();
            foreach (var toSave in saveables)
            {
                if (ShouldCaptureSaveable(toSave, scene))
                    toSave?.OnSave(snapshot);
            }

            gameData = snapshot;
            hasLoadedFromDisk = true;
        }

        public void LoadGame()
        {
            if (!File.Exists(savePath))
            {
                Debug.LogWarning($"[SaveManager] Save file not found at {savePath}. Skipping load.");
                return;
            }

            string json = File.ReadAllText(savePath);
            gameData = JsonUtility.FromJson<GameData>(json) ?? new GameData();
            hasLoadedFromDisk = true;

            foreach (var toLoad in saveables)
            {
                toLoad?.OnLoad(gameData);
            }

            Debug.Log($"[SaveManager] Game loaded from {savePath}.");
        }

        private static bool ShouldCaptureSaveable(ISaveable saveable, Scene targetScene)
        {
            if (!targetScene.IsValid())
                return true;

            if (saveable is Component component)
            {
                var scene = component.gameObject.scene;
                return scene == targetScene;
            }

            return true;
        }
    }
}
