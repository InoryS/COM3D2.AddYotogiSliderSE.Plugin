using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using HarmonyLib;
using BepInEx.Logging;


namespace COM3D2.AddYotogiSliderSE.Plugin
{
    public class InOutAnimationHook
    {
        static ManualLogSource Logger => AddYotogiSliderSE.Instance.Logger;

        static object ioSettingsInstance;
        static FieldInfo enablePluginFieldInfo;
        static FieldInfo enableMorpherFieldInfo;

        static bool isHooked = false;


        static InOutAnimationHook()
        {
            ReInitialize();
        }

        public static void InOutAnimation_Settings_Load(object __result)
        {
#if DEBUG
            Logger.LogDebug("InOutAnimation settings reloaded");
#endif
            ioSettingsInstance = __result;
        }

        public static void ReInitialize()
        {
            if (isHooked) return;
            var settingsClass = AccessTools.TypeByName("COM3D2.InOutAnimation.Plugin.InOutAnimation+Settings");
            if (settingsClass is null) return;

            Logger.LogInfo("COM3D2.InOutAnimation.Plugin is installed. Hooking into settings");

            var loadMethod = AccessTools.Method(settingsClass, "Load", new Type[] { });
            if (loadMethod is null) throw new Exception("Cannot get load method of settings class");

            var harmony = new Harmony("COM3D2.AddYotogiSliderSE.Plugin.InOutAnimationHook");

            var hookMethod = AccessTools.Method(typeof(InOutAnimationHook), nameof(InOutAnimation_Settings_Load));

            harmony.Patch(
                loadMethod,
                postfix: new HarmonyMethod(hookMethod)
            );

            enablePluginFieldInfo = AccessTools.Field(settingsClass, "enablePlugin");
            if (enablePluginFieldInfo is null)
            {
                throw new Exception($"Cannot get enablePlugin field of {settingsClass}");
            }

            enableMorpherFieldInfo = AccessTools.Field(settingsClass, "enableMorpher");
            if (enableMorpherFieldInfo is null)
            {
                throw new Exception($"Cannot get enableMorpher field of {settingsClass}");
            }

            isHooked = true;
        }

        public static bool IsPluginEnabled
        {
            get
            {
                if (ioSettingsInstance is null) return false;
                return (bool)enablePluginFieldInfo.GetValue(ioSettingsInstance);
            }
        }

        public static bool IsMorpherEnabled
        {
            get
            {
                if (ioSettingsInstance is null) return false;
                return (bool)enableMorpherFieldInfo.GetValue(ioSettingsInstance);
            }
        }
    }
}
