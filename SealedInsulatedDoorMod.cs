using HarmonyLib;
using KMod;
using System.Collections.Generic;
using UnityEngine;

namespace SealedInsulatedDoor
{
    public class SealedInsulatedDoorMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[SealedInsulatedDoor] Mod loaded successfully!");
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings))]
    [HarmonyPatch(nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public class GeneratedBuildings_LoadGeneratedBuildings_Patch
    {
        public static void Prefix()
        {
            ModUtil.AddBuildingToPlanScreen("Base", SealedInsulatedDoorConfig.ID, "doors", "PressureDoor");
        }
    }

    [HarmonyPatch(typeof(Db))]
    [HarmonyPatch("Initialize")]
    public class Db_Initialize_Patch
    {
        public static void Postfix()
        {
            Tech tech = Db.Get().Techs.TryGet("TemperatureModulation");
            if (tech != null)
            {
                tech.unlockedItemIDs.Add(SealedInsulatedDoorConfig.ID);
            }
        }
    }

    [HarmonyPatch(typeof(Localization))]
    [HarmonyPatch("Initialize")]
    public class Localization_Initialize_Patch
    {
        public static void Postfix()
        {
            string id = SealedInsulatedDoorConfig.ID.ToUpperInvariant();
            
            // Default English
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", SealedInsulatedDoorConfig.DisplayName);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", SealedInsulatedDoorConfig.Description);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", SealedInsulatedDoorConfig.Effect);
            
            // Chinese translation
            if (Localization.GetLocale() != null)
            {
                string code = Localization.GetLocale().Code;
                if (code == "zh_klei" || code == "zh-CN" || code == "zh_CN" || code.StartsWith("zh"))
                {
                    Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", "密封隔热门");
                    Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", "采用先进密封技术的重型门，即使在开启状态下也能保持完美的气体和液体隔离，同时提供绝对的隔热效果。");
                    Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", "无论门处于何种状态，都能阻隔所有气体、液体和热量传递。复制人可以自由通过。");
                }
            }
        }
    }

    [HarmonyPatch(typeof(Door))]
    [HarmonyPatch("SetSimState")]
    public class Door_SetSimState_Patch
    {
        public static void Postfix(Door __instance, bool is_door_open, IList<int> cells)
        {
            if (__instance.gameObject.GetComponent<SealedDoorController>() == null)
                return;

            foreach (int cell in cells)
            {
                if (Grid.IsValidCell(cell))
                {
                    SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.GasImpermeable);
                    SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.LiquidImpermeable);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Door))]
    [HarmonyPatch("Open")]
    public class Door_Open_Patch
    {
        public static void Postfix(Door __instance)
        {
            var sealedController = __instance.gameObject.GetComponent<SealedDoorController>();
            if (sealedController == null)
                return;

            Building building = __instance.gameObject.GetComponent<Building>();
            if (building != null)
            {
                foreach (int cell in building.PlacementCells)
                {
                    if (Grid.IsValidCell(cell))
                    {
                        SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.GasImpermeable);
                        SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.LiquidImpermeable);
                        Grid.Foundation[cell] = true;
                    }
                }
            }
        }
    }
}
