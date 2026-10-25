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

namespace VaultHeim
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // The Vault Chest is a new buildable: anyone who sees one (and the server, which keeps the chests in step) needs the mod.
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class VaultHeimPlugin : BaseUnityPlugin
    {
        public const string Guid = "mbaya.VaultHeim";
        public const string Name = "VaultHeim";
        public const string Version = "0.2.0";

        // Based on the Black metal chest's model.
        private const string BasePiece = "piece_chest_blackmetal";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> OthersCanDeposit;
        internal static ConfigEntry<float> SyncSeconds;
        internal static ConfigEntry<float> PanelOffsetX;
        internal static ConfigEntry<float> PanelOffsetY;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            OthersCanDeposit = Config.Bind("Vault", "OthersCanDeposit", true,
                "Other players can open your vault chests and put things in, but only you can take things out. " +
                "If false, only you can open them. (The take-out rule is enforced in each player's game.)");
            SyncSeconds = Config.Bind("Vault", "SyncSeconds", 2f,
                new ConfigDescription("How often the server copies a vault's contents to the player's other vault chests. (Server setting.)", new AcceptableValueRange<float>(0.5f, 30f)));

            PanelOffsetX = Config.Bind("Panel", "PanelOffsetX", 0f, new ConfigDescription("Where you dragged the Restock panel (saved automatically).", new AcceptableValueRange<float>(-3000f, 3000f)));
            PanelOffsetY = Config.Bind("Panel", "PanelOffsetY", 0f, new ConfigDescription("Where you dragged the Restock panel (saved automatically).", new AcceptableValueRange<float>(-3000f, 3000f)));

            PrefabManager.OnVanillaPrefabsAvailable += AddChest;

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(VaultHeimPlugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private void Update()
        {
            VaultServer.Tick();
            VaultPanel.Update();
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();

        private void AddChest()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddChest;

            var piece = new CustomPiece(VaultChest.PrefabName, BasePiece, new PieceConfig
            {
                Name = "Vault Chest",
                Description = "A chest that holds the same things as every other Vault Chest you have built, wherever they are. " +
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
                Log.LogError("Could not create the Vault Chest.");
                return;
            }

            VaultChest.Customise(prefab);
            PieceManager.Instance.AddPiece(piece);
        }
    }
}
