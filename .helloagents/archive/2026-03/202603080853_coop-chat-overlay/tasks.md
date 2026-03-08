# 任务清单: coop-chat-overlay

> **@status:** completed | 2026-03-08 09:08

```yaml
@feature: coop-chat-overlay
@created: 2026-03-08
@status: completed
@mode: R3
```

<!-- LIVE_STATUS_BEGIN -->
状态: completed | 进度: 12/12 (100%) | 更新: 2026-03-08 09:08:00
当前: 已完成构建、发布、知识库同步与归档准备
<!-- LIVE_STATUS_END -->

## 进度概览

| 完成 | 失败 | 跳过 | 总数 |
|------|------|------|------|
| 12 | 0 | 0 | 12 |

---

## 任务列表

### 1. 项目骨架与运行时入口

- [√] 1.1 参考 `E:/sts2-borrow` 初始化 `Sts2Speak` 工程骨架，创建 `.csproj`、`project.godot`、`mod_manifest.json`、`MainFile.cs` 与基础目录结构。
- [√] 1.2 在 `Patches/GameBootstrapPatch.cs` 中挂接 `NGame._Ready`、`NRun._Ready` 与 `RunManager.CleanUp` 生命周期，为聊天服务提供初始化、对局挂载和清理入口。
  - 依赖: 1.1

### 2. 联机消息协议

- [√] 2.1 在 `Messages/ChatBroadcastMessage.cs` 中定义聊天广播消息字段、序列化逻辑、可靠传输模式与广播行为，确保支持 `MessageId`、`SenderId` 与 `Text`。
  - 依赖: 1.1
- [√] 2.2 在 `Services/ChatService.cs` 中接入消息注册/注销、发送和接收处理链路，完成消息 ID 去重、发送者校验与历史写入入口。
  - 依赖: 2.1, 1.2

### 3. 聊天服务与业务状态

- [√] 3.1 在 `Services/ChatService.cs` 中实现聊天历史缓存、多人联机可用性检查、显示名解析与本地发送流程，并对空消息、长度限制和不可用状态给出稳定处理。
  - 依赖: 2.2
- [√] 3.2 在 `Services/ChatService.cs` 中实现战斗内头顶气泡复用：当玩家 `Creature` 和 `NCombatRoom.Instance.CombatVfxContainer` 可用时，调用 `NSpeechBubbleVfx.Create(...)` 显示或替换当前聊天气泡。
  - 依赖: 3.1

### 4. 聊天 UI 与状态机

- [√] 4.1 在 `Ui/ChatOverlay.cs` 中实现聊天框基础布局：上方聊天记录区、下方输入框/发送入口，并挂载到 `NRun.Instance.GlobalUi`。
  - 依赖: 1.2, 3.1
- [√] 4.2 在 `Ui/ChatOverlay.cs` 中实现 `Hidden | Preview | Compose | Fading` 状态机，打通 `Tab` 显隐、远端消息只显示记录区、输入态焦点获取与阻塞屏幕逻辑。
  - 依赖: 4.1
- [√] 4.3 在 `Ui/ChatOverlay.cs` 中实现 Preview 停留 5 秒 + Fading 4 秒渐隐，以及新消息和 `Tab` 对旧 timer/tween 的取消与重置。
  - 依赖: 4.2

### 5. 集成验证与文档同步

- [√] 5.1 执行构建与基础验证，至少确认项目可编译、聊天主链路无明显语法/引用错误，并记录无法在当前环境自动验证的联机场景。
  - 依赖: 3.2, 4.3
- [√] 5.2 更新 `README.md`，写明功能范围、Tab 用法、头顶气泡复用边界、已知限制和建议的多人手工验证步骤。
  - 依赖: 5.1
- [√] 5.3 更新 `.helloagents` 知识库中的 `INDEX.md`、`context.md`、`modules/_index.md` 与聊天模块文档，确保代码与文档一致。
  - 依赖: 5.2

---

## 执行日志

| 时间 | 任务 | 状态 | 备注 |
|------|------|------|------|
| 2026-03-08 08:53:00 | 方案包初始化 | completed | 已创建 `202603080853_coop-chat-overlay`，待进入开发实施阶段。 |
| 2026-03-08 09:01:00 | Debug 构建验证 | completed | `dotnet build` 通过，生成并复制 `Sts2Speak.dll`。 |
| 2026-03-08 09:05:00 | Release 发布验证 | completed | `dotnet publish -c Release` 通过，已生成 `Sts2Speak.dll` 与 `Sts2Speak.pck`。 |
| 2026-03-08 09:08:00 | 文档与知识库同步 | completed | README 与 `.helloagents` 文档已按当前代码结构补齐。 |

---

## 执行备注

> 本方案面向第一版可用的局内联机聊天体验，优先保证 `Tab` 显隐、远端预览、自动淡出、消息去重与战斗气泡复用链路稳定。
>
> 头顶说话气泡属于 best effort：当前实现目标是在战斗中尽量复用原生 `NSpeechBubbleVfx` 表现；若消息发生在非战斗或玩家 `Creature` 不可用的时机，仍以聊天记录区显示为准。
