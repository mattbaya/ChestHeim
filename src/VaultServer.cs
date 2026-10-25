using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChestHeim
{
    /// <summary>
    /// The server's half: it keeps every one of a player's echo chests in step. Each chest keeps its contents in its own saved
    /// data (like any chest), and the server copies the newest copy to the others. Only the server can do that, because it's the
    /// only one that knows about chests far from any player.
    ///
    /// A chest records when its contents last changed. The newest one is the source; any other chest of that player that is
    /// behind (and not open) gets a copy. A newly built chest first catches up with the player's others. The server also writes
    /// each chest how many echo chests that player has and how many of the others are open, which the players' games use to stop
    /// two chests being open at once and to avoid spilling a shared vault's contents on the ground when one chest is taken down.
    /// </summary>
    internal static class VaultServer
    {
        private static float _nextSync;

        public static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        public static void Tick()
        {
            if (!IsServer || ZDOMan.instance == null) return;
            if (Time.time < _nextSync) return;
            _nextSync = Time.time + ChestHeimPlugin.SyncSeconds.Value;

            var groups = new Dictionary<string, List<ZDO>>();
            foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (!VaultChest.IsVault(zdo)) continue;
                // Linked with the builder's other echo chests, or with every echo chest set to the same clan.
                string clan = zdo.GetString(ClanChest.KeyClan, "").Trim().ToLowerInvariant();
                string group = clan.Length > 0 ? "c:" + clan : "b:" + zdo.GetLong(ZDOVars.s_creator);
                if (!groups.TryGetValue(group, out var list)) groups[group] = list = new List<ZDO>();
                list.Add(zdo);
            }

            foreach (var group in groups.Values) SyncGroup(group);
        }

        private static bool IsOpen(ZDO zdo) => zdo.GetInt(ZDOVars.s_inUse) == 1;

        private static void SetIfChanged(ZDO zdo, int key, int value)
        {
            if (zdo.GetInt(key) != value) zdo.Set(key, value);
        }

        private static void SyncGroup(List<ZDO> chests)
        {
            // Bookkeeping the players' games rely on.
            int open = chests.Count(IsOpen);
            foreach (var chest in chests)
            {
                SetIfChanged(chest, VaultChest.KeyCount, chests.Count);
                SetIfChanged(chest, VaultChest.KeyBusy, open - (IsOpen(chest) ? 1 : 0));
            }

            // The chest with the newest contents is the source.
            ZDO source = null;
            long newest = -1;
            foreach (var chest in chests)
            {
                if (chest.GetInt(VaultChest.KeyJoined) != 1) continue;
                long stamp = chest.GetLong(VaultChest.KeyStamp);
                if (stamp > newest)
                {
                    newest = stamp;
                    source = chest;
                }
            }

            if (source == null)
            {
                // Nothing has been set up yet (the player's first echo chest): it simply starts the vault.
                foreach (var chest in chests) Join(chest, 0L);
                return;
            }

            byte[] items = source.GetByteArray(ZDOVars.s_items);
            foreach (var chest in chests)
            {
                if (chest == source || IsOpen(chest)) continue;

                bool behind = chest.GetInt(VaultChest.KeyJoined) != 1 || chest.GetLong(VaultChest.KeyStamp) < newest;
                if (!behind) continue;

                Copy(chest, items, newest);
            }
        }

        private static void Join(ZDO chest, long stamp)
        {
            if (chest.GetInt(VaultChest.KeyJoined) == 1) return;
            Write(chest, null, stamp);
        }

        private static void Copy(ZDO chest, byte[] items, long stamp) => Write(chest, items, stamp);

        /// <summary>Writes a chest's contents (when there are any to write) and marks it as part of the vault.</summary>
        private static void Write(ZDO chest, byte[] items, long stamp)
        {
            chest.SetOwner(ZDOMan.GetSessionID());
            if (items != null && items.Length > 0) chest.Set(ZDOVars.s_items, items);
            chest.Set(VaultChest.KeyJoined, 1);
            chest.Set(VaultChest.KeyStamp, stamp);
            // Let a player's game take charge of the chest again when it's near it.
            chest.SetOwner(0L);
            ZDOMan.instance.ForceSendZDO(chest.m_uid);
        }
    }
}
