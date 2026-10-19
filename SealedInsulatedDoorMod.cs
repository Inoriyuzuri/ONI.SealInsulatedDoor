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

    /// <summary>
    /// 修补 vanilla 在 U59 遗留的一个 bug：Door.OVERRIDE_ANIMS 里含 null。
    ///
    /// U59 的 Door 静态构造器是：
    ///     OVERRIDE_ANIMS = new KAnimFile[] { Assets.GetAnim("anim_use_remote_kanim") };
    /// 但 "anim_use_remote_kanim" 这个动画在 U59 的资源表里已经不存在，
    /// Assets.GetAnim 返回 null，于是 OVERRIDE_ANIMS[0] == null。
    ///
    /// Door.OnPrefabInit 会把它整体赋给 Workable.overrideAnims。
    /// 之后复制人对这类门执行"开门/关门"工作时，StandardWorker.AttachOverrideAnims
    /// 会把数组元素逐个传给 KAnimControllerBase.AddAnimOverrides，
    /// 遇到 null 就 Debug.LogError("AddAnimOverrides tried to add a null override to ...").
    ///
    /// 本补丁在不改动游戏文件的前提下，把 null 元素剔除，避免日志刷屏与缺失工作动画。
    /// 对该 mod 自己的门和其它所有 DoorType 门同样生效。
    /// </summary>
    [HarmonyPatch(typeof(Door), "OnPrefabInit")]
    public static class Door_OnPrefabInit_Patch
    {
        public static void Postfix(Door __instance)
        {
            if (__instance == null) return;

            KAnimFile[] anims = __instance.overrideAnims;
            if (anims == null || anims.Length == 0) return;

            List<KAnimFile> clean = null;
            for (int i = 0; i < anims.Length; i++)
            {
                if (anims[i] == null)
                {
                    if (clean == null)
                    {
                        clean = new List<KAnimFile>(anims.Length);
                        for (int j = 0; j < i; j++) clean.Add(anims[j]);
                    }
                }
                else if (clean != null)
                {
                    clean.Add(anims[i]);
                }
            }

            // 没有 null 就不必改动，避免无谓的数组分配。
            if (clean == null) return;

            __instance.overrideAnims = clean.Count == 0 ? null : clean.ToArray();
        }
    }

    /// <summary>
    /// U59 适配说明：
    ///
    /// U59 把 SimMessages.SetCellProperties / ClearCellProperties 的签名从 2 参数改成了 3 参数：
    ///     void SetCellProperties(int gameCell, byte properties, int callbackIdx)
    ///     void ClearCellProperties(int gameCell, byte properties, int callbackIdx)
    /// 旧模组按 2 参数编译，运行时抛 MissingMethodException，导致任何门开关都崩。
    ///
    /// 本补丁改用 3 参数新签名，第三参传 SimMessages.InvalidCallback（= -1，表示不注册回调）。
    ///
    /// 同时修正了旧实现的两个语义错误：
    ///   1) 旧代码 SetInsulation(cell, float.MaxValue) —— 这是**最大导热**，等于完全不隔热。
    ///      正确语义见 vanilla Insulator（隔热砖组件）：
    ///          OnSpawn:   SetInsulation(cell, building.Def.ThermalConductivity)
    ///          OnCleanUp: SetInsulation(cell, 1f)
    ///      即该值就是导热系数，越小越隔热，1f 为普通值。这里用 0.01f（与 vanilla 隔热砖同级）。
    ///   2) 旧代码在 OnCleanUp 里 SetInsulation(cell, 0f) —— 0f 是"完全无导热"的极端值，
    ///      而 vanilla Door.OnCleanUp / Insulator.OnCleanUp 都用 1f 复位。这里改为 1f。
    ///
    /// 挂载点选择 Door.Sim200ms（而非 SetSimState）：
    ///   - Door 本身实现 ISim200ms，Sim200ms 每 200ms 必被调用，是稳定的周期钩子；
    ///   - vanilla 自己就在 Sim200ms 里做"门开后液体刷新"的重置逻辑，说明该位置做周期修正符合原设计意图；
    ///   - 不 patch SetSimState，避免与 sim 层的 Dig / ReplaceAndDisplaceElement 回调交错造成状态不一致。
    ///
    /// 只在门关闭/开启的持久状态上写入，不依赖 SetSimState 的瞬时调用，因此"开门也密封"能真正保持。
    /// </summary>
    [HarmonyPatch(typeof(Door), "Sim200ms")]
    public static class Door_Sim200ms_Patch
    {
        // 目标格子的密封属性：气体不渗透(1) | 液体不渗透(2)
        private const byte SEAL_PROPERTIES = (byte)(Sim.Cell.Properties.GasImpermeable
                                                  | Sim.Cell.Properties.LiquidImpermeable);

        // 与 vanilla 隔热砖（InsulationTile）同级的热导率，越小越隔热。
        private const float SEAL_INSULATION = 0.01f;

        public static void Postfix(Door __instance)
        {
            if (__instance == null) return;

            KPrefabID prefabID = __instance.GetComponent<KPrefabID>();
            if (prefabID == null || prefabID.PrefabTag != SealedInsulatedDoorConfig.ID) return;

            Building building = __instance.building;
            if (building == null) return;

            int[] cells = building.PlacementCells;
            if (cells == null) return;

            for (int i = 0; i < cells.Length; i++)
            {
                int cell = cells[i];
                if (!Grid.IsValidCell(cell)) continue;

                // U59: 三参数签名，第三参用 SimMessages.InvalidCallback (-1) 表示无需回调。
                SimMessages.SetCellProperties(cell, SEAL_PROPERTIES, SimMessages.InvalidCallback);
                SimMessages.SetInsulation(cell, SEAL_INSULATION);
            }
        }
    }

    /// <summary>
    /// 拆除 / 销毁时复位格子属性，避免残留密封状态污染存档。
    /// 复位值严格对齐 vanilla：
    ///   - Door.OnCleanUp 用 ClearCellProperties(cell, 12, -1) 清 SolidImpermeable|Unbreakable，并用 SetInsulation(cell, 1f)
    ///   - Insulator.OnCleanUp 用 SetInsulation(cell, 1f)
    /// </summary>
    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    public static class Door_OnCleanUp_Patch
    {
        private const byte SEAL_PROPERTIES = (byte)(Sim.Cell.Properties.GasImpermeable
                                                  | Sim.Cell.Properties.LiquidImpermeable);

        public static void Prefix(Door __instance)
        {
            if (__instance == null) return;

            KPrefabID prefabID = __instance.GetComponent<KPrefabID>();
            if (prefabID == null || prefabID.PrefabTag != SealedInsulatedDoorConfig.ID) return;

            Building building = __instance.building;
            if (building == null) return;

            int[] cells = building.PlacementCells;
            if (cells == null) return;

            for (int i = 0; i < cells.Length; i++)
            {
                int cell = cells[i];
                if (!Grid.IsValidCell(cell)) continue;

                // U59: 三参数签名
                SimMessages.ClearCellProperties(cell, SEAL_PROPERTIES, SimMessages.InvalidCallback);
                // 复位为普通导热系数（不是 0f）
                SimMessages.SetInsulation(cell, 1f);
            }
        }
    }

    // 注册建筑和本地化
    [HarmonyPatch(typeof(Db), "Initialize")]
    public class Db_Initialize_Patch
    {
        public static void Prefix()
        {
            // Register English first as fallback
            string id = SealedInsulatedDoorConfig.ID.ToUpperInvariant();
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.NAME", "Sealed Insulated Door");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.DESC", "A heavy-duty door with perfect gas, liquid and thermal isolation.");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{id}.EFFECT", "Blocks all gas, liquid, and thermal transfer regardless of door state. Duplicants can pass through freely.");
        }

        public static void Postfix()
        {
            // 解锁到科技树。注意：Temp eratureModulation 在 U59 仍存在（已从语言文件 STRINGS.RESEARCH.TECHS.TEMPERATUREMODULATION 确认）。
            // 用 TryGet + null 检查（不要用 ?. 静默失败，便于排查）。
            Tech tech = Db.Get().Techs.TryGet("TemperatureModulation");
            if (tech != null)
            {
                if (!tech.unlockedItemIDs.Contains(SealedInsulatedDoorConfig.ID))
                    tech.unlockedItemIDs.Add(SealedInsulatedDoorConfig.ID);
            }
            else
            {
                Debug.LogWarning("[SealedInsulatedDoor] Tech 'TemperatureModulation' not found in U59; building may not appear in build menu.");
            }

            // 注册到建造菜单（Base -> doors 子分类，排在 PressureDoor 之后）。
            ModUtil.AddBuildingToPlanScreen("Base", SealedInsulatedDoorConfig.ID, "doors", "PressureDoor");

            // 注意：这里绝对不能调用 Assets.GetBuildingDef / Assets.BuildingDefs。
            // 调用链是 Assets.OnPrefabInit -> Assets.CreatePrefabs -> Db.Get() -> Db.Initialize
            // -> 本 Postfix，此时 Assets.BuildingDefs 仍为 null，访问会抛 NullReferenceException
            // 并中断 Assets.OnPrefabInit，导致所有建筑 prefab 注册失败。

            // Now check locale and override if Chinese
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

