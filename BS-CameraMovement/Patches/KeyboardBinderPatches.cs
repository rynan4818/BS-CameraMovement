using HarmonyLib;
using HMUI;
using UnityEngine;

namespace BS_CameraMovement.Patches
{
    [HarmonyPatch(typeof(KeyboardBinder), nameof(KeyboardBinder.ManualUpdate))]
    public class KeyboardBinderPatches
    {
        public static bool Prefix()
        {
            // IMGUIのテキストフィールドなどにフォーカスがある場合は、ゲーム側のキー入力を処理しない
            if (GUIUtility.keyboardControl != 0)
            {
                return false;
            }
            return true;
        }
    }
}
