# VaultHeim

A [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) mod for Valheim: a buildable **Vault Chest** whose contents are the same in every Vault Chest its builder has built, anywhere. Thunderstore package: `VaultHeim`.

Player-facing docs: [package/README.md](package/README.md).

> Testing build: not yet tried in the game.

## Layout

| Path | Purpose |
| --- | --- |
| `src/` | Mod source (`VaultHeim.csproj`, targets `netstandard2.1`) |
| `tools/make_icon.py` | Draws the Thunderstore icon (standard library only) |
| `package/` | Thunderstore files |
| `build.sh` | Builds and zips a Thunderstore package into `dist/` |
| `libs/` | Game assemblies and Jötunn to compile against (**git-ignored, never commit**) |

## How it works

The Vault Chest is a Jötunn custom piece cloned from the Black metal chest, resized to the Wardrobe's 5x10 slots. It is an ordinary `Container`, so its contents live in its own saved data (the `items` field). `VaultServer` (runs on the server or host) groups vault chests by builder and copies the newest contents to the others: each chest records `vh_stamp` when its contents change (a postfix on `Container.Save`), the newest joined chest is the source, and chests that are behind and not open get a copy of `items`. The server also writes `vh_count` (how many vault chests the builder has) and `vh_busy` (how many others are open) onto each chest. Players' games use those to refuse opening a second chest while one is open and to skip spilling the vault's contents when one chest of several is dismantled. Taking is restricted to the builder by guarding the inventory GUI's take paths (right-click, drag, Take all).

## Building

Same setup as [Bag O' Heim](https://github.com/mattbaya/BagOHeim#building). Then `./build.sh`.

## Contributing

Bug reports and pull requests are welcome. Please open an issue first for anything larger than a small fix. Releases to Thunderstore are published by the maintainer.
