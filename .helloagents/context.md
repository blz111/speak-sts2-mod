# 项目上下文

## 1. 基本信息

```yaml
名称: Sts2Speak
描述: 为《Slay the Spire 2》多人联机局提供局内文字聊天框与战斗内说话气泡复用
类型: 游戏 Mod / C# / Godot
状态: 已实现，待联机实测
```

## 2. 技术上下文

```yaml
语言: C# / .NET 9
框架: Godot 4.5.1 + Harmony + Slay the Spire 2 官方 Modding API
包管理器: dotnet
构建工具: dotnet build / dotnet publish
```

### 主要依赖

| 依赖 | 版本 | 用途 |
|------|------|------|
| Godot.NET.Sdk | 4.5.1 | 构建 Godot C# Mod 工程 |
| 0Harmony | 游戏目录内置 | 通过 Patch 挂接初始化与运行时生命周期 |
| sts2.dll | 游戏目录内置 | 访问联机、UI、运行时与 VFX API |
| Alchyr.Sts2.ModAnalyzers | 0.0.1 | 提供社区模板的静态分析支持 |

## 3. 项目概述

### 核心功能

- 按 `Tab` 打开或关闭聊天框
- 聊天框上方展示聊天记录，下方提供输入框与发送按钮
- 队友发言时仅展示聊天记录预览，不强制拉起输入框
- 预览停留 5 秒后开始 4 秒渐隐，期间再次按 `Tab` 可立即取消渐隐
- 战斗中 best effort 复用 `NSpeechBubbleVfx.Create(...)` 显示玩家头顶说话气泡

### 项目边界

```yaml
范围内:
  - 多人联机局内公共文字聊天
  - Tab 显隐聊天框
  - 远端消息预览与自动渐隐
  - 战斗内头顶气泡复用
范围外:
  - 私聊、频道、表情或聊天记录持久化
  - 非联机局场景下的聊天支持
  - 非战斗场景下保证存在头顶气泡
  - 本地化资源完善与 UI 美术打磨
```

## 4. 开发约定

### 代码规范

```yaml
命名风格: PascalCase 类型 + camelCase 私有字段
目录组织: 根目录配置 + Messages/ Services/ Ui/ Patches 功能分层
实现原则: 服务层负责消息、历史与气泡，UI 层负责显示状态机与输入交互
```

### 错误处理

```yaml
日志方式: 默认写入 MainFile.Logger
幂等策略: 聊天广播消息用 MessageId 去重
降级行为: 头顶气泡不可用时仅保留聊天记录显示
```

### 测试要求

```yaml
自动化测试: 暂无单元测试框架
构建验证: 已完成 dotnet build 与 dotnet publish -c Release
联机验证: 仍需双人多人局手工验证聊天同步、预览渐隐和战斗气泡
```

## 5. 当前约束

| 约束 | 原因 | 来源 |
|------|------|------|
| 当前聊天仅支持单行消息，最大长度由服务层截断 | 采用 `LineEdit` 作为最小可用输入框 | `Services/ChatService.cs` |
| 头顶说话气泡仅在战斗中、且玩家 `Creature` 可用时尝试显示 | 复用的是战斗内 `NSpeechBubbleVfx` 路径 | `Services/ChatService.cs` |
| 预览/渐隐状态完全由 `ChatOverlay` 本地状态机控制 | 保持服务层和 UI 层职责分离 | `Ui/ChatOverlay.cs` |

## 6. 已知技术债务

| 债务描述 | 优先级 | 建议处理时机 |
|---------|--------|-------------|
| 项目当前仅完成构建与发布验证，缺少真实联机场景的行为确认 | P1 | 下一次多人联机实测 |
| 构建时仍有 `CS0436` 与 `STS002` 警告，暂未影响产物生成 | P2 | 功能稳定后统一处理 |
