using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sts2Speak.Services;

namespace Sts2Speak.Ui;

public partial class ChatOverlay : Control
{
    private enum OverlayState
    {
        Hidden,
        Preview,
        Compose,
        Fading
    }

    private PanelContainer _panel = null!;
    private RichTextLabel _historyLabel = null!;
    private HBoxContainer _inputRow = null!;
    private LineEdit _input = null!;
    private Button _sendButton = null!;
    private Tween? _fadeTween;
    private int _previewVersion;
    private OverlayState _state = OverlayState.Hidden;

    public bool IsComposeVisible => _state == OverlayState.Compose;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _panel = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(560f, 260f),
            OffsetLeft = 24f,
            OffsetBottom = -24f
        };
        _panel.SetAnchorsPreset(LayoutPreset.BottomLeft);
        AddChild(_panel);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _panel.AddChild(margin);

        VBoxContainer layout = new();
        layout.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddThemeConstantOverride("separation", 10);
        margin.AddChild(layout);

        _historyLabel = new RichTextLabel
        {
            BbcodeEnabled = false,
            FitContent = false,
            ScrollActive = true,
            ScrollFollowing = true,
            SelectionEnabled = true,
            CustomMinimumSize = new Vector2(0f, 170f)
        };
        _historyLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _historyLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddChild(_historyLabel);

        _inputRow = new HBoxContainer();
        _inputRow.AddThemeConstantOverride("separation", 8);
        layout.AddChild(_inputRow);

        _input = new LineEdit
        {
            PlaceholderText = "输入聊天内容，回车发送"
        };
        _input.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _input.TextSubmitted += _ => SubmitCurrentText();
        _inputRow.AddChild(_input);

        _sendButton = new Button
        {
            Text = "发送"
        };
        _sendButton.Pressed += SubmitCurrentText;
        _inputRow.AddChild(_sendButton);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Tab })
        {
            return;
        }

        HandleTabPressed();
        GetViewport().SetInputAsHandled();
    }

    public void RefreshHistory(IReadOnlyList<ChatService.ChatEntry> entries)
    {
        _historyLabel.Text = entries.Count == 0
            ? "聊天已连接。按 Tab 打开输入框。"
            : string.Join('\n', entries.Select(entry => $"[{entry.Timestamp}] {entry.DisplayName}: {entry.Text}"));
    }

    public void ShowPreview()
    {
        if (IsComposeVisible)
        {
            return;
        }

        CancelTransientVisibility();
        SetPanelAlpha(1f);
        _panel.Visible = true;
        _inputRow.Visible = false;
        _state = OverlayState.Preview;

        int version = ++_previewVersion;
        _ = RunPreviewFadeAsync(version);
    }

    public void ShowCompose()
    {
        if (!ChatService.CanUseChat(out string reason))
        {
            MainFile.Logger.Info(reason);
            HideOverlay();
            return;
        }

        CancelTransientVisibility();
        SetPanelAlpha(1f);
        _panel.Visible = true;
        _inputRow.Visible = true;
        _state = OverlayState.Compose;
        NHotkeyManager.Instance?.AddBlockingScreen(this);
        _input.GrabFocus();
    }

    public void HideOverlay()
    {
        CancelTransientVisibility();
        _state = OverlayState.Hidden;
        _panel.Visible = false;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
    }

    private void HandleTabPressed()
    {
        if (_state == OverlayState.Compose)
        {
            HideOverlay();
            return;
        }

        ShowCompose();
    }

    private async Task RunPreviewFadeAsync(int version)
    {
        SceneTreeTimer delayTimer = GetTree().CreateTimer(5.0);
        await ToSignal(delayTimer, SceneTreeTimer.SignalName.Timeout);
        if (version != _previewVersion || _state != OverlayState.Preview)
        {
            return;
        }

        _state = OverlayState.Fading;
        _fadeTween = CreateTween();
        _fadeTween.TweenMethod(Callable.From<float>(SetPanelAlpha), 1f, 0f, 4.0);
        await ToSignal(_fadeTween, Tween.SignalName.Finished);
        if (version != _previewVersion || _state != OverlayState.Fading)
        {
            return;
        }

        HideOverlay();
    }

    private void CancelTransientVisibility()
    {
        _previewVersion++;
        if (_fadeTween != null && GodotObject.IsInstanceValid(_fadeTween))
        {
            _fadeTween.Kill();
        }

        _fadeTween = null;
        SetPanelAlpha(1f);
    }

    private void SetPanelAlpha(float alpha)
    {
        _panel.Modulate = new Color(1f, 1f, 1f, alpha);
    }

    private void SubmitCurrentText()
    {
        if (!ChatService.TrySendChat(_input.Text))
        {
            return;
        }

        _input.Text = string.Empty;
        _input.GrabFocus();
    }
}
