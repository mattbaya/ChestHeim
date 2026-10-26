using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ChestHeim
{
    /// <summary>
    /// The window opened with Shift+E on a chest: write on its label (text, colours, an icon from a searchable list) and, if you
    /// carry a Clan Key and built the chest, share or link it with your clan.
    /// </summary>
    internal static class ChestWindow
    {
        private const int WindowId = 0x7A11;
        private static readonly Color[] Swatches =
        {
            Color.white, new Color(1f, 0.32f, 0.32f), new Color(1f, 0.72f, 0.3f), new Color(1f, 0.93f, 0.35f), new Color(0.61f, 0.8f, 0.39f),
            new Color(0.3f, 0.82f, 0.88f), new Color(0.39f, 0.71f, 0.96f), new Color(0.73f, 0.41f, 0.78f), new Color(0.55f, 0.43f, 0.39f), Color.black,
        };
        private static readonly string[] CommonIcons =
        {
            "Wood", "FineWood", "Stone", "Coal", "Copper", "Tin", "Bronze", "Iron", "BlackMetal", "Silver", "Flametal", "SurtlingCore",
            "Resin", "LeatherScraps", "DeerHide", "Feathers", "Flint", "Honey", "MeadHealthMinor", "CookedMeat", "ArrowWood", "Coins", "Hammer",
        };

        private static Container _chest;
        private static Rect _rect;
        private static int _tab;
        private static string _text = "", _icon = "", _textColor = "", _iconColor = "", _message = "";
        private static string _clanText = "";
        private static bool _picking;
        private static string _query = "";
        private static Vector2 _scroll;
        private static List<(string prefab, string label)> _results = new List<(string, string)>();
        private static CursorLockMode _lockBefore;
        private static bool _visibleBefore;
        private static GUIStyle _preview;

        public static bool IsOpen => _chest != null;
        private static float Scale => Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);

        public static void Open(Container chest)
        {
            _chest = chest;
            var data = ChestLabel.Read(chest);
            _text = data.Text;
            _icon = data.Icon;
            _textColor = data.TextColor;
            _iconColor = data.IconColor;
            _clanText = ClanChest.MyClan;
            _message = "";
            _tab = 0;
            _picking = false;
            _query = "";
            _results = Search("");
            _lockBefore = Cursor.lockState;
            _visibleBefore = Cursor.visible;
            float s = Scale;
            _rect = new Rect((Screen.width - 540f * s) / 2f, (Screen.height - 560f * s) / 2f, 540f * s, 560f * s);
        }

        public static void Close()
        {
            if (_chest == null) return;
            _chest = null;
            Cursor.lockState = _lockBefore;
            Cursor.visible = _visibleBefore;
        }

        private static bool ClanAvailable => _chest != null && VaultRules.IsBuilder(_chest) && ClanChest.HasKey(Player.m_localPlayer);

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
                if (_picking) _picking = false; else Close();
                return;
            }

            float s = Scale;
            GUI.skin.label.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.button.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.textField.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.textArea.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.toggle.fontSize = Mathf.RoundToInt(16f * s);
            if (_preview == null) _preview = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleLeft };
            _preview.fontSize = Mathf.RoundToInt(22f * s);

            GUI.Box(_rect, GUIContent.none);
            _rect = GUI.Window(WindowId, _rect, DrawWindow, "Chest");
        }

        private static void DrawWindow(int id)
        {
            float s = Scale;
            GUILayout.Space(6f * s);
            if (ClanAvailable)
            {
                _tab = GUILayout.Toolbar(_tab, new[] { "Label", "Clan" }, GUILayout.Height(28f * s));
                GUILayout.Space(4f * s);
            }
            else
            {
                _tab = 0;
            }

            if (_tab == 1) DrawClan(s);
            else if (_picking) DrawPicker(s);
            else DrawLabel(s);

            GUILayout.FlexibleSpace();
            if (_message.Length > 0) GUILayout.Label(_message);
            if (!_picking && GUILayout.Button("Close (Esc)", GUILayout.Height(28f * s))) Close();
            GUI.DragWindow();
        }

        // ------------------------------------------------------------------ the label

        private static void DrawLabel(float s)
        {
            GUILayout.Label("Text. Colours: <color=#ff5252>red</color> is written <color=#ff5252>…</color> with tags, and <b>, <i> work too:");
            _text = GUILayout.TextArea(_text, 80, GUILayout.Height(58f * s));
            GUILayout.Space(2f * s);

            GUILayout.Label("Text colour (used where the text has no colour tag of its own):");
            SwatchRow(s, ref _textColor);

            GUILayout.Space(6f * s);
            GUILayout.BeginHorizontal();
            var sprite = ChestLabel.SpriteOf(_icon);
            var box = GUILayoutUtility.GetRect(40f * s, 40f * s, GUILayout.Width(40f * s));
            if (sprite != null) DrawSprite(box, sprite, ChestLabel.ParseColor(_iconColor, Color.white));
            GUILayout.Label(sprite != null ? "Icon: " + ChestLabel.NameOf(_icon) : "No icon");
            if (GUILayout.Button("Choose icon...", GUILayout.Width(150f * s), GUILayout.Height(30f * s)))
            {
                _picking = true;
                _results = Search(_query);
            }

            if (GUILayout.Button("No icon", GUILayout.Width(90f * s), GUILayout.Height(30f * s))) _icon = "";
            GUILayout.EndHorizontal();

            GUILayout.Label("Icon colour:");
            SwatchRow(s, ref _iconColor);

            GUILayout.Space(8f * s);
            GUILayout.Label("Preview:");
            var previewColor = ChestLabel.ParseColor(_textColor, Color.white);
            GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(46f * s));
            var small = GUILayoutUtility.GetRect(36f * s, 36f * s, GUILayout.Width(36f * s));
            if (sprite != null) DrawSprite(small, sprite, ChestLabel.ParseColor(_iconColor, Color.white));
            var old = GUI.contentColor;
            GUI.contentColor = previewColor;
            GUILayout.Label(_text, _preview);
            GUI.contentColor = old;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * s);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save label", GUILayout.Height(32f * s)))
            {
                ChestLabel.Write(_chest, new LabelData { Text = _text.Trim(), Icon = _icon, TextColor = _textColor, IconColor = _iconColor });
                _message = "Saved.";
            }

            if (GUILayout.Button("Remove label", GUILayout.Height(32f * s)))
            {
                ChestLabel.Write(_chest, default);
                _text = _icon = _textColor = _iconColor = "";
                _message = "Label removed.";
            }

            GUILayout.EndHorizontal();
        }

        private static void SwatchRow(float s, ref string hex)
        {
            GUILayout.BeginHorizontal();
            var old = GUI.color;
            foreach (var color in Swatches)
            {
                var rect = GUILayoutUtility.GetRect(26f * s, 22f * s, GUILayout.Width(26f * s));
                GUI.color = color;
                if (GUI.Button(rect, GUIContent.none)) hex = "#" + ColorUtility.ToHtmlStringRGB(color);
            }

            GUI.color = old;
            hex = GUILayout.TextField(hex ?? "", 9, GUILayout.Width(90f * s));
            GUILayout.EndHorizontal();
        }

        private static void DrawSprite(Rect rect, Sprite sprite, Color tint)
        {
            var texture = sprite.texture;
            if (texture == null) return;
            var r = sprite.textureRect;
            var old = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height));
            GUI.color = old;
        }

        // ------------------------------------------------------------------ the icon list

        private static List<(string prefab, string label)> Search(string query)
        {
            var results = new List<(string, string)>();
            query = (query ?? "").Trim();
            if (ObjectDB.instance == null) return results;

            if (query.Length < 2)
            {
                foreach (var prefab in CommonIcons)
                    if (ChestLabel.SpriteOf(prefab) != null) results.Add((prefab, ChestLabel.NameOf(prefab)));
                return results;
            }

            foreach (var go in ObjectDB.instance.m_items)
            {
                if (results.Count >= 40) break;
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop == null || drop.m_itemData.m_shared.m_icons == null || drop.m_itemData.m_shared.m_icons.Length == 0) continue;
                string label = Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
                if (label.IndexOf(query, System.StringComparison.CurrentCultureIgnoreCase) < 0 && go.name.IndexOf(query, System.StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                results.Add((go.name, label));
            }

            return results;
        }

        private static void DrawPicker(float s)
        {
            GUILayout.Label("Search the game's items by name:");
            string before = _query;
            _query = GUILayout.TextField(_query, 30, GUILayout.Height(28f * s));
            if (_query != before) _results = Search(_query);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(340f * s));
            foreach (var (prefab, label) in _results)
            {
                GUILayout.BeginHorizontal();
                var sprite = ChestLabel.SpriteOf(prefab);
                var box = GUILayoutUtility.GetRect(32f * s, 32f * s, GUILayout.Width(32f * s));
                if (sprite != null) DrawSprite(box, sprite, Color.white);
                if (GUILayout.Button(label, GUILayout.Height(32f * s)))
                {
                    _icon = prefab;
                    _picking = false;
                }

                GUILayout.EndHorizontal();
            }

            if (_results.Count == 0) GUILayout.Label("Nothing matches that.");
            GUILayout.EndScrollView();
            if (GUILayout.Button("Back", GUILayout.Height(30f * s))) _picking = false;
        }

        // ------------------------------------------------------------------ the clan

        private static void DrawClan(float s)
        {
            string shared = ClanChest.ClanOf(_chest);
            bool echo = VaultChest.IsVault(_chest);
            GUILayout.Label("Your clan (saved on this character):");
            _clanText = GUILayout.TextField(_clanText, 24, GUILayout.Height(28f * s));
            GUILayout.Space(6f * s);
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
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class ChestWindowTakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ChestWindow.IsOpen) __result = false;
        }
    }

    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class ChestWindowControllerInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (ChestWindow.IsOpen) __result = false;
        }
    }

    /// <summary>Keeps the pointer steady while the chest window is up (the game otherwise takes the mouse back every frame).</summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    internal static class KeepCursorPatch
    {
        private static void Postfix()
        {
            if (!ChestWindow.IsOpen) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
