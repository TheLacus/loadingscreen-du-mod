// Project:         Loading Screen for Daggerfall Unity
// Web Site:        http://forums.dfworkshop.net/viewtopic.php?f=14&t=469
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/TheLacus/loadingscreen-du-mod
// Original Author: TheLacus (TheLacus@yandex.com)
// Contributors:    

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

/*
 * TODO:
 * - Improve GenderTip().
 * - Seek informations from quests.
 */

namespace LoadingScreen.Components
{
    /// <summary>
    /// Provide a string to be shown on the loading screen,
    /// taking in consideration informations obtained from the save game
    /// with the purpose of providing useful tips.
    /// </summary>
    public class DfTips : LoadingScreenComponent
    {
        #region Tips Definition

#pragma warning disable 0649

        class DaggerfallTips
        {
            public List<string> generic;
            public Location location;
            public Career career;
            public Character character;
            public Progress progress;
            public List<string> death;
        }

        class Location
        {
            public List<string> exterior, dungeon;
        }

        class Career
        {
            public List<string> LOWHEALT, LOWGOLD, HIGHGOLD, LOWLEVEL;
            public string HIGHLEVEL;
            public List<string> WAGON;
        }

        class Character
        {
            public Dictionary<string, List<string>> race;
        }

        class Progress
        {
            public List<string> basic, advanced;
        }

#pragma warning restore 0649

        #endregion

        #region Fields

        /// <summary>
        /// All tips from language-specific file.
        /// </summary>
        DaggerfallTips tips;

        /// <summary>
        /// Fallback tip.
        /// </summary>
        const string fallbackTip = "Something wrong with your tips file...";

        /// <summary>
        /// Fallback tip.
        /// </summary>
        readonly List<string> fallbackTips = new List<string>() { fallbackTip };

        // UI fields
        string tip = string.Empty;

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor for Daggerfall Tips.
        /// </summary>
        public DfTips(Rect rect)
            :base(rect, 9)
        {
            this.style.wordWrap = true;
            ParseTips();
        }

        public override void Draw()
        {
            GUI.Label(rect, tip, style);
        }

        public override void OnLoadingScreen(SaveData_v1 saveData)
        {
            tip = GetTip(saveData);
        }

        public override void OnLoadingScreen(DaggerfallTravelPopUp sender)
        {
            tip = GetTip(sender);
        }

        public override void OnLoadingScreen(PlayerEnterExit.TransitionEventArgs args)
        {
            tip = GetTip(args.TransitionType);
        }

        public override void OnDeathScreen()
        {
            tip = GetTip();
        }

        #endregion

        #region Algorithm

        /// <summary>
        /// Get a tip to show on screen for save loading.
        /// </summary>
        /// <param name="saveData">Save being loaded.</param>
        /// <returns>Tip</returns>
        private string GetTip(SaveData_v1 saveData)
        {
            SetSeed();
            switch (Random.Range(0, 6))
            {
                case 0:
                    // Save specific
                    return RandomTip(SaveTips(saveData));
                case 1:
                case 2:
                    // Scaled on level
                    int playerLevel = saveData.playerData.playerEntity.level;
                    return RandomTip(ScaledTips(playerLevel));
                case 3:
                case 4:
                    // Location
                    return RandomTip(LocationTips(saveData.playerData.playerPosition.insideDungeon));
                default:
                    // Generic tips
                    return RandomTip(tips.generic);
            }
        }

        /// <summary>
        /// Gets a tip to show on screen for fast travel.
        /// </summary>
        /// <param name="sender">Travel popup.</param>
        /// <returns>Tip</returns>
        private string GetTip(DaggerfallTravelPopUp sender)
        {
            SetSeed();
            switch (Random.Range(0, 5))
            {
                case 0:
                case 1:
                    // Scaled on level
                    int playerLevel = GameManager.Instance.PlayerEntity.Level;
                    return RandomTip(ScaledTips(playerLevel));
                case 2:
                case 3:
                    // Location
                    return RandomTip(LocationTips(false));
                default:
                    // Generic tips
                    return RandomTip(tips.generic);
            }
        }

        /// <summary>
        /// Get a tip to show on screen for entering/exiting.
        /// </summary>
        /// <param name="transitionType">Transition in action.</param>
        /// <returns>Tip</returns>
        private string GetTip(PlayerEnterExit.TransitionType transitionType)
        {
            SetSeed();
            const int maxValue = 5;
            switch (Random.Range(0, maxValue))
            {
                case 0:
                    // Generic tips
                    return RandomTip(tips.generic);
                case 1:
                    // Based on player informations
                    return RandomTip(PlayerTips());
                case 2:
                    // Scaled on level
                    int playerLevel = GameManager.Instance.PlayerEntity.Level;
                    return RandomTip(ScaledTips(playerLevel));
                default:
                    // Location
                    bool inDungeon = (transitionType == PlayerEnterExit.TransitionType.ToDungeonInterior);
                    return RandomTip(LocationTips(inDungeon));
            }
        }

        /// <summary>
        /// Get a tip to show on screen for Death Screen.
        /// </summary>
        /// <returns>Tip</returns>
        private string GetTip()
        {
            SetSeed();
            switch (Random.Range(0, 6))
            {
                case 0:
                    // Generic tips
                    return RandomTip(tips.generic);
                case 1:
                case 2:
                    // Location
                    bool inDungeon = GameManager.Instance.IsPlayerInsideDungeon;
                    return RandomTip(LocationTips(inDungeon));
                default:
                    // Death
                    return RandomTip(tips.death);
            }
        }

        #endregion

        #region Algorithm Methods

        /// <summary>
        /// Get tip specific to location
        /// </summary>
        /// <param name="inDungeon">Dungeon or exteriors?</param>
        /// <returns>List of tips</returns>
        private List<string> LocationTips(bool inDungeon)
        {
            const int maxValue = 6; // the higher, the more probable it will be specific
            Location l = tips.location;
            switch (Random.Range(0, maxValue))
            {
                case 0:
                    return l.dungeon;
                case 1:
                    return l.exterior;
                default:
                    return inDungeon ? l.dungeon : l.exterior;
            }
        }

        /// <summary>
        /// Get tips seeking information from the savegame.
        /// </summary>
        /// <param name="saveData">Save.</param>
        private List<string> SaveTips(SaveData_v1 saveData)
        {
            // Variables
            var tips = new List<string>();
            PlayerEntityData_v1 data = saveData.playerData.playerEntity;

            // Race
            tips.AddRange(RaceTip((Races)data.raceTemplate.ID));

            // Others
            HealthTips(tips, data.currentHealth, data.maxHealth);
            GoldTips(tips, data.goldPieces);
            LevelTips(tips, data.level, data.name);
            WagonTips(tips, data.wagonItems.Length);

            return tips;
        }

        /// <summary>
        /// Get tips seeking information from PlayerEntity.
        /// </summary>
        private List<string> PlayerTips()
        {
            // Variables
            var tips = new List<string>();
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            // Race
            tips.AddRange(RaceTip((Races)player.RaceTemplate.ID));

            // Others
            HealthTips(tips, player.CurrentHealth, player.MaxHealth);
            GoldTips(tips, player.GoldPieces);
            LevelTips(tips, player.Level, player.Name);
            WagonTips(tips, player.WagonItems.Count);

            return tips;
        }

        /// <summary>
        /// Race-specific tips
        /// </summary>
        /// <param name="race">Race of player charachter.</param>
        /// <returns>Tips for race</returns>
        private List<string> RaceTip(Races race)
        {
            List<string> raceTips;
            if (tips.character.race.TryGetValue(race.ToString(), out raceTips))
                return raceTips;

            Debug.LogErrorFormat("Failed to get tip for race {0}", race.ToString());

            if (race != Races.None)
                return RaceTip(Races.None);

            return fallbackTips;
        }

        private void HealthTips(List<string> list, int current, int max)
        {
            if (current < (max / 4))
                list.AddRange(tips.career.LOWHEALT);
        }

        private void GoldTips(List<string> list, int gold)
        {
            const int lowGold = 2000, highGold = 5000;
            if (gold < lowGold)
                list.AddRange(tips.career.LOWGOLD);
            else if (gold > highGold)
                list.AddRange(tips.career.HIGHGOLD);
        }

        private void LevelTips(List<string> list, int level, string name)
        {
            const int lowLevel = 11, highLevel = 29;
            if (level < lowLevel)
                list.AddRange(tips.career.LOWLEVEL);
            else if (level > highLevel)
                list.Add(string.Format(tips.career.HIGHLEVEL, name));
        }

        private void WagonTips(List<string> list, int items)
        {
            if (items == 0)
                list.AddRange(tips.career.WAGON);
        }

        /// <summary>
        /// Choose tips according to player level.
        /// </summary>
        /// <param name="playerLevel">Level of player in game.</param>
        /// <returns>List of tips</returns>
        private List<string> ScaledTips(int playerLevel)
        {
            return Random.Range(0, 33) > playerLevel ? tips.progress.basic : tips.progress.advanced;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Loads and parses tips from resources.
        /// </summary>
        private void ParseTips()
        {
            var textAsset = LoadingScreen.Mod.GetAsset<TextAsset>("Tips");
            fsData data = fsJsonParser.Parse(textAsset.text);

            var result = ModManager._serializer.TryDeserialize(data, ref tips);
            if (result.HasWarnings)
                Debug.LogFormat("{0}: {1}", LoadingScreen.Mod.Title, result.FormattedMessages);
        }

        /// <summary>
        /// Init seed for random methods.
        /// </summary>
        private static void SetSeed()
        {
            Random.InitState(System.Environment.TickCount);
        }

        /// <summary>
        /// Get one tip from a list.
        /// </summary>
        /// <param name="tips">List of tips.</param>
        /// <returns>One tip.</returns>
        private static string RandomTip(List<string> tips)
        {
            try
            {
                int index = Random.Range(0, tips.Count);
                return tips[index];
            }
            catch (System.Exception e)
            {
                Debug.LogError("LoadingScreen: Failed to get a tip string\n" + e.ToString());
                return string.Format("{0}({1})", fallbackTip, e.Message);
            }
        }

        #endregion
    }
}
