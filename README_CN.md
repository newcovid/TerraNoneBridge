[English](README.md) | [NoneBot2 插件](https://github.com/newcovid/nonebot-plugin-terralink) | [通信协议文档](https://github.com/newcovid/nonebot-plugin-terralink/blob/master/TerraNoneBridge通信文档.md)

<div align="center">

# TerraNoneBridge - 泰拉群服互通

**将 Terraria tModLoader 服务器接入 NoneBot2 机器人的桥接模组**

[![License](https://img.shields.io/github/license/newcovid/TerraNoneBridge.svg)](LICENSE)
[![Steam Workshop](https://img.shields.io/steam/subscriptions/3617766364?label=Steam%20Workshop)](https://steamcommunity.com/sharedfiles/filedetails/?id=3617766364)
![tModLoader](https://img.shields.io/badge/tModLoader-2026.08%2B-green.svg)

</div>

## 概述

**TerraNoneBridge** 是一个 tModLoader 模组。它在专用服务器上以 WebSocket 客户端身份连接 [NoneBot2](https://nonebot.dev/) 插件 [nonebot-plugin-terralink](https://github.com/newcovid/nonebot-plugin-terralink)，把游戏内聊天、系统播报和服务器数据推送到机器人，并执行机器人下发的查询与管理指令。

本仓库是模组源码。普通用户请直接在 [Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3617766364)订阅；机器人端的安装与群内指令请参见插件仓库。

### 主要功能

- **双向聊天同步**：游戏内玩家聊天与 QQ 群消息实时互通。
- **分类播报过滤**：系统播报按玩家进出、死亡、Boss、世界事件、城镇 NPC、其他模组、服务器状态分类，可逐类关闭，并支持关键词屏蔽。
- **可读的聊天文本**：物品、颜色等聊天标签在服务端转换为可读文本，例如 `[传奇 天顶剑]`。
- **远程管理**：踢人、存档、修改时间、给予物品与 Buff、清怪、液体沉降。
- **数据查询**：TPS、在线玩家、背包、Boss 进度、物品详情与合成树。
- **安全执行**：修改性指令仅能由机器人端发起，并在主线程执行；服务器卡顿时自动拒绝。

## 安装

1. 在 [Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3617766364)订阅本模组并在 tModLoader 服务器上启用，或从 [Releases](https://github.com/newcovid/TerraNoneBridge/releases) 下载 `TerraNoneBridge.tmod` 放入服务器的 `Mods` 目录。模组为双端模组，玩家加入服务器时会自动同步。
2. 在 NoneBot2 中安装并配置 [nonebot-plugin-terralink](https://github.com/newcovid/nonebot-plugin-terralink)。
3. 编辑服务器的模组配置（见下文），使地址、端口与 Token 与插件配置一致，然后重启服务器。

## 配置

配置文件位于 tModLoader 存档目录的 `ModConfigs/TerraNoneBridge_ServerConfig.json`，也可以在游戏内的模组配置界面修改。

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `IpAddress` | `127.0.0.1` | NoneBot 插件所在主机地址 |
| `Port` | `7778` | NoneBot 插件监听端口 |
| `AccessToken` | `""` | 鉴权 Token，需与插件 `terralink_links` 中的 `token` 一致 |
| `ReconnectInterval` | `5` | 断线重连间隔（秒），`0` 表示不自动重连 |
| `EnableChatSync` | `true` | 玩家聊天双向互通总开关 |
| `EnableEventBroadcast` | `true` | 系统播报总开关，关闭后下列分类全部不发送 |
| `BroadcastJoinLeave` | `true` | 玩家加入/离开 |
| `BroadcastPlayerDeath` | `true` | 玩家死亡（含其他模组自定义的死亡原因） |
| `BroadcastBoss` | `true` | Boss 苏醒/被打败及 Boss 预警 |
| `BroadcastWorldEvent` | `true` | 入侵、血月/日食、陨石、世界进度、玩家传送等原版事件 |
| `BroadcastTownNpc` | `true` | 城镇 NPC 到达、死亡与离开 |
| `BroadcastOtherMods` | `true` | 其他模组发出的全服播报 |
| `BroadcastServerStatus` | `true` | 服务器启停通知与控制台 `say` 消息 |
| `BlockedKeywords` | `[]` | 包含任一关键词的消息不转发（不区分大小写） |
| `CustomExportPath` | `""` | `/tnb exportassets` 的导出目录，留空则按时间戳新建 |

示例：

```json
{
  "IpAddress": "127.0.0.1",
  "Port": 7778,
  "AccessToken": "your_secret_token",
  "ReconnectInterval": 5,
  "EnableChatSync": true,
  "EnableEventBroadcast": true,
  "BroadcastOtherMods": false,
  "BlockedKeywords": ["自动钓鱼机"]
}
```

服务器端开关从源头决定哪些消息发往机器人；群管理员还可以在 QQ 群中用 `/terralink filter` 按群进一步过滤。

## 指令

游戏内输入 `/tnb <子指令>`，服务器控制台输入 `tnb <子指令>`：

| 子指令 | 说明 |
| --- | --- |
| `help [指令]` | 查看指令列表 |
| `list` | 在线玩家 |
| `tps` | 服务器性能 |
| `boss` | Boss 击杀进度 |
| `inv <玩家>` | 玩家背包 |
| `search <关键词>` | 搜索物品与 Buff |
| `query <名称/ID>` | 物品详情 |
| `recipe <名称/ID>` | 合成表与用途 |
| `time` | 查询当前时间 |
| `exportassets [all]` | 导出物品/NPC/Buff 纹理，供插件渲染图片（仅单人或主机模式） |

`kick`、`give`、`buff`、`butcher`、`save`、`settle` 以及 `time set` 属于修改性指令，只能通过 NoneBot 端执行。

## 从源码构建

1. 将本仓库克隆到 tModLoader 的 `ModSources/TerraNoneBridge` 目录。
2. 在游戏内 **Workshop → Develop Mods** 中点击 **Build + Reload**，或在仓库目录执行 `dotnet build -c Release`（需已安装 tModLoader）。

`tools/debug_server.py` 是一个模拟 NoneBot 端的调试服务器，可在不启动机器人的情况下查看模组发出的数据包并发送测试指令：

```bash
pip install websockets
python tools/debug_server.py
```

## 仓库结构

```text
Core/
  Commands/     指令框架与各子指令实现
  Config/       服务器配置 (ModConfig)
  Hooks/        聊天、Boss、世界事件钩子
  Net/          WebSocket 客户端与数据包定义
  Services/     物品、玩家、世界等数据服务
  Systems/      资源导出、性能监控
  Utils/        播报分类、聊天标签转换等工具
Localization/   中英文本地化
tools/          调试工具
```

## 许可证

本项目基于 [GNU General Public License v3.0 only](LICENSE) 发布。
