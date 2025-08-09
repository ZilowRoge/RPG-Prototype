using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Systems.SaveSystem.SaveData;
using Systems.Statistics;
using Systems.Jobs;

namespace Systems.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance {
            get; private set;
        }

        private string savePath;
        private GameData gameData = new GameData();
        private List<ISaveable> saveables = new List<ISaveable>();

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
            if (!saveables.Contains(saveable)) {
                saveables.Add(saveable);
            }
        }

        public void Unregister(ISaveable saveable) {
            saveables.Remove(saveable);
        }

        public void SaveGame()
        {
            foreach(var toSave in saveables)
            {
                toSave.OnSave(gameData);
            }
            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(savePath, json);
        }

        public void LoadGame()
        {
            if (!File.Exists(savePath))
                return;

            foreach(var toLoad in saveables)
            {
                toLoad.OnLoad(gameData);
            }

            string json = File.ReadAllText(savePath);
            gameData = JsonUtility.FromJson<GameData>(json);
        }
    }
}
