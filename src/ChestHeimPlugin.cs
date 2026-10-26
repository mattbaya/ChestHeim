using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace ChestHeim
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // The Echo Chest is a new buildable: anyone who sees one (and the server, which keeps the chests in step) needs the mod.
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class ChestHeimPlugin : BaseUnityPlugin
    {
        public const string Guid = "mbaya.ChestHeim";
        public const string Name = "ChestHeim";
        public const string Version = "0.5.3";

        // Based on the Black metal chest's model.
        private const string BasePiece = "piece_chest_blackmetal";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> OthersCanDeposit;
        internal static ConfigEntry<float> SyncSeconds;
        internal static ConfigEntry<bool> LabelsEnabled;
        internal static ConfigEntry<LabelSide> LabelSideSetting;
        internal static ConfigEntry<float> PanelOffsetX;
        internal static ConfigEntry<float> PanelOffsetY;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            OthersCanDeposit = Config.Bind("Echo", "OthersCanDeposit", true,
                "Other players can open your echo chests and put things in, but only you can take things out. " +
                "If false, only you can open them. (The take-out rule is enforced in each player's game.)");
            SyncSeconds = Config.Bind("Echo", "SyncSeconds", 2f,
                new ConfigDescription("How often the server copies an echo chest's contents to the player's other echo chests. (Server setting.)", new AcceptableValueRange<float>(0.5f, 30f)));

            LabelsEnabled = Config.Bind("Labels", "ShowLabels", true, "Show the labels written on chests (Shift+E on a chest to write one).");
            LabelSideSetting = Config.Bind("Labels", "Side", LabelSide.Front, "Which side of a chest its label is on. If it appears on the wrong side, try another.");
            PanelOffsetX = Config.Bind("Panel", "PanelOffsetX", 0f, new ConfigDescription("Where you dragged the Restock panel (saved automatically).", new AcceptableValueRange<float>(-3000f, 3000f)));
            PanelOffsetY = Config.Bind("Panel", "PanelOffsetY", 0f, new ConfigDescription("Where you dragged the Restock panel (saved automatically).", new AcceptableValueRange<float>(-3000f, 3000f)));

            PrefabManager.OnVanillaPrefabsAvailable += AddChest;

            _harmony = new Harmony(Guid);
            SafePatch.All(_harmony, typeof(ChestHeimPlugin).Assembly, Log);
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private void Update()
        {
            VaultServer.Tick();
            VaultPanel.Update();
        }

        private void OnGUI() => ChestWindow.Draw();

        private void OnDestroy() => _harmony?.UnpatchSelf();

        private void AddChest()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddChest;

            var piece = new CustomPiece(VaultChest.PrefabName, BasePiece, new PieceConfig
            {
                Name = "Echo Chest",
                Description = "A chest that holds the same things as every other Echo Chest you have built, wherever they are. " +
                    "Anyone can put things in; only its builder can take them out.",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                Requirements = new[]
                {
                    new RequirementConfig("FineWood", 10, 0, true),
                    new RequirementConfig("Iron", 10, 0, true),
                    new RequirementConfig("SurtlingCore", 5, 0, true),
                },
            });

            var prefab = piece.PiecePrefab;
            if (prefab == null)
            {
                Log.LogError("Could not create the Echo Chest.");
                return;
            }

            ClanChest.AddKey();
            VaultChest.Customise(prefab);
            PieceManager.Instance.AddPiece(piece);
        }
    }
}
