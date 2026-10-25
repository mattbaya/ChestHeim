using HarmonyLib;
using UnityEngine;

namespace ChestHeim
{
    internal static class VaultRules
    {
        /// <summary>Set while a chest is being emptied without its contents being kept (so that isn't recorded as a change).</summary>
        public static bool Quiet;

        public static bool IsBuilder(Container chest) =>
            Player.m_localPlayer != null && chest.m_nview.GetZDO().GetLong(ZDOVars.s_creator) == Player.m_localPlayer.GetPlayerID();

        /// <summary>The builder, or (for a chest linked to a clan) anyone whose clan matches.</summary>
        public static bool CanTake(Container chest) =>
            IsBuilder(chest) || ClanChest.SameClan(ClanChest.ClanOf(chest), ClanChest.MyClan);

        public static void Say(string text) => Player.m_localPlayer?.Message(MessageHud.MessageType.Center, text);

        public static long Now() => ZNet.instance != null ? (long)(ZNet.instance.GetTimeSeconds() * 1000.0) : 0L;
    }

    /// <summary>Whenever the player changes a echo chest's contents, note when, so the server knows which copy is newest.</summary>
    [HarmonyPatch(typeof(Container), "Save")]
    internal static class SavePatch
    {
        private static void Postfix(Container __instance)
        {
            if (VaultRules.Quiet || !VaultChest.IsVault(__instance)) return;
            var zdo = __instance.m_nview.GetZDO();
            // A chest that hasn't been brought into the group yet (just built) must never overwrite the vault with an empty one.
            if (zdo.GetInt(VaultChest.KeyJoined) != 1) return;
            zdo.Set(VaultChest.KeyStamp, VaultRules.Now());
        }
    }

    /// <summary>Who can open a echo chest, and no two of a player's chests open at once.</summary>
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class OpenPatch
    {
        private static bool Prefix(Container __instance, bool hold, ref bool __result)
        {
            if (hold || !VaultChest.IsVault(__instance)) return true;

            string clan = ClanChest.ClanOf(__instance);
            if (clan.Length > 0 && !VaultRules.CanTake(__instance))
            {
                VaultRules.Say("This chest belongs to a clan");
                __result = true;
                return false;
            }

            if (clan.Length == 0 && !ChestHeimPlugin.OthersCanDeposit.Value && !VaultRules.IsBuilder(__instance))
            {
                VaultRules.Say("This vault is private");
                __result = true;
                return false;
            }

            if (__instance.m_nview.GetZDO().GetInt(VaultChest.KeyBusy) > 0)
            {
                VaultRules.Say("This vault is open in another chest");
                __result = true;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Taking a echo chest down must not spill the vault's contents on the ground while the player's other chests still hold
    /// them (that would duplicate everything). If there are others, the chest is just emptied quietly; the last one drops its
    /// contents as any chest does.
    /// </summary>
    [HarmonyPatch(typeof(Container), "OnDestroyed")]
    internal static class DestroyPatch
    {
        private static bool Prefix(Container __instance)
        {
            if (!VaultChest.IsVault(__instance)) return true;
            if (__instance.m_nview.GetZDO().GetInt(VaultChest.KeyCount) < 2) return true;

            VaultRules.Quiet = true;
            try { __instance.m_inventory.RemoveAll(); }
            finally { VaultRules.Quiet = false; }
            return false;
        }
    }

    // --- Only the builder can take things out of a echo chest. Everyone else can look and put things in. ---

    internal static class TakeRule
    {
        public static bool OtherPlayersVault(out Container chest)
        {
            chest = InventoryGui.instance != null ? InventoryGui.instance.m_currentContainer : null;
            return chest != null && VaultChest.IsVault(chest) && !VaultRules.CanTake(chest);
        }

        public static void Refuse() => VaultRules.Say("Only its builder can take things out of this chest");
    }

    [HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
    internal static class TakeAllPatch
    {
        private static bool Prefix()
        {
            if (!TakeRule.OtherPlayersVault(out _)) return true;
            TakeRule.Refuse();
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
    internal static class RightClickPatch
    {
        private static bool Prefix(InventoryGrid grid)
        {
            if (!TakeRule.OtherPlayersVault(out var chest) || grid.GetInventory() != chest.GetInventory()) return true;
            TakeRule.Refuse();
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
    internal static class SelectPatch
    {
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item)
        {
            if (!TakeRule.OtherPlayersVault(out var chest) || grid.GetInventory() != chest.GetInventory()) return true;

            // Picking something up out of the chest (or swapping it for what you hold) is taking.
            bool holding = __instance.m_dragGo != null && __instance.m_dragItem != null;
            if (!holding)
            {
                if (item == null) return true;   // clicking an empty slot
                TakeRule.Refuse();
                return false;
            }

            // Putting down is fine in an empty slot, or on a stack of the same thing (it just adds to it).
            if (item == null) return true;
            var drag = __instance.m_dragItem;
            bool joins = item.m_shared.m_name == drag.m_shared.m_name && item.m_quality == drag.m_quality && item.m_stack < item.m_shared.m_maxStackSize;
            if (joins) return true;

            TakeRule.Refuse();
            return false;
        }
    }
}
