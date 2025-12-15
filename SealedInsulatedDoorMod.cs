using HarmonyLib;
using KMod;

namespace SealedInsulatedDoor
{
    public class SealedInsulatedDoorMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public class GeneratedBuildings_Patch
    {
        public static void Prefix()
        {
            ModUtil.AddBuildingToPlanScreen("Base", SealedInsulatedDoorConfig.ID, "doors", "PressureDoor");
        }
    }

    [HarmonyPatch(typeof(Db), "Initialize")]
    public class Db_Patch
    {
        public static void Postfix()
        {
            var tech = Db.Get().Techs.TryGet("TemperatureModulation");
            tech?.unlockedItemIDs.Add(SealedInsulatedDoorConfig.ID);
        }
    }

    [HarmonyPatch(typeof(Localization), "Initialize")]
    public class Localization_Patch
    {
        public static void Postfix()
        {
            string id = SealedInsulatedDoorConfig.ID.ToUpperInvariant();
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", SealedInsulatedDoorConfig.DisplayName);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", SealedInsulatedDoorConfig.Description);
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", SealedInsulatedDoorConfig.Effect);

            var locale = Localization.GetLocale();
            if (locale != null && locale.Code.StartsWith("zh"))
            {
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", "密封隔热门");
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", "采用先进密封技术的重型门，即使在开启状态下也能保持完美的气体和液体隔离，同时提供绝对的隔热效果。");
                Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", "无论门处于何种状态，都能阻隔所有气体、液体和热量传递。复制人可以自由通过。");
            }
        }
    }
}
