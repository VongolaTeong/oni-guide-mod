using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using UnityEngine;

namespace NextStepGuide
{
    /// <summary>
    /// Mod entry point. Registered by KMod via the static OnLoad below.
    /// Keep this thin: initialise PLib, apply Harmony patches, log loudly so
    /// failures are greppable in Player.log under the [NextStepGuide] prefix.
    /// </summary>
    public sealed class ModEntry : UserMod2
    {
        /// <summary>Log prefix used everywhere so the mod is greppable in Player.log.</summary>
        public const string Prefix = "[NextStepGuide]";

        public override void OnLoad(Harmony harmony)
        {
            // base.OnLoad applies every [HarmonyPatch] in this assembly.
            base.OnLoad(harmony);

            PUtil.InitLibrary(false);

            Debug.Log($"{Prefix} v0.1.0 loaded — Harmony patches applied " +
                      "(built against U59-737790, Unity 6000.3.5f2).");
        }
    }
}
