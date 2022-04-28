using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace COM3D2.AddYotogiSliderSE.Plugin
{
    public static class YotogiStageSelectManagerCompat
    {
        static bool Is216 = false;

        // older than 2.16
        static FieldInfo classic_SelectedStageField;
        static FieldInfo classic_SelectedStageRefDayTimeField;

        // 2.16+
        static PropertyInfo v216_SelectedStageProperty;
        static FieldInfo v216_StageExpansionPack_stageDataField;
        static FieldInfo v216_StageExpansionPack_isNoonField;

        static YotogiStageSelectManagerCompat()
        {
            classic_SelectedStageField = AccessTools.Field(typeof(YotogiStageSelectManager), "SelectedStage");
            classic_SelectedStageRefDayTimeField = AccessTools.Field(typeof(YotogiStageSelectManager), "SelectedStageRefDayTime");

            if(classic_SelectedStageField == null)
            {
                Is216 = true;
                v216_SelectedStageProperty = AccessTools.Property(typeof(YotogiStageSelectManager), "SelectedStage");
                v216_StageExpansionPack_isNoonField = AccessTools.Field(v216_SelectedStageProperty.PropertyType, "isNoon");
                v216_StageExpansionPack_stageDataField = AccessTools.Field(v216_SelectedStageProperty.PropertyType, "stageData");
            }
        }

        public static string GetCurrentStagePrefab()
        {
            if (Is216)
            {
                return GetCurrentStagePrefab_216();
            }

            return GetCurrentStagePrefab_Classic();
        }

        static string GetCurrentStagePrefab_Classic()
        {
            var stage = (YotogiStage.Data)classic_SelectedStageField.GetValue(null);
            var daytime = (bool)classic_SelectedStageRefDayTimeField.GetValue(null);
            int i = (daytime) ? 0 : 1;
            return stage.prefabName[i];
        }

        static string GetCurrentStagePrefab_216()
        {
            var selected_stage = v216_SelectedStageProperty.GetValue(null, null);
            var stage = (YotogiStage.Data)v216_StageExpansionPack_stageDataField.GetValue(selected_stage);
            var daytime = (bool)v216_StageExpansionPack_isNoonField.GetValue(selected_stage);

            int i = (daytime) ? 0 : 1;
            return stage.prefabName[i];
        }
    }
}
