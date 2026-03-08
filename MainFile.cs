using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Sts2Speak.Diagnostics;

namespace Sts2Speak;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    internal const string ModId = "Sts2Speak";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        RuntimeTrace.Write("MainFile.Initialize() called.");
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Logger.Info("Sts2Speak initialized.");
        RuntimeTrace.Write("Harmony.PatchAll() completed.");
    }
}
