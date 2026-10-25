using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace EchoChests
{
    /// <summary>
    /// Clan chests: carry a Clan Key and press Shift+E on a chest you built to share it with a named clan. Everyone who has set
    /// their character's clan to that name can open it, and nobody else can (except you, who built it). The rule is enforced in
    /// each player's game, the same way the Echo Chest's take-out rule is.
    /// </summary>
    internal static class ClanChest
    {
        public const string KeyItem = "EchoChests_ClanKey";
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
    internal static class ClanInteractPatch
    {
        private static bool Prefix(Container __instance, Humanoid character, bool hold, bool alt, ref bool __result)
        {
            if (hold || !alt || character != Player.m_localPlayer) return true;
            var player = (Player)character;
            if (!ClanChest.HasKey(player) || !VaultRules.IsBuilder(__instance)) return true;

            ClanWindow.Open(__instance);
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
            if (VaultRules.IsBuilder(__instance) && ClanChest.HasKey(Player.m_localPlayer))
                __result += "\n[<color=yellow><b>Shift+E</b></color>] Clan sharing";
        }
    }

    /// <summary>The little window for choosing your clan and sharing the chest with it.</summary>
    internal static class ClanWindow
    {
        private const int WindowId = 0x7A11;
        private static Container _chest;
        private static string _clanText = "";
        private static string _message = "";
        private static Rect _rect;
        private static CursorLockMode _lockBefore;
        private static bool _visibleBefore;

        public static bool IsOpen => _chest != null;

        public static void Open(Container chest)
        {
            _chest = chest;
            _clanText = ClanChest.MyClan;
            _message = "";
            _lockBefore = Cursor.lockState;
            _visibleBefore = Cursor.visible;
            float s = Scale;
            _rect = new Rect((Screen.width - 460f * s) / 2f, (Screen.height - 390f * s) / 2f, 460f * s, 390f * s);
        }

        public static void Close()
        {
            if (_chest == null) return;
            _chest = null;
            Cursor.lockState = _lockBefore;
            Cursor.visible = _visibleBefore;
        }

        private static float Scale => Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);

        public static void Draw()
        {
            if (_chest == null) return;
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead() || Vector3.Distance(player.transform.position, _chest.transform.position) > 8f)
            {
                Close();
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                Close();
                return;
            }

            GUI.skin.label.fontSize = Mathf.RoundToInt(16f * Scale);
            GUI.skin.button.fontSize = Mathf.RoundToInt(16f * Scale);
            GUI.skin.textField.fontSize = Mathf.RoundToInt(16f * Scale);
            GUI.Box(_rect, GUIContent.none);
            _rect = GUI.Window(WindowId, _rect, DrawWindow, "Clan chest");
        }

        private static void DrawWindow(int id)
        {
            float s = Scale;
            string shared = ClanChest.ClanOf(_chest);
            GUILayout.Space(8f * s);
            GUILayout.Label("Your clan (saved on this character):");
            _clanText = GUILayout.TextField(_clanText, 24, GUILayout.Height(28f * s));
            GUILayout.Space(6f * s);
            bool echo = VaultChest.IsVault(_chest);
            GUILayout.Label(shared.Length > 0 ? $"This chest is shared with the clan \"{shared}\"." : "This chest isn't shared.");
            if (echo) GUILayout.Label("An Echo Chest set to a clan shows the same things as every other Echo Chest set to that clan. Its own contents are replaced by theirs.");
            GUILayout.Space(6f * s);

            bool hasClan = _clanText.Trim().Length > 0;
            GUI.enabled = hasClan;
            if (GUILayout.Button(echo ? "Link this chest with my clan" : "Share this chest with my clan", GUILayout.Height(32f * s)))
            {
                ClanChest.MyClan = _clanText;
                ClanChest.SetClan(_chest, _clanText);
                _message = $"Shared with {_clanText.Trim()}. Anyone whose clan is set to that name can open it.";
            }

            GUI.enabled = shared.Length > 0;
            if (GUILayout.Button(echo ? "Back to my own echo chests" : "Stop sharing", GUILayout.Height(32f * s)))
            {
                ClanChest.SetClan(_chest, "");
                _message = "No longer shared.";
            }

            GUI.enabled = hasClan;
            if (GUILayout.Button("Just save my clan name", GUILayout.Height(28f * s)))
            {
                ClanChest.MyClan = _clanText;
                _message = "Saved.";
            }

            GUI.enabled = true;
            if (_message.Length > 0) GUILayout.Label(_message);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close (Esc)", GUILayout.Height(28f * s))) Close();
            GUI.DragWindow();
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class ClanTakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ClanWindow.IsOpen) __result = false;
        }
    }

    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class ClanControllerInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ClanWindow.IsOpen) __result = false;
        }
    }
}
