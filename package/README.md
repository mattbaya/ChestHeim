# EchoChests

**A chest whose contents follow you. Every Echo Chest you build, anywhere in the world, holds the same things.**

Put your tools in the Echo Chest at your main base, walk to your mining outpost, open the Echo Chest there, and they're all in it. Take something out at the outpost and it's gone from the base chest too. It's one stash with many doors.

<!-- IMAGE: echo-chest.png (an Echo Chest, with its purple glow, in a base) -->

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

1. Open the **Hammer** and go to **Furniture**. Build a **Echo Chest**: 10 Fine Wood, 10 Iron and 5 Surtling Cores.
2. Use it like any chest and put things in. It holds **50 slots**, as many as the biggest chest in the game.
3. Build another Echo Chest somewhere else (another base, an outpost, a boat house). Open it: **it already holds the same things.**

An Echo Chest looks like a black-metal chest with a purple tint, and gives off a faint purple glow, so you can tell it apart.

## Clan chests

Share one set of echo chests with your whole clan. Craft a **Clan Key** at a workbench (2 Bronze, 2 Fine Wood). Carry it, look at an Echo Chest you built, and press **Shift+E**. Type your clan name and press **Link this chest with my clan**. From then on that chest shows the same things as every other Echo Chest set to that clan, wherever those were built: build one at an outpost, link it to the clan, and it's connected back to the home chest. Everyone whose own clan name matches (they set it the same way, with a key of their own, using *Just save my clan name*) can open those chests and take things out; nobody else can open them. The chest's own previous contents are replaced by the clan's when it joins, so link an empty chest. **Back to my own echo chests** returns it to your personal set.

The key also works on an ordinary chest you built: **Share this chest with my clan** makes it open only to you and your clan (normally anyone can open an ordinary chest unless a ward covers it). Names aren't case-sensitive. The rules are checked in each player's game, so use it among people you trust.

## Restock

Open your echo chest and a **Restock** panel appears beside it. Add the items you like to carry and how many (search by name, use **+** and **-**, hold Shift for bigger steps), or click **Remember what I carry** to copy what's in your inventory now. Click **Restock** and the stash tops your inventory up to those amounts, taking only what you're short of. Anything the stash doesn't have is reported. The list belongs to your character, so every echo chest shows the same one. The panel can be dragged (double-click it to reset) and has an **X** to close the chest. Only the chest's builder sees it.

## How it works

All of *your* Echo Chests are one stash. Whenever you change what's in one (put something in or take something out), the server copies the new contents to your other Echo Chests, usually within a couple of seconds. It doesn't matter how far apart they are, or whether you're anywhere near the others.

- Your stash is **yours**: it belongs to whoever built the chests. Two players each building Echo Chests have two separate stashes.
- **Only one of your Echo Chests can be open at a time.** If you try to open a second one while the first is still open, it tells you the stash is open elsewhere.
- **Taking an Echo Chest down is safe.** If you have other Echo Chests, the one you remove just goes away, and nothing spills on the ground (that would duplicate your things). When you remove your **last** Echo Chest, its contents drop on the ground like any chest.

## Other players

Anyone can **open** your Echo Chest, look inside, and **put things in**, so a friend can drop you supplies. **Only you, the builder, can take things out.** For anyone else, taking, "Take all" and swapping an item with one in the chest are all refused with a message.

Prefer a stash nobody else can touch? Set `Echo / OthersCanDeposit` to false and only you can open them.

(This take-out rule is enforced in each player's game, like most chest mods. It's meant for friends sharing a server, not for stopping someone determined who modifies their game.)

## Examples

**A travelling toolbox.** Keep your pickaxes, hammer, cultivator and spare armour in your Echo Chest. Whichever base you reach, open the Echo Chest there and everything's in reach.

**Ore without the hauling.** Dump your ore into the Echo Chest at the mine. Open the one at your forge: it's there.

**A drop box for friends.** Tell a friend to put supplies in your Echo Chest. You find them in every one of your chests.

**A safety net.** Every chest holds a full copy, so if one is lost (a raid, a mistake), your things are still in the others.

## Good to know

- **The stash is one set of 50 slots**, however many chests you build. More chests don't add more space.
- **Each chest is a normal chest underneath, with a full copy of the stash.** It saves with the world like any other.
- **The server copies the newest contents** to the rest, so a chest that hasn't been loaded for a while catches up as soon as the server does its check.
- **A newly built chest** first catches up with your stash. It never replaces what's already in it with its own empty contents.

## Multiplayer

Install **EchoChests on the server as well as every player**. The Echo Chest is a new buildable, so a server without the mod removes it, and the server is also what keeps your chests in step. Versions must match. It works the same in single player, where your game is its own server.

## Configuration

`BepInEx/config/mbaya.EchoChests.cfg`, or Gale's config editor:

| Section | Key | Default | Notes |
| --- | --- | --- | --- |
| Echo | OthersCanDeposit | true | Others can open your Echo Chests and put things in; only you take things out. False = only you can open them |
| Panel | PanelOffsetX / PanelOffsetY | 0 | Where you dragged the Restock panel (saved automatically) |
| Echo | SyncSeconds | 2 | How often the server copies a stash's contents to your other chests (a server setting) |

## Troubleshooting

- **The Echo Chest isn't in the Hammer menu.** It's under **Furniture**. Check the mod is in the profile you launch from.
- **My second chest is empty.** The server needs a moment, usually a couple of seconds, to copy the contents. Close it and open it again. Check that the mod is on the **server** too: without it, chests aren't kept in step.
- **"This stash is open in another chest."** One of your Echo Chests is open (possibly by you on another screen, or someone else's game). Close it first.
- **"Only its builder can take things out."** The chest belongs to another player. You can put things in, but only they can take things out.
- **Something else.** Open an issue at [github.com/mattbaya/EchoChests](https://github.com/mattbaya/EchoChests) and attach `BepInEx/LogOutput.log`.

## Coming next

A **team chest** shared by several players, linked the same way, is planned.

## Works with

- **[Bag O' Heim](https://thunderstore.io/c/valheim/p/Svaha/BagOHeim/)**: packs that refill themselves from chests in your base.
- **[GearHeim](https://thunderstore.io/c/valheim/p/Svaha/GearHeim/)**: loadouts that restock from chests in your base.
- **[SortHeim](https://thunderstore.io/c/valheim/p/Svaha/SortHeim/)**: sort an Echo Chest with its buttons.
