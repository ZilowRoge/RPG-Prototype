using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Systems.SaveSystem.SaveData;
using Systems.Statistics;

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

        public void SavePlayerStats(Player.Statistics.StatsController stats)
        {
            if (gameData.playerData == null)
                gameData.playerData = new PlayerStatisticsData();

            gameData.playerData.currentHealth = stats.CurrentHealth;
            gameData.playerData.currentMana = stats.CurrentMana;
            gameData.playerData.currentStamina = stats.CurrentStamina;

            List<SerializedStatEntry> statEntries = new();
            foreach(var element in stats.GetCurrentStats().GetAll()) {
                statEntries.Add(new SerializedStatEntry{
                    key = element.stat.ToString(),
                    value =  element.value 
                });
            }
            gameData.playerData.stats = statEntries;
            gameData.playerData.level = stats.CurrentLevel;
            gameData.playerData.experience = stats.CurrentExperience;
            gameData.playerData.pendingStatPoints = stats.GetPendingPoints();
        }

        public void SaveGame()
        {
            foreach(var toSave in saveables)
            {
                toSave.OnSave();
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
                toLoad.OnLoad();
            }

            string json = File.ReadAllText(savePath);
            gameData = JsonUtility.FromJson<GameData>(json);
        }

        public void LoadPlayerStats(Player.Statistics.StatsController stats)
        {
            if (gameData == null || gameData.playerData == null)
                return;

            stats.SetHealth(gameData.playerData.currentHealth);
            stats.SetMana(gameData.playerData.currentMana);
            stats.SetStamina(gameData.playerData.currentStamina);
            stats.SetLevel(gameData.playerData.level);
            stats.SetExperience(gameData.playerData.experience);

            var statList = new List<(EStatistics, int)>();
            foreach (var statData in gameData.playerData.stats) {
                if (Enum.TryParse(statData.key, out EStatistics stat)) {
                    statList.Add((stat, statData.value));
                }
            }
            stats.GetCurrentStats().SetStats(statList, gameData.playerData.pendingStatPoints);
        }
    }
}
