using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sts2Speak.Diagnostics;
using Sts2Speak.Services;

namespace Sts2Speak.Ui;

public partial class ChatOverlay : Control
{
    private const float PanelMargin = 24f;
    private const float PanelMaxWidth = 560f;
    private const float PanelMaxHeight = 260f;
    private const float PanelMinWidth = 320f;
    private const float PanelMinHeight = 180f;

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
        TopLevel = true;
        ZAsRelative = false;
        ZIndex = 4096;
        MouseFilter = MouseFilterEnum.Ignore;

        _panel = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(PanelMinWidth, PanelMinHeight)
        };
        _panel.SetAnchorsPreset(LayoutPreset.TopLeft);
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

        Viewport viewport = GetViewport();
        if (viewport != null)
        {
            viewport.SizeChanged += UpdateLayout;
        }

        UpdateLayout();
        RuntimeTrace.Write("ChatOverlay._Ready(): layout initialized.");
    }

    public override void _ExitTree()
    {
        Viewport viewport = GetViewport();
        if (viewport != null)
        {
            viewport.SizeChanged -= UpdateLayout;
        }

        base._ExitTree();
    }

    public void RefreshHistory(IReadOnlyList<ChatService.ChatEntry> entries)
    {
        _historyLabel.Text = entries.Count == 0
            ? "按 Tab 打开聊天框。"
            : string.Join('\n', entries.Select(entry => $"[{entry.Timestamp}] {entry.DisplayName}: {entry.Text}"));
    }

    public void ShowPreview()
    {
        if (IsComposeVisible)
        {
            return;
        }

        CancelTransientVisibility();
        UpdateLayout();
        MoveToFront();
        _panel.MoveToFront();
        SetPanelAlpha(1f);
        _panel.Visible = true;
        _inputRow.Visible = false;
        _state = OverlayState.Preview;
        RuntimeTrace.Write("ChatOverlay.ShowPreview() called.");

        int version = ++_previewVersion;
        _ = RunPreviewFadeAsync(version);
    }

    public void ShowCompose()
    {
        CancelTransientVisibility();
        UpdateLayout();
        MoveToFront();
        _panel.MoveToFront();
        SetPanelAlpha(1f);
        _panel.Visible = true;
        _inputRow.Visible = true;
        _state = OverlayState.Compose;
        NHotkeyManager.Instance?.AddBlockingScreen(this);
        _input.GrabFocus();
        RuntimeTrace.Write("ChatOverlay.ShowCompose() called.");
    }

    public void HideOverlay()
    {
        CancelTransientVisibility();
        _state = OverlayState.Hidden;
        _panel.Visible = false;
        NHotkeyManager.Instance?.RemoveBlockingScreen(this);
        RuntimeTrace.Write("ChatOverlay.HideOverlay() called.");
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

    public void ToggleCompose()
    {
        RuntimeTrace.Write($"ChatOverlay.ToggleCompose() called while state={_state}.");
        HandleTabPressed();
    }

    private void UpdateLayout()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float panelWidth = Mathf.Min(PanelMaxWidth, Mathf.Max(PanelMinWidth, viewportSize.X - PanelMargin * 2f));
        float panelHeight = Mathf.Min(PanelMaxHeight, Mathf.Max(PanelMinHeight, viewportSize.Y - PanelMargin * 2f));

        _panel.Position = new Vector2(PanelMargin, Mathf.Max(PanelMargin, viewportSize.Y - panelHeight - PanelMargin));
        _panel.Size = new Vector2(panelWidth, panelHeight);
        RuntimeTrace.Write($"ChatOverlay.UpdateLayout(): viewport={viewportSize}, panelPos={_panel.Position}, panelSize={_panel.Size}.");
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
