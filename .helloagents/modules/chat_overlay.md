# chat_overlay 模块

## 1. 模块职责

`chat_overlay` 是 `Sts2Speak` 的核心功能模块，负责多人联机聊天链路的完整闭环：

- 注册并处理聊天广播消息
- 缓存聊天记录并向 UI 回放
- 管理聊天框 `Hidden | Preview | Compose | Fading` 状态
- 处理 `Tab` 显隐逻辑与输入提交
- 在战斗中 best effort 复用 `NSpeechBubbleVfx.Create(...)` 显示头顶说话气泡

## 2. 文件组成

| 文件 | 作用 |
|------|------|
| `MainFile.cs` | Mod 入口，初始化 Harmony Patch |
| `Patches/GameBootstrapPatch.cs` | 在 `NGame` / `NRun` / `RunManager` 生命周期中挂接聊天服务 |
| `Messages/ChatBroadcastMessage.cs` | 定义聊天消息字段、序列化与可靠广播行为 |
| `Services/ChatService.cs` | 处理消息收发、去重、历史缓存、显示名解析与气泡复用 |
| `Ui/ChatOverlay.cs` | 渲染聊天框，管理预览、输入态与渐隐状态机 |

## 3. 关键接口

### `ChatBroadcastMessage`

```yaml
字段:
  - MessageId: string
  - SenderId: ulong
  - Text: string
行为:
  - ShouldBroadcast = true
  - Mode = Reliable
```

### `ChatService`

```yaml
初始化:
  - InitializeGlobal()
  - AttachToCurrentRun()
  - DetachFromCurrentRun()
聊天:
  - TrySendChat(string rawText)
  - GetHistory()
  - RefreshOverlayHistory()
```

### `ChatOverlay`

```yaml
显示状态:
  - Hidden
  - Preview
  - Compose
  - Fading
关键方法:
  - RefreshHistory(...)
  - ShowPreview()
  - ShowCompose()
  - HideOverlay()
```

## 4. 运行流程

### 场景一：本地玩家发送消息

```yaml
1. 玩家按 Tab 打开 Compose 状态
2. 在 LineEdit 输入文字并提交
3. ChatService.TrySendChat() 规范化文本并广播 ChatBroadcastMessage
4. 服务层追加本地历史并刷新 ChatOverlay
5. 若战斗上下文可用，则为发送者创建或替换头顶说话气泡
```

### 场景二：远端玩家发言

```yaml
1. NetService 收到 ChatBroadcastMessage
2. ChatService 通过 MessageId 去重并校验 SenderId
3. 追加聊天历史并刷新 ChatOverlay
4. 若当前不是 Compose，则切到 Preview
5. Preview 停留 5 秒后进入 4 秒渐隐
6. 若期间再次收到消息或玩家按 Tab，则取消旧渐隐流程
```

## 5. 设计约束

| 约束 | 说明 |
|------|------|
| 仅多人联机局生效 | 单机或伪联机状态下不启用聊天 |
| 消息长度受服务层截断 | 避免过长文本破坏 UI 与气泡显示 |
| 头顶气泡是 best effort | 仅在战斗、存在 `Creature` 和 `CombatVfxContainer` 时尝试显示 |
| 历史缓存有上限 | 通过 `MaxHistoryCount` 控制内存与显示规模 |

## 6. 已知问题

- 仍需真实多人联机验证广播回环、显示时序和气泡表现
- 当前没有持久化记录与本地化资源文件
- 构建仍存在未处理警告，但不影响 DLL 和 PCK 生成
