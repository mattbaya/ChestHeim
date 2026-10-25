# VaultHeim

**A chest whose contents follow you. Every Vault Chest you build, anywhere in the world, holds the same things.**

Put your tools in the Vault Chest at your main base, walk to your mining outpost, open the Vault Chest there, and they're all in it. Take something out at the outpost and it's gone from the base chest too. It's one vault with many doors.

<!-- IMAGE: vault-chest.png (a Vault Chest, with its purple glow, in a base) -->

## Contents

- [Getting started](#getting-started)
- [How it works](#how-it-works)
- [Other players](#other-players)
- [Examples](#examples)
- [Good to know](#good-to-know)
- [Multiplayer](#multiplayer)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Coming next](#coming-next)

## Getting started

1. Open the **Hammer** and go to **Furniture**. Build a **Vault Chest**: 10 Fine Wood, 10 Iron and 5 Surtling Cores.
2. Use it like any chest and put things in. It holds **50 slots**, as many as the biggest chest in the game.
3. Build another Vault Chest somewhere else (another base, an outpost, a boat house). Open it: **it already holds the same things.**

A Vault Chest looks like a black-metal chest with a purple tint, and gives off a faint purple glow, so you can tell it apart.

## Restock

Open your vault chest and a **Restock** panel appears beside it. Add the items you like to carry and how many (search by name, use **+** and **-**, hold Shift for bigger steps), or click **Remember what I carry** to copy what's in your inventory now. Click **Restock** and the vault tops your inventory up to those amounts, taking only what you're short of. Anything the vault doesn't have is reported. The list belongs to your character, so every vault chest shows the same one. The panel can be dragged (double-click it to reset) and has an **X** to close the chest. Only the chest's builder sees it.

## How it works

All of *your* Vault Chests are one vault. Whenever you change what's in one (put something in or take something out), the server copies the new contents to your other Vault Chests, usually within a couple of seconds. It doesn't matter how far apart they are, or whether you're anywhere near the others.

- Your vault is **yours**: it belongs to whoever built the chests. Two players each building Vault Chests have two separate vaults.
- **Only one of your Vault Chests can be open at a time.** If you try to open a second one while the first is still open, it tells you the vault is open elsewhere.
- **Taking a Vault Chest down is safe.** If you have other Vault Chests, the one you remove just goes away, and nothing spills on the ground (that would duplicate your things). When you remove your **last** Vault Chest, its contents drop on the ground like any chest.

## Other players

Anyone can **open** your Vault Chest, look inside, and **put things in**, so a friend can drop you supplies. **Only you, the builder, can take things out.** For anyone else, taking, "Take all" and swapping an item with one in the chest are all refused with a message.

Prefer a vault nobody else can touch? Set `Vault / OthersCanDeposit` to false and only you can open them.

(This take-out rule is enforced in each player's game, like most chest mods. It's meant for friends sharing a server, not for stopping someone determined who modifies their game.)

## Examples

**A travelling toolbox.** Keep your pickaxes, hammer, cultivator and spare armour in your Vault Chest. Whichever base you reach, open the Vault Chest there and everything's in reach.

**Ore without the hauling.** Dump your ore into the Vault Chest at the mine. Open the one at your forge: it's there.

**A drop box for friends.** Tell a friend to put supplies in your Vault Chest. You find them in every one of your chests.

**A safety net.** Every chest holds a full copy, so if one is lost (a raid, a mistake), your things are still in the others.

## Good to know

- **The vault is one set of 50 slots**, however many chests you build. More chests don't add more space.
- **Each chest is a normal chest underneath, with a full copy of the vault.** It saves with the world like any other.
- **The server copies the newest contents** to the rest, so a chest that hasn't been loaded for a while catches up as soon as the server does its check.
- **A newly built chest** first catches up with your vault. It never replaces what's already in it with its own empty contents.

## Multiplayer

Install **VaultHeim on the server as well as every player**. The Vault Chest is a new buildable, so a server without the mod removes it, and the server is also what keeps your chests in step. Versions must match. It works the same in single player, where your game is its own server.

## Configuration

`BepInEx/config/mbaya.VaultHeim.cfg`, or Gale's config editor:

| Section | Key | Default | Notes |
| --- | --- | --- | --- |
| Vault | OthersCanDeposit | true | Others can open your Vault Chests and put things in; only you take things out. False = only you can open them |
| Panel | PanelOffsetX / PanelOffsetY | 0 | Where you dragged the Restock panel (saved automatically) |
| Vault | SyncSeconds | 2 | How often the server copies a vault's contents to your other chests (a server setting) |

## Troubleshooting

- **The Vault Chest isn't in the Hammer menu.** It's under **Furniture**. Check the mod is in the profile you launch from.
- **My second chest is empty.** The server needs a moment, usually a couple of seconds, to copy the contents. Close it and open it again. Check that the mod is on the **server** too: without it, chests aren't kept in step.
- **"This vault is open in another chest."** One of your Vault Chests is open (possibly by you on another screen, or someone else's game). Close it first.
- **"Only its builder can take things out."** The chest belongs to another player. You can put things in, but only they can take things out.
- **Something else.** Open an issue at [github.com/mattbaya/VaultHeim](https://github.com/mattbaya/VaultHeim) and attach `BepInEx/LogOutput.log`.

## Coming next

A **team chest** shared by several players, linked the same way, is planned.

## Works with

- **[Bag O' Heim](https://thunderstore.io/c/valheim/p/Svaha/BagOHeim/)**: packs that refill themselves from chests in your base.
- **[GearHeim](https://thunderstore.io/c/valheim/p/Svaha/GearHeim/)**: loadouts that restock from chests in your base.
- **[SortHeim](https://thunderstore.io/c/valheim/p/Svaha/SortHeim/)**: sort a Vault Chest with its buttons.
