# Sts2Speak 知识库

> 本文件是知识库入口，描述项目导航、核心模块与当前状态。

## 快速导航

| 需要了解 | 读取文件 |
|---------|---------|
| 项目概况、技术栈、开发约定 | [context.md](context.md) |
| 模块索引 | [modules/_index.md](modules/_index.md) |
| 聊天功能主模块 | [modules/chat_overlay.md](modules/chat_overlay.md) |
| 变更历史 | [CHANGELOG.md](CHANGELOG.md) |
| 历史方案索引 | [archive/_index.md](archive/_index.md) |
| 当前待执行方案 | [plan/](plan/) |

## 模块关键词索引

| 模块 | 关键词 | 摘要 |
|------|--------|------|
| chat_overlay | 聊天框, Tab, 预览, 渐隐, 气泡, 联机消息 | 管理聊天消息广播、聊天记录 UI、远端预览态和战斗内头顶说话气泡复用。 |

## 知识库状态

```yaml
kb_version: unknown
最后更新: 2026-03-08 09:00
模块数量: 1
待执行方案: 0
```

## 读取指引

```yaml
启动任务:
  - 先读取本文件获取导航
  - 再读取 context.md 获取项目上下文
  - 若回顾本次功能实现，优先检查 archive/2026-03/202603080853_coop-chat-overlay/

任务相关:
  - 涉及聊天框、Tab、预览渐隐或头顶气泡: 读取 modules/chat_overlay.md
  - 涉及本次实现方案: 读取 archive/2026-03/202603080853_coop-chat-overlay/*
```
