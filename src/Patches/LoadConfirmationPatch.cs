using HarmonyLib;
using UnityEngine;

namespace NextStepGuide.Patches
{
    /// <summary>
    /// Attaches the per-colony <see cref="GuideController"/> when a colony's Game
    /// instance spawns. Also our Phase-0 proof that the Harmony pipeline patches
    /// game code, not just that the assembly loaded.
    /// </summary>
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    internal static class LoadConfirmationPatch
    {
        private static void Postfix()
        {
            Debug.Log($"{ModEntry.Prefix} Game.OnSpawn reached — attaching guide controller.");

            var go = Game.Instance != null ? Game.Instance.gameObject : null;
            if (go == null) return;

            if (go.GetComponent<GuideController>() == null)
                go.AddComponent<GuideController>();
        }
    }
}
