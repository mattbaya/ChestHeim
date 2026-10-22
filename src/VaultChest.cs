using UnityEngine;

namespace VaultHeim
{
    /// <summary>The Vault Chest's identity (its prefab name and the fields it keeps in its saved data) and its look.</summary>
    internal static class VaultChest
    {
        public const string PrefabName = "VaultHeim_Chest";
        public static readonly int PrefabHash = PrefabName.GetStableHashCode();

        // Saved fields (on top of the chest's own "items").
        /// <summary>1 once the server has brought this chest into step with the player's other vault chests.</summary>
        public static readonly int KeyJoined = "vh_joined".GetStableHashCode();
        /// <summary>When the contents last changed (the newest copy wins).</summary>
        public static readonly int KeyStamp = "vh_stamp".GetStableHashCode();
        /// <summary>How many vault chests this player has (written by the server).</summary>
        public static readonly int KeyCount = "vh_count".GetStableHashCode();
        /// <summary>How many of the player's other vault chests are open right now (written by the server).</summary>
        public static readonly int KeyBusy = "vh_busy".GetStableHashCode();

        public static bool IsVault(ZDO zdo) => zdo != null && zdo.GetPrefab() == PrefabHash;

        public static bool IsVault(Container container) =>
            container != null && container.m_nview != null && container.m_nview.IsValid() && IsVault(container.m_nview.GetZDO());

        /// <summary>The same size as the biggest chest in the game (the Wardrobe: 50 slots), a purple tint and a faint light.</summary>
        public static void Customise(GameObject prefab)
        {
            var container = prefab.GetComponent<Container>();
            if (container != null)
            {
                container.m_name = "Vault Chest";
                container.m_width = 5;
                container.m_height = 10;
                container.m_privacy = Container.PrivacySetting.Public;   // access rules are ours (see VaultPatches)
                container.m_autoDestroyEmpty = false;
                container.m_checkGuardStone = false;
            }

            var tint = new Color(0.55f, 0.4f, 1f);
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty("_Color")) continue;
                    var copy = new Material(materials[i]) { name = materials[i].name + "_Vault" };
                    copy.color = Color.Lerp(copy.color, tint, 0.55f);
                    materials[i] = copy;
                }

                renderer.sharedMaterials = materials;
            }

            var glow = new GameObject("VaultGlow");
            glow.transform.SetParent(prefab.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.6f, 0.45f, 1f);
            light.range = 3f;
            light.intensity = 0.7f;
            light.shadows = LightShadows.None;
        }
    }
}
