using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using Sts2Speak.Services;

namespace Sts2Speak.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGameReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ChatService.InitializeGlobal();
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun._Ready))]
public static class NRunReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ChatService.AttachToCurrentRun();
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
public static class RunManagerCleanUpPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        ChatService.DetachFromCurrentRun();
    }
}
