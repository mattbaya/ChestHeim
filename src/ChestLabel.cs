using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace ChestHeim
{
    internal enum LabelSide { Front, Right, Back, Left }

    /// <summary>What a chest's label says and looks like. It's kept in the chest's own saved data, so everyone sees it.</summary>
    internal struct LabelData
    {
        public string Text, Icon, TextColor, IconColor;
        public bool Empty => string.IsNullOrEmpty(Text) && string.IsNullOrEmpty(Icon);
        public string Signature => Text + "|" + Icon + "|" + TextColor + "|" + IconColor;
    }

    internal static class ChestLabel
    {
        private static readonly int KeyText = "ch_ltext".GetStableHashCode();
        private static readonly int KeyIcon = "ch_licon".GetStableHashCode();
        private static readonly int KeyTextColor = "ch_lcolor".GetStableHashCode();
        private static readonly int KeyIconColor = "ch_licolor".GetStableHashCode();

        /// <summary>Chests that players build: not ships, carts or gravestones.</summary>
        public static bool Labelable(Container chest) =>
            chest != null && chest.m_nview != null && chest.m_nview.IsValid() && chest.GetComponent<Piece>() != null
            && Utils.GetPrefabName(chest.gameObject).ToLowerInvariant().Contains("chest");

        public static LabelData Read(Container chest)
        {
            var zdo = chest.m_nview.GetZDO();
            return new LabelData
            {
                Text = zdo.GetString(KeyText, ""),
                Icon = zdo.GetString(KeyIcon, ""),
                TextColor = zdo.GetString(KeyTextColor, ""),
                IconColor = zdo.GetString(KeyIconColor, ""),
            };
        }

        public static void Write(Container chest, LabelData data)
        {
            if (!Labelable(chest)) return;
            chest.m_nview.ClaimOwnership();
            var zdo = chest.m_nview.GetZDO();
            zdo.Set(KeyText, data.Text ?? "");
            zdo.Set(KeyIcon, data.Icon ?? "");
            zdo.Set(KeyTextColor, data.TextColor ?? "");
            zdo.Set(KeyIconColor, data.IconColor ?? "");
        }

        public static Color ParseColor(string hex, Color fallback) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c) ? c : fallback;

        public static string Plain(string text) => Regex.Replace(text ?? "", "<[^>]+>", "");

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>An item's own icon (or a piece's), by prefab name.</summary>
        public static Sprite SpriteOf(string prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return null;
            if (Sprites.TryGetValue(prefab, out var cached) && cached != null) return cached;

            Sprite sprite = null;
            var item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = item != null ? item.GetComponent<ItemDrop>() : null;
            if (drop != null && drop.m_itemData.m_shared.m_icons != null && drop.m_itemData.m_shared.m_icons.Length > 0)
                sprite = drop.m_itemData.m_shared.m_icons[0];
            if (sprite == null && ZNetScene.instance != null)
            {
                var go = ZNetScene.instance.GetPrefab(prefab);
                var piece = go != null ? go.GetComponent<Piece>() : null;
                if (piece != null) sprite = piece.m_icon;
            }

            if (sprite != null) Sprites[prefab] = sprite;
            return sprite;
        }

        public static string NameOf(string prefab)
        {
            var item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : prefab;
        }
    }

    /// <summary>The sign on the front of a chest: an icon and some text on a dark plate, drawn in the world.</summary>
    internal class ChestLabelView : MonoBehaviour
    {
        private Container _chest;
        private string _signature;
        private GameObject _root;
        private float _next;
        private TextMesh _fitText;
        private float _fitWidth, _fitHeight;
        private int _fitTries;

        private void Awake() => _chest = GetComponent<Container>();

        private void Update()
        {
            if (_fitText != null) TryFit();
            if (Time.time < _next) return;
            _next = Time.time + 0.5f;
            Refresh();
        }

        private void Clear()
        {
            if (_root != null) Destroy(_root);
            _root = null;
            _fitText = null;
        }

        private void OnDestroy() => Clear();

        private void Refresh()
        {
            if (_chest == null || _chest.m_nview == null || !_chest.m_nview.IsValid() || !ChestHeimPlugin.LabelsEnabled.Value)
            {
                _signature = null;
                Clear();
                return;
            }

            var data = ChestLabel.Read(_chest);
            string signature = data.Signature + "|" + ChestHeimPlugin.LabelSideSetting.Value;
            if (signature == _signature) return;
            _signature = signature;
            Clear();
            if (!data.Empty) Build(data);
        }

        private void Build(LabelData data)
        {
            // The chest's size, in its own space, from what it's made of.
            Bounds? bounds = null;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is TrailRenderer || renderer is LineRenderer || renderer.GetType().Name.Contains("Particle")) continue;
                var b = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one);
                    var local = transform.InverseTransformPoint(corner);
                    if (bounds == null) bounds = new Bounds(local, Vector3.zero); else { var e = bounds.Value; e.Encapsulate(local); bounds = e; }
                }
            }

            if (bounds == null) return;
            var box = bounds.Value;

            var side = ChestHeimPlugin.LabelSideSetting.Value;
            Vector3 n = side == LabelSide.Front ? Vector3.forward : side == LabelSide.Back ? Vector3.back : side == LabelSide.Right ? Vector3.right : Vector3.left;
            bool alongX = side == LabelSide.Front || side == LabelSide.Back;
            float extent = alongX ? (n.z > 0 ? box.max.z : -box.min.z) : (n.x > 0 ? box.max.x : -box.min.x);
            var position = new Vector3(box.center.x, box.min.y + box.size.y * 0.42f, box.center.z);
            position += n * (extent + 0.015f);

            float width = Mathf.Max(0.3f, (alongX ? box.size.x : box.size.z) * 0.85f);
            float row = Mathf.Clamp(width * 0.3f, 0.12f, 0.3f);

            _root = new GameObject("ChestLabel");
            _root.transform.SetParent(transform, false);
            _root.transform.localPosition = position;
            // Facing out, so it reads from the front.
            _root.transform.localRotation = Quaternion.LookRotation(-n, Vector3.up);

            AddPlate(_root.transform, width, row + 0.06f);

            var sprite = ChestLabel.SpriteOf(data.Icon);
            float textLeft = -width / 2f + 0.03f;
            if (sprite != null)
            {
                var iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(_root.transform, false);
                var sr = iconObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = ChestLabel.ParseColor(data.IconColor, Color.white);
                float biggest = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                iconObject.transform.localScale = Vector3.one * (row / Mathf.Max(0.0001f, biggest));
                iconObject.transform.localPosition = new Vector3(-width / 2f + row / 2f + 0.03f, 0f, -0.004f);
                textLeft = -width / 2f + row + 0.07f;
            }

            if (!string.IsNullOrEmpty(data.Text)) AddText(_root.transform, data, sprite != null, textLeft, width, row);
        }

        private static void AddPlate(Transform parent, float width, float height)
        {
            var plate = new GameObject("Plate");
            plate.transform.SetParent(parent, false);
            var mesh = new Mesh { name = "ChestLabelPlate" };
            mesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
            mesh.triangles = new[] { 0, 2, 3, 0, 3, 1 };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.RecalculateNormals();
            plate.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = plate.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.07f, 0.05f, 0.04f, 0.82f) };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            plate.transform.localScale = new Vector3(width, height, 1f);
        }

        private void AddText(Transform parent, LabelData data, bool hasIcon, float left, float width, float row)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = GUIManager.Instance != null ? GUIManager.Instance.AveriaSerifBold : Resources.GetBuiltinResource<Font>("Arial.ttf");
            mesh.richText = true;
            mesh.text = data.Text;
            mesh.fontSize = 64;
            mesh.characterSize = 0.01f;
            mesh.color = ChestLabel.ParseColor(data.TextColor, Color.white);
            mesh.anchor = hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            mesh.alignment = hasIcon ? TextAlignment.Left : TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float available = hasIcon ? width / 2f - 0.03f - left : width - 0.06f;
            go.transform.localPosition = new Vector3(hasIcon ? left : 0f, 0f, -0.004f);
            // The text's real size is only known once it has been built, so it's fitted in the next frames.
            _fitText = mesh;
            _fitWidth = available;
            _fitHeight = row * Mathf.Max(1, data.Text.Split('\n').Length);
            _fitTries = 0;
        }

        private void TryFit()
        {
            var size = _fitText.GetComponent<MeshRenderer>().bounds.size;
            // Seen from the side the text is thin, so use the larger of its two flat sizes.
            float w = Mathf.Max(size.x, size.z), h = size.y;
            if ((w <= 0.0001f || h <= 0.0001f) && ++_fitTries < 20) return;
            if (w > 0.0001f && h > 0.0001f)
            {
                float scale = Mathf.Min(_fitWidth / w, (_fitHeight * 0.85f) / h);
                _fitText.transform.localScale = Vector3.one * scale;
            }

            _fitText = null;
        }
    }

    /// <summary>Gives every chest a label view, and a plain-text name when you look at it.</summary>
    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class ChestLabelAwakePatch
    {
        private static void Postfix(Container __instance)
        {
            if (__instance.GetComponent<ChestLabelView>() != null) return;
            if (__instance.GetComponent<Piece>() == null || !Utils.GetPrefabName(__instance.gameObject).ToLowerInvariant().Contains("chest")) return;
            __instance.gameObject.AddComponent<ChestLabelView>();
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class ChestLabelHoverPatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (!ChestHeimPlugin.LabelsEnabled.Value || !ChestLabel.Labelable(__instance)) return;
            string text = ChestLabel.Plain(ChestLabel.Read(__instance).Text).Replace("\n", " ").Trim();
            if (text.Length == 0) return;
            string name = Localization.instance.Localize(__instance.m_name);
            int at = __result.IndexOf(name, System.StringComparison.Ordinal);
            if (at >= 0) __result = __result.Substring(0, at) + text + __result.Substring(at + name.Length);
        }
    }
}
