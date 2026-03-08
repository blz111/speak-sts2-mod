using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using Sts2Speak.Diagnostics;
using Sts2Speak.Services;

namespace Sts2Speak.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGameReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        RuntimeTrace.Write("NGame._Ready postfix fired.");
        ChatService.InitializeGlobal();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
public static class NGameInputPatch
{
    [HarmonyPostfix]
    public static void Postfix(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        bool isTabPressed = keyEvent.Keycode == Key.Tab
            || keyEvent.PhysicalKeycode == Key.Tab
            || keyEvent.KeyLabel == Key.Tab;
        if (!isTabPressed)
        {
            return;
        }

        RuntimeTrace.Write("NGame._Input received Tab.");
        if (ChatService.ToggleOverlayFromTab())
        {
            NGame.Instance?.GetViewport()?.SetInputAsHandled();
        }
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
public static class NRunReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        RuntimeTrace.Write("NRun._Ready postfix fired.");
        ChatService.AttachToCurrentRun();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        RuntimeTrace.Write("RunManager.CleanUp prefix fired.");
        ChatService.DetachFromCurrentRun();
    }
}
