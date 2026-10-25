using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ChestHeim
{
    /// <summary>
    /// Clan chests: carry a Clan Key and press Shift+E on a chest you built to share it with a named clan. Everyone who has set
    /// their character's clan to that name can open it, and nobody else can (except you, who built it). The rule is enforced in
    /// each player's game, the same way the Echo Chest's take-out rule is.
    /// </summary>
    internal static class ClanChest
    {
        public const string KeyItem = "ChestHeim_ClanKey";
        private const string ClanKey = "VH.Clan";
        public static readonly int KeyClan = "vh_clan".GetStableHashCode();

        public static string MyClan
        {
            get
            {
                var player = Player.m_localPlayer;
                return player != null && player.m_customData.TryGetValue(ClanKey, out var clan) ? clan : "";
            }
            set
            {
                var player = Player.m_localPlayer;
                if (player != null) player.m_customData[ClanKey] = (value ?? "").Trim();
            }
        }

        public static string ClanOf(Container chest) =>
            chest != null && chest.m_nview != null && chest.m_nview.IsValid() ? chest.m_nview.GetZDO().GetString(KeyClan, "") : "";

        public static bool SameClan(string a, string b) =>
            a.Length > 0 && b.Length > 0 && string.Equals(a.Trim(), b.Trim(), System.StringComparison.CurrentCultureIgnoreCase);

        public static bool HasKey(Player player) =>
            player != null && player.GetInventory().GetAllItems().Any(i => i.m_dropPrefab != null && i.m_dropPrefab.name == KeyItem);

        public static void SetClan(Container chest, string clan)
        {
            if (chest == null || chest.m_nview == null || !chest.m_nview.IsValid()) return;
            chest.m_nview.ClaimOwnership();
            var zdo = chest.m_nview.GetZDO();
            zdo.Set(KeyClan, (clan ?? "").Trim());
            // An echo chest that changes group catches up with the new group's contents (the server does that).
            if (VaultChest.IsVault(zdo)) zdo.Set(VaultChest.KeyJoined, 0);
        }

        /// <summary>The craftable key. It's a plain stone with a new name.</summary>
        public static void AddKey()
        {
            var key = new CustomItem(KeyItem, "Ruby", new ItemConfig
            {
                Name = "Clan Key",
                Description = "Carry it and press Shift+E on a chest you built to share it with your clan.",
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 1,
                Amount = 1,
                Requirements = new[]
                {
                    new RequirementConfig("Bronze", 2, 0, true),
                    new RequirementConfig("FineWood", 2, 0, true),
                },
            });
            if (key.ItemDrop != null) key.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
            ItemManager.Instance.AddItem(key);
        }
    }

    /// <summary>Who may open a chest that has been shared with a clan.</summary>
    [HarmonyPatch(typeof(Container), nameof(Container.CheckAccess))]
    internal static class ClanAccessPatch
    {
        private static void Postfix(Container __instance, long playerID, ref bool __result)
        {
            string clan = ClanChest.ClanOf(__instance);
            if (clan.Length == 0 || VaultChest.IsVault(__instance)) return;

            bool builder = __instance.m_nview.GetZDO().GetLong(ZDOVars.s_creator) == playerID;
            __result = builder || ClanChest.SameClan(clan, ClanChest.MyClan);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class ChestWindowInteractPatch
    {
        private static bool Prefix(Container __instance, Humanoid character, bool hold, bool alt, ref bool __result)
        {
            if (hold || !alt || character != Player.m_localPlayer || !ChestLabel.Labelable(__instance)) return true;
            var player = (Player)character;
            bool echo = VaultChest.IsVault(__instance);
            // Whoever can use the chest can label it; an echo chest only for its builder or clan.
            if (echo ? !VaultRules.CanTake(__instance) : !__instance.CheckAccess(player.GetPlayerID())) return true;

            ChestWindow.Open(__instance);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class ClanHoverPatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            string clan = ClanChest.ClanOf(__instance);
            if (clan.Length > 0) __result += $"\n<color=#b39ddb>Clan chest: {clan}</color>";
            if (ChestLabel.Labelable(__instance))
                __result += "\n[<color=yellow><b>Shift+E</b></color>] Label" + (VaultRules.IsBuilder(__instance) && ClanChest.HasKey(Player.m_localPlayer) ? " / clan" : "");
        }
    }
}
