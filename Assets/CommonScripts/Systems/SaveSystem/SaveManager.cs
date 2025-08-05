using System;
using System.IO;
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

        public void SavePlayerStats(Player.Statistics.StatsController stats)
        {
            if (gameData.playerData == null)
                gameData.playerData = new PlayerStatisticsData();

            gameData.playerData.currentHealth = stats.CurrentHealth;
            gameData.playerData.currentMana = stats.CurrentMana;
            gameData.playerData.currentStamina = stats.CurrentStamina;
        }

        public void SaveGame()
        {
            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(savePath, json);
        }

        public void LoadGame()
        {
            if (!File.Exists(savePath))
                return;

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
        }
    }
}
