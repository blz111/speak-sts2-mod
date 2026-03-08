using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using Sts2Speak.Messages;
using Sts2Speak.Ui;

namespace Sts2Speak.Services;

public static class ChatService
{
    public readonly record struct ChatEntry(string Timestamp, string DisplayName, string Text, bool IsLocal);

    private const int MaxHistoryCount = 40;
    private const int MaxMessageLength = 140;

    private static readonly HashSet<string> ProcessedMessageIds = new();
    private static readonly List<ChatEntry> History = new();
    private static readonly Dictionary<ulong, NSpeechBubbleVfx?> ActiveBubbles = new();

    private static INetGameService? _registeredNetService;
    private static ChatOverlay? _overlay;
    private static bool _globalInitialized;

    public static IReadOnlyList<ChatEntry> GetHistory()
    {
        return History;
    }

    public static void InitializeGlobal()
    {
        if (_globalInitialized)
        {
            return;
        }

        _globalInitialized = true;
        MainFile.Logger.Info("Sts2Speak global bootstrap complete.");
    }

    public static void AttachToCurrentRun()
    {
        INetGameService? netService = RunManager.Instance.NetService;
        if (netService == null)
        {
            return;
        }

        if (!ReferenceEquals(_registeredNetService, netService))
        {
            DetachFromCurrentRun();
            netService.RegisterMessageHandler<ChatBroadcastMessage>(HandleChatBroadcastMessage);
            _registeredNetService = netService;
            ProcessedMessageIds.Clear();
            History.Clear();
            ActiveBubbles.Clear();
        }

        EnsureOverlay();
        RefreshOverlayHistory();
        MainFile.Logger.Info("Sts2Speak attached to current run.");
    }

    public static void DetachFromCurrentRun()
    {
        if (_registeredNetService != null)
        {
            _registeredNetService.UnregisterMessageHandler<ChatBroadcastMessage>(HandleChatBroadcastMessage);
            _registeredNetService = null;
        }

        foreach ((_, NSpeechBubbleVfx? bubble) in ActiveBubbles)
        {
            if (bubble != null && GodotObject.IsInstanceValid(bubble))
            {
                bubble.QueueFree();
            }
        }

        ActiveBubbles.Clear();
        ProcessedMessageIds.Clear();
        History.Clear();

        if (_overlay != null && GodotObject.IsInstanceValid(_overlay))
        {
            _overlay.QueueFree();
        }

        _overlay = null;
    }

    public static bool CanUseChat(out string reason)
    {
        reason = string.Empty;

        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || NRun.Instance?.GlobalUi == null)
        {
            reason = "当前不在运行中的联机局内。";
            return false;
        }

        if (_registeredNetService == null)
        {
            reason = "当前联机服务不可用。";
            return false;
        }

        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer || runState.Players.Count <= 1)
        {
            reason = "只有真正的多人联机局才能使用聊天。";
            return false;
        }

        return true;
    }

    public static bool TrySendChat(string rawText)
    {
        if (!CanUseChat(out string reason))
        {
            MainFile.Logger.Info(reason);
            return false;
        }

        string text = NormalizeMessage(rawText);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        Player? me = LocalContext.GetMe(runState);
        if (me == null || !LocalContext.NetId.HasValue || _registeredNetService == null)
        {
            MainFile.Logger.Warn("Sts2Speak failed to resolve the local player.");
            return false;
        }

        ChatBroadcastMessage message = new()
        {
            MessageId = Guid.NewGuid().ToString("N"),
            SenderId = me.NetId,
            Text = text
        };

        try
        {
            _registeredNetService.SendMessage(message);
            ProcessedMessageIds.Add(message.MessageId);
            AppendHistory(GetPlayerDisplayName(me), text, true);
            TryShowSpeechBubble(me, text);
            RefreshOverlayHistory();
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to send chat message: {ex}");
            return false;
        }
    }

    public static void RefreshOverlayHistory()
    {
        if (_overlay == null || !GodotObject.IsInstanceValid(_overlay))
        {
            return;
        }

        _overlay.RefreshHistory(History);
    }

    private static ChatOverlay EnsureOverlay()
    {
        if (_overlay != null && GodotObject.IsInstanceValid(_overlay))
        {
            return _overlay;
        }

        _overlay = new ChatOverlay();
        NRun.Instance!.GlobalUi.AddChild(_overlay);
        _overlay.RefreshHistory(History);
        return _overlay;
    }

    private static void HandleChatBroadcastMessage(ChatBroadcastMessage message, ulong senderId)
    {
        if (string.IsNullOrWhiteSpace(message.MessageId) || ProcessedMessageIds.Contains(message.MessageId))
        {
            return;
        }

        ProcessedMessageIds.Add(message.MessageId);

        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
        {
            return;
        }

        Player? sender = runState.Players.FirstOrDefault(player => player.NetId == message.SenderId);
        if (sender == null)
        {
            MainFile.Logger.Warn("Sts2Speak received a message with a missing sender.");
            return;
        }

        if (senderId != sender.NetId)
        {
            MainFile.Logger.Warn($"Sts2Speak sender mismatch. Header={senderId}, payload={sender.NetId}.");
            return;
        }

        string text = NormalizeMessage(message.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        AppendHistory(GetPlayerDisplayName(sender), text, LocalContext.IsMe(sender));
        RefreshOverlayHistory();

        ChatOverlay overlay = EnsureOverlay();
        if (!overlay.IsComposeVisible)
        {
            overlay.ShowPreview();
        }

        TryShowSpeechBubble(sender, text);
    }

    private static void AppendHistory(string displayName, string text, bool isLocal)
    {
        History.Add(new ChatEntry(
            DateTime.Now.ToString("HH:mm"),
            displayName,
            text,
            isLocal));

        if (History.Count > MaxHistoryCount)
        {
            History.RemoveAt(0);
        }
    }

    private static void TryShowSpeechBubble(Player player, string text)
    {
        if (!CombatManager.Instance.IsInProgress || NCombatRoom.Instance?.CombatVfxContainer == null)
        {
            return;
        }

        if (player.Creature == null || player.Creature.IsDead)
        {
            return;
        }

        if (ActiveBubbles.TryGetValue(player.NetId, out NSpeechBubbleVfx? bubble)
            && bubble != null
            && GodotObject.IsInstanceValid(bubble))
        {
            bubble.QueueFree();
        }

        double seconds = Math.Max(1.5, Math.Min(5.5, text.Length * 0.08));
        NSpeechBubbleVfx? newBubble = NSpeechBubbleVfx.Create(text, player.Creature, seconds);
        if (newBubble == null)
        {
            return;
        }

        NCombatRoom.Instance.CombatVfxContainer.AddChild(newBubble);
        ActiveBubbles[player.NetId] = newBubble;
    }

    private static string NormalizeMessage(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        string collapsed = rawText
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (collapsed.Length > MaxMessageLength)
        {
            collapsed = collapsed[..MaxMessageLength];
        }

        return collapsed;
    }

    private static string GetPlayerDisplayName(Player player)
    {
        string playerName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
        return string.IsNullOrWhiteSpace(playerName) ? $"Player {player.NetId}" : playerName;
    }
}
