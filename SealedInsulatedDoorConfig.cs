using System;
using TUNING;
using UnityEngine;
using PeterHan.PLib.Options;

namespace SealedInsulatedDoor
{
    /// <summary>
    /// U59 配置的密封隔热门。
    ///
    /// 隔热实现（U59 原生机制，无 Harmony patch）：
    ///   1) BuildingDef.ThermalConductivity = 0.01f —— 与 vanilla InsulatedDoorConfig 一致；
    ///   2) Door.insulationModifier = 0.01f —— 这是 U59 隔热门真正的开关：
    ///      Door.SetSimState 里 `if (insulationModifier != 1f) SimMessages.SetInsulation(cell, insulationModifier)`，
    ///      默认值 1f 意味着"完全不隔热"，必须显式设置成 != 1f 的值才会生效。
    ///   3) doorType 保持 ManualPressure，使门仍可开关 / 支持逻辑端口 / 支持 AccessControl，
    ///      与 vanilla InsulatedDoorConfig 完全一致。
    /// </summary>
    public class SealedInsulatedDoorConfig : IBuildingConfig
    {
        public const string ID = "SealedInsulatedDoor";

        // 与 vanilla InsulatedDoorConfig.INSULATION_MODIFIER 相同的取值。
        private const float INSULATION_MODIFIER = 0.01f;
        private const float THERMAL_CONDUCTIVITY = 0.01f;

        // 默认门开关动画速度倍数（可在 PLib 选项里配置）。
        private const int DEFAULT_SPEED_MULTIPLIER = 5;

        // U59: plan screen 子分类名字符串（TUNING.BUILDINGS.PlanSubcategoryName.doors == 3 -> "doors"）。
        private const string PLAN_SUBCATEGORY = "doors";

        /// <summary>
        /// 惰性读取 PLib 配置。不要写成字段初始化器：
        /// IBuildingConfig 由 Db.Initialize 反射实例化，字段初始化器的执行时机依赖 PLib 是否已完成配置加载，
        /// 且 readonly 字段只会求值一次，运行期修改选项后无法刷新。
        /// 这里做完整兜底，任何异常都不会污染 Db.Initialize。
        /// </summary>
        private static int GetSpeedMultiplier()
        {
            try
            {
                ModSettings settings = SingletonOptions<ModSettings>.Instance;
                if (settings != null)
                    return Mathf.Clamp(settings.SpeedMultiplier, 1, 100);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SealedInsulatedDoor] Failed to read ModSettings, falling back to default: " + e);
            }
            return DEFAULT_SPEED_MULTIPLIER;
        }

        /// <summary>
        /// U59 新增的 plan screen 子分类组织校验：PlanBuildingToggle.Config 会用
        /// TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[def.PrefabID] 去查子分类名，
        /// 查不到就 Debug.LogWarning("Building ... has not been added to plan screen subcategory organization in BuildingTuning.cs")
        /// 并且该门的子分类名为空。
        /// ModUtil.AddBuildingToPlanScreen 只写 PLANORDER[category].buildingAndSubcategoryData，不会写这个字典，
        /// 所以模组必须自己把 PrefabID -> 子分类名 注册进去。
        /// </summary>
        private static void RegisterPlanScreenSubcategory()
        {
            // TUNING.BUILDINGS 的静态构造函数会在首次访问任何静态成员时完成填充，
            // 此处访问 PLANSUBCATEGORYSORTING 即保证在填充之后写入。
            if (TUNING.BUILDINGS.PLANSUBCATEGORYSORTING != null &&
                !TUNING.BUILDINGS.PLANSUBCATEGORYSORTING.ContainsKey(ID))
            {
                TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[ID] = PLAN_SUBCATEGORY;
            }
        }

        public override BuildingDef CreateBuildingDef()
        {

            // U59: 必须先补 plan screen 子分类注册，否则 PlanBuildingToggle.Config 会打警告且子分类名为空。
            RegisterPlanScreenSubcategory();

            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 1, 2, "SealedInsulatedDoor_kanim", 30, 60f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.ALL_METALS,
                1600f, BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NONE);

            def.ThermalConductivity = THERMAL_CONDUCTIVITY;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.IsFoundation = true;
            def.TileLayer = (ObjectLayer)9;
            def.AudioCategory = "Metal";
            def.PermittedRotations = PermittedRotations.R90;
            // 渲染层级：必须低于小人的 Move(25) / Creatures(24) 层，否则门会盖在小人身上。
            // vanilla ManualPressureDoor 用 30(TileMain) 是因为它的动画“开门”后会收进门框，
            // 本模组的 kanim 开门时仍有可见像素，用 30 会让门贴图压在小人上方。
            // 这里改用 19(Building) —— 与 vanilla POIBunkerExteriorDoor（同为 1x2 门）一致。
            def.SceneLayer = (Grid.SceneLayer)19;
            def.ForegroundLayer = (Grid.SceneLayer)16;

            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_gear_LP", NOISE_POLLUTION.NOISY.TIER1);
            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_open", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_close", NOISE_POLLUTION.NOISY.TIER2);

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            int speedMultiplier = GetSpeedMultiplier();

            Door door = go.AddOrGet<Door>();
            door.hasComplexUserControls = true;
            door.poweredAnimSpeed = speedMultiplier;
            door.unpoweredAnimSpeed = speedMultiplier;
            // 保持 ManualPressure：门仍可开关、支持逻辑端口与 AccessControl（与 vanilla InsulatedDoorConfig 一致）。
            door.doorType = Door.DoorType.ManualPressure;
            // U59 隔热的关键：默认值 1f 表示"不做隔热设置"，必须设成 != 1f 的值。
            door.insulationModifier = INSULATION_MODIFIER;

            go.AddOrGet<ZoneTile>();
            go.AddOrGet<AccessControl>();
            go.AddOrGet<KBoxCollider2D>();
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            Prioritizable.AddRef(go);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<AccessControl>().controlEnabled = true;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";

            // 注意：这里不再设置 overrideAnims。
            // Door.OnPrefabInit 里已经执行 `overrideAnims = Door.OVERRIDE_ANIMS`（vanilla 自己维护），
            // 模组原先调用的 Assets.GetAnim("anim_use_remote_kanim") 在 U59 资源表里不存在（返回 null），
            // 属于无效代码，已删除。
        }
    }
}

