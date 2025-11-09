using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
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
            var snapshot = new GameData();
            foreach (var toSave in saveables)
            {
                toSave?.OnSave(snapshot);
            }

            gameData = snapshot;
            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(savePath, json);
        }

        public void LoadGame()
        {
            if (!File.Exists(savePath))
                return;

            string json = File.ReadAllText(savePath);
            gameData = JsonUtility.FromJson<GameData>(json) ?? new GameData();
            hasLoadedFromDisk = true;

            foreach (var toLoad in saveables)
            {
                toLoad?.OnLoad(gameData);
            }
        }
    }
}
