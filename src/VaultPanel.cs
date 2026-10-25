using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace VaultHeim
{
    /// <summary>
    /// A panel beside your open Vault Chest: pick how many of some items you like to carry, click Restock, and the vault tops your
    /// inventory up to those amounts. The amounts are kept on your character, so every vault chest uses the same list.
    /// </summary>
    internal static class VaultPanel
    {
        private const string TargetsKey = "VH.Restock";
        private const float Width = 400f;
        private const float RowHeight = 34f;
        private const float Zoom = 1.3f;
        private const int MaxRows = 10;
        private const int MaxResults = 5;

        private static GameObject _panel;
        private static GameObject _close;
        private static InputField _search;
        private static string _query = "";
        private static string _status = "";
        private static Container _chest;
        private static bool _blocking;
        private static Vector2 _offset;

        // ------------------------------------------------------------------ the saved list

        private static List<KeyValuePair<string, int>> Targets()
        {
            var list = new List<KeyValuePair<string, int>>();
            var player = Player.m_localPlayer;
            if (player == null || !player.m_customData.TryGetValue(TargetsKey, out var text) || string.IsNullOrEmpty(text)) return list;
            foreach (var part in text.Split(';'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) && amount > 0)
                    list.Add(new KeyValuePair<string, int>(kv[0], amount));
            }

            return list;
        }

        private static void SaveTargets(IEnumerable<KeyValuePair<string, int>> targets)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            player.m_customData[TargetsKey] = string.Join(";", targets.Where(t => t.Value > 0).Select(t => t.Key + "=" + t.Value.ToString(CultureInfo.InvariantCulture)));
        }

        private static void Adjust(string prefab, int delta)
        {
            var targets = Targets();
            int index = targets.FindIndex(t => t.Key == prefab);
            int amount = Mathf.Max(0, (index >= 0 ? targets[index].Value : 0) + delta);
            if (index >= 0) targets.RemoveAt(index);
            if (amount > 0) targets.Add(new KeyValuePair<string, int>(prefab, amount));
            SaveTargets(targets);
            Rebuild();
        }

        private static ItemDrop.ItemData.SharedData SharedOf(string prefab)
        {
            var go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = go != null ? go.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData.m_shared : null;
        }

        private static string NameOf(string prefab)
        {
            var shared = SharedOf(prefab);
            return shared != null ? Localization.instance.Localize(shared.m_name) : prefab;
        }

        private static int Carried(Inventory inventory, string prefab) =>
            inventory.GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefab).Sum(i => i.m_stack);

        // ------------------------------------------------------------------ the actions

        private static void Remember()
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            var totals = new Dictionary<string, int>();
            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (item.m_dropPrefab == null || item.m_equipped) continue;
                string name = item.m_dropPrefab.name;
                totals[name] = (totals.TryGetValue(name, out int n) ? n : 0) + item.m_stack;
            }

            SaveTargets(totals);
            _status = "Remembered what you're carrying.";
            Rebuild();
        }

        /// <summary>Takes from the open vault chest whatever it takes to bring the inventory up to the amounts.</summary>
        private static void Restock()
        {
            var player = Player.m_localPlayer;
            if (player == null || _chest == null) return;
            var inventory = player.GetInventory();
            var vault = _chest.GetInventory();
            int moved = 0, short_ = 0;

            foreach (var target in Targets())
            {
                int need = target.Value - Carried(inventory, target.Key);
                if (need <= 0) continue;

                foreach (var item in vault.GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == target.Key).ToList())
                {
                    if (need <= 0) break;
                    int take = Mathf.Min(need, item.m_stack);
                    var copy = item.Clone();
                    copy.m_stack = take;
                    if (!inventory.CanAddItem(copy)) break;
                    if (!inventory.AddItem(copy)) break;
                    vault.RemoveItem(item, take);
                    need -= take;
                    moved += take;
                }

                if (need > 0) short_++;
            }

            _status = moved == 0 ? "Nothing to take." : $"Took {moved} item{(moved == 1 ? "" : "s")}." + (short_ > 0 ? $" {short_} short." : "");
            Rebuild();
        }

        // ------------------------------------------------------------------ showing and placing

        public static void Show(Container chest)
        {
            _chest = chest;
            _query = "";
            _status = "";
            _offset = new Vector2(VaultHeimPlugin.PanelOffsetX.Value, VaultHeimPlugin.PanelOffsetY.Value);
            Rebuild();
        }

        public static void Hide()
        {
            if (_panel != null) Object.Destroy(_panel);
            if (_close != null) Object.Destroy(_close);
            _panel = null;
            _close = null;
            _search = null;
            _chest = null;
            SetBlock(false);
        }

        public static void Update()
        {
            if (_panel == null) return;
            var contents = InventoryGui.instance != null ? InventoryGui.instance.m_container : null;
            var rect = _panel.GetComponent<RectTransform>();
            if (contents != null && rect.parent != contents) Attach();
            SetBlock(_search != null && _search.isFocused);
        }

        private static void SetBlock(bool block)
        {
            if (block == _blocking) return;
            _blocking = block;
            GUIManager.BlockInput(block);
        }

        private static void Attach()
        {
            var rect = _panel.GetComponent<RectTransform>();
            var contents = InventoryGui.instance != null ? InventoryGui.instance.m_container : null;
            rect.localScale = Vector3.one * Zoom;
            if (contents == null) return;
            rect.SetParent(contents, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(6f, 0f) + _offset;
        }

        private static void CloseChest()
        {
            if (InventoryGui.instance != null && InventoryGui.instance.m_currentContainer != null) InventoryGui.instance.CloseContainer();
        }

        // ------------------------------------------------------------------ building the panel

        private static void Rebuild()
        {
            if (_chest == null || GUIManager.CustomGUIFront == null) { Hide(); return; }
            if (_panel != null) Object.Destroy(_panel);

            var targets = Targets();
            var results = SearchResults(targets);
            int rows = Mathf.Max(1, Mathf.Min(targets.Count, MaxRows));
            float height = 230f + rows * RowHeight + (results.Count + 1) * RowHeight;

            var gui = GUIManager.Instance;
            _panel = gui.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-Width / 2 - 30f, 0f), Width, height, false);
            var canvas = _panel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            _panel.AddComponent<GraphicRaycaster>();
            var drag = _panel.AddComponent<PanelDrag>();
            drag.Target = _panel.GetComponent<RectTransform>();
            drag.Moved = delta => { _offset += delta; Attach(); };
            drag.Dropped = () => { VaultHeimPlugin.PanelOffsetX.Value = _offset.x; VaultHeimPlugin.PanelOffsetY.Value = _offset.y; };
            drag.Reset = () => { _offset = Vector2.zero; Attach(); VaultHeimPlugin.PanelOffsetX.Value = 0f; VaultHeimPlugin.PanelOffsetY.Value = 0f; };
            Attach();

            Button("X", Width / 2 - 34f, -24f, 44f, CloseChest);

            float y = -28f;
            Text("Restock", 0f, y, 24, gui.ValheimOrange, Width - 100f, TextAnchor.MiddleCenter);
            y -= 34f;
            Text("Set what you like to carry, then click Restock.", 0f, y, 14, Color.gray, Width - 40f, TextAnchor.MiddleCenter);
            y -= 40f;

            Button("Restock", -95f, y, 190f, Restock);
            Button("Remember what I carry", 110f, y, 200f, Remember);
            y -= 38f;
            if (_status.Length > 0)
            {
                Text(_status, 0f, y, 14, gui.ValheimBeige, Width - 40f, TextAnchor.MiddleCenter);
            }

            y -= RowHeight;

            if (targets.Count == 0)
            {
                Text("Nothing yet. Add items below.", 0f, y, 14, Color.gray, Width - 40f, TextAnchor.MiddleCenter);
                y -= RowHeight;
            }

            var inventory = Player.m_localPlayer.GetInventory();
            foreach (var target in targets.Take(MaxRows))
            {
                int have = Carried(inventory, target.Key);
                string color = have >= target.Value ? "#9ccc65" : "#ffb74d";
                Text($"{NameOf(target.Key)}  <color={color}>{have}/{target.Value}</color>", -Width / 2 + 20f, y, 16, Color.white, 230f, TextAnchor.MiddleLeft);
                string prefab = target.Key;
                int step = (SharedOf(prefab)?.m_maxStackSize ?? 1) >= 50 ? 10 : 1;
                step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? step * 10 : step;
                Button("-", 85f, y, 34f, () => Adjust(prefab, -step));
                Button("+", 123f, y, 34f, () => Adjust(prefab, step));
                Button("x", 161f, y, 34f, () => Adjust(prefab, -int.MaxValue / 2));
                y -= RowHeight;
            }

            y -= 10f;
            var field = gui.CreateInputField(_panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y),
                InputField.ContentType.Standard, "Add an item: type part of a name, press Enter", 16, Width - 50f, 30f);
            _search = field.GetComponent<InputField>();
            _search.text = _query;
            _search.onEndEdit.AddListener(text => { _query = text; Rebuild(); });
            y -= RowHeight + 4f;

            foreach (var result in results)
            {
                string prefab = result.Key;
                int amount = Mathf.Min(SharedOf(prefab)?.m_maxStackSize ?? 1, 10);
                Button("+ " + result.Value, 0f, y, Width - 50f, () => { Adjust(prefab, amount); _query = ""; Rebuild(); });
                y -= RowHeight;
            }
        }

        private static List<KeyValuePair<string, string>> SearchResults(List<KeyValuePair<string, int>> targets)
        {
            var results = new List<KeyValuePair<string, string>>();
            string query = (_query ?? "").Trim();
            if (query.Length < 2 || ObjectDB.instance == null) return results;
            foreach (var go in ObjectDB.instance.m_items)
            {
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop == null || targets.Any(t => t.Key == go.name)) continue;
                var shared = drop.m_itemData.m_shared;
                if (shared.m_icons == null || shared.m_icons.Length == 0) continue;
                string label = Localization.instance.Localize(shared.m_name);
                if (label.IndexOf(query, System.StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                results.Add(new KeyValuePair<string, string>(go.name, label));
                if (results.Count >= MaxResults) break;
            }

            return results;
        }

        private static void Text(string text, float x, float y, int size, Color color, float width, TextAnchor anchor)
        {
            var t = GUIManager.Instance.CreateText(text, _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(x + (anchor == TextAnchor.MiddleLeft ? width / 2 : 0f), y), GUIManager.Instance.AveriaSerifBold,
                size, color, true, Color.black, width, RowHeight - 2f, false).GetComponent<Text>();
            t.alignment = anchor;
            t.supportRichText = true;
        }

        private static void Button(string label, float x, float y, float width, System.Action onClick)
        {
            var go = GUIManager.Instance.CreateButton(label, _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), width, 30f);
            go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => onClick());
        }
    }

    /// <summary>Shows the panel while the builder has a vault chest open.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    internal static class PanelShowPatch
    {
        private static void Postfix(Container container)
        {
            if (container != null && VaultChest.IsVault(container) && VaultRules.IsBuilder(container)) VaultPanel.Show(container);
            else VaultPanel.Hide();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class PanelHidePatch
    {
        private static void Postfix() => VaultPanel.Hide();
    }
}
