using System.Collections.Generic;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace SealedInsulatedDoor
{
    public class SealedInsulatedDoorMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(ModSettings));
        }
    }

    // 密封逻辑：设置气体/液体不透过
    [HarmonyPatch(typeof(Door), "SetSimState")]
    public class Door_SetSimState_Patch
    {
        public static void Postfix(Door __instance, IList<int> cells)
        {
            if (__instance.PrefabID() != SealedInsulatedDoorConfig.ID) return;

            foreach (int cell in cells)
            {
                if (!Grid.IsValidCell(cell)) continue;
                SimMessages.SetInsulation(cell, float.MaxValue);
                SimMessages.SetCellProperties(cell, (byte)(Sim.Cell.Properties.GasImpermeable | Sim.Cell.Properties.LiquidImpermeable));
            }
        }
    }

    // 清理：移除门时恢复格子属性
    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    public class Door_OnCleanUp_Patch
    {
        public static void Prefix(Door __instance)
        {
            if (__instance.PrefabID() != SealedInsulatedDoorConfig.ID) return;

            foreach (int cell in __instance.building.PlacementCells)
            {
                if (!Grid.IsValidCell(cell)) continue;
                SimMessages.ClearCellProperties(cell, (byte)(Sim.Cell.Properties.GasImpermeable | Sim.Cell.Properties.LiquidImpermeable));
                SimMessages.SetInsulation(cell, 0f);
            }
        }
    }

    // 注册建筑和本地化
    [HarmonyPatch(typeof(Db), "Initialize")]
    public class Db_Initialize_Patch
    {
        public static void Prefix()
        {
            string id = SealedInsulatedDoorConfig.ID.ToUpperInvariant();
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", "Sealed Insulated Door");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", "A heavy-duty door with perfect gas, liquid and thermal isolation.");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", "Blocks all gas, liquid, and thermal transfer regardless of door state. Duplicants can pass through freely.");
        }

        public static void Postfix()
        {
            Db.Get().Techs.TryGet("TemperatureModulation")?.unlockedItemIDs.Add(SealedInsulatedDoorConfig.ID);
            ModUtil.AddBuildingToPlanScreen("Base", SealedInsulatedDoorConfig.ID, "doors", "PressureDoor");
        }
    }

    // 中文本地化
    [HarmonyPatch(typeof(Localization), "Initialize")]
    public class Localization_Initialize_Patch
    {
        public static void Postfix()
        {
            var locale = Localization.GetLocale();
            if (locale != null && locale.Code.StartsWith("zh"))
            {
                string id = SealedInsulatedDoorConfig.ID.ToUpperInvariant();
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", "密封隔热门");
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", "采用先进密封技术的重型门，即使在开启状态下也能保持完美的气体和液体隔离，同时提供绝对的隔热效果。");
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", "无论门处于何种状态，都能阻隔所有气体、液体和热量传递。复制人可以自由通过。");
            }
        }
    }
}
