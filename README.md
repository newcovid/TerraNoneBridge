[简体中文](README_CN.md) | [NoneBot2 plugin](https://github.com/newcovid/nonebot-plugin-terralink) | [Protocol specification](https://github.com/newcovid/nonebot-plugin-terralink/blob/master/TerraNoneBridge_Protocol.md)

<div align="center">

# TerraNoneBridge

**A tModLoader mod that connects Terraria servers to NoneBot2 bots**

[![License](https://img.shields.io/github/license/newcovid/TerraNoneBridge.svg)](LICENSE)
[![Steam Workshop](https://img.shields.io/steam/subscriptions/3617766364?label=Steam%20Workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=3617766364)
![tModLoader](https://img.shields.io/badge/tModLoader-2026.08%2B-green.svg)

</div>

## Overview

**TerraNoneBridge** is a tModLoader mod. On a dedicated server it connects as a WebSocket client to the [NoneBot2](https://nonebot.dev/) plugin [nonebot-plugin-terralink](https://github.com/newcovid/nonebot-plugin-terralink), pushes in-game chat, system broadcasts and server data to the bot, and executes the query and administration commands the bot sends.

This repository contains the mod source. Players and server operators should subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3617766364); see the plugin repository for bot-side setup and chat commands.

### Features

- **Two-way chat sync** — relay player chat between the game and a QQ group in real time.
- **Categorized broadcast filters** — system broadcasts are classified as join/leave, deaths, bosses, world events, town NPCs, other mods and server status; each category can be turned off, and keyword blocking is supported.
- **Readable chat text** — item, color and other chat tags are converted to readable text on the server, e.g. `[Legendary Zenith]`.
- **Remote administration** — kick players, save the world, change time, give items and buffs, butcher hostiles, settle liquids.
- **Data queries** — TPS, online players, inventories, boss progression, item details and recipe trees.
- **Safe execution** — state-changing commands can only be issued from the bot side, run on the main thread, and are rejected while the server is unresponsive.

## Installation

1. Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3617766364) and enable the mod on your tModLoader server, or download `TerraNoneBridge.tmod` from [Releases](https://github.com/newcovid/TerraNoneBridge/releases) into the server's `Mods` folder. It is a both-sides mod; players receive it automatically when they join.
2. Install and configure [nonebot-plugin-terralink](https://github.com/newcovid/nonebot-plugin-terralink) in NoneBot2.
3. Edit the server-side mod config (see below) so the address, port and token match the plugin, then restart the server.

## Configuration

The config file is `ModConfigs/TerraNoneBridge_ServerConfig.json` in the tModLoader save directory. It can also be edited from the in-game mod config menu.

| Key | Default | Description |
| --- | --- | --- |
| `IpAddress` | `127.0.0.1` | Host running the NoneBot plugin |
| `Port` | `7778` | Port the NoneBot plugin listens on |
| `AccessToken` | `""` | Authentication token; must match `token` in the plugin's `terralink_links` |
| `ReconnectInterval` | `5` | Reconnect delay in seconds; `0` disables auto-reconnect |
| `EnableChatSync` | `true` | Master switch for two-way player chat |
| `EnableEventBroadcast` | `true` | Master switch for system broadcasts; when off, none of the categories below are sent |
| `BroadcastJoinLeave` | `true` | Player join/leave |
| `BroadcastPlayerDeath` | `true` | Player deaths, including custom death reasons from other mods |
| `BroadcastBoss` | `true` | Boss awakened/defeated and boss warnings |
| `BroadcastWorldEvent` | `true` | Invasions, Blood Moon/eclipse, meteors, world progression, player teleports and other vanilla events |
| `BroadcastTownNpc` | `true` | Town NPC arrivals, deaths and departures |
| `BroadcastOtherMods` | `true` | Server-wide broadcasts from other mods |
| `BroadcastServerStatus` | `true` | Server start/stop notices and console `say` messages |
| `BlockedKeywords` | `[]` | Messages containing any of these keywords are not forwarded (case-insensitive) |
| `CustomExportPath` | `""` | Output directory for `/tnb exportassets`; empty creates a timestamped folder |

Example:

```json
{
  "IpAddress": "127.0.0.1",
  "Port": 7778,
  "AccessToken": "your_secret_token",
  "ReconnectInterval": 5,
  "EnableChatSync": true,
  "EnableEventBroadcast": true,
  "BroadcastOtherMods": false,
  "BlockedKeywords": ["out of bait"]
}
```

Server-side switches decide what leaves the server. Group administrators can filter further per group with `/terralink filter` in the plugin.

## Commands

Use `/tnb <subcommand>` in game, or `tnb <subcommand>` in the server console:

| Subcommand | Description |
| --- | --- |
| `help [command]` | List commands |
| `list` | Online players |
| `tps` | Server performance |
| `boss` | Boss progression |
| `inv <player>` | Player inventory |
| `search <keyword>` | Search items and buffs |
| `query <name/id>` | Item details |
| `recipe <name/id>` | Recipes and usages |
| `time` | Current in-game time |
| `exportassets [all]` | Export item/NPC/buff textures for plugin rendering (single player or host only) |

`kick`, `give`, `buff`, `butcher`, `save`, `settle` and `time set` change server state and can only be executed from the NoneBot side.

## Building from source

1. Clone this repository into tModLoader's `ModSources/TerraNoneBridge` directory.
2. Use **Build + Reload** under **Workshop → Develop Mods** in game, or run `dotnet build -c Release` in the repository directory (requires an installed tModLoader).

`tools/debug_server.py` is a debug server that stands in for the NoneBot side. It shows the packets the mod sends and lets you send test commands without running a bot:

```bash
pip install websockets
python tools/debug_server.py
```

## Repository layout

```text
Core/
  Commands/     Command framework and subcommand implementations
  Config/       Server config (ModConfig)
  Hooks/        Chat, boss and world event hooks
  Net/          WebSocket client and packet definitions
  Services/     Item, player and world data services
  Systems/      Asset export and performance monitoring
  Utils/        Broadcast classification, chat tag conversion and helpers
Localization/   English and Simplified Chinese localization
tools/          Debug tooling
```

## License

This project is licensed under the [GNU General Public License v3.0 only](LICENSE).
