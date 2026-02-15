using HarmonyLib;
using HMUI;
using UnityEngine;

namespace BS_CameraMovement.HarmonyPatches
{
    [HarmonyPatch(typeof(KeyboardBinder), nameof(KeyboardBinder.ManualUpdate))]
    public class KeyboardBinderManualUpdatePatche
    {
        public static bool IsEditorMode { get; set; } = false;

        public static bool Prefix()
        {
            if (!IsEditorMode)
            {
                return true;
            }
            if (GUIUtility.keyboardControl != 0)
            {
                return false;
            }
            return true;
        }
    }
}
