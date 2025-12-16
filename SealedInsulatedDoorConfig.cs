using TUNING;
using UnityEngine;
using PeterHan.PLib.Options;

namespace SealedInsulatedDoor
{
    public class SealedInsulatedDoorConfig : IBuildingConfig
    {
        public const string ID = "SealedInsulatedDoor";

        private readonly int SpeedMultiplier = SingletonOptions<ModSettings>.Instance.SpeedMultiplier;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 1, 2, "SealedInsulatedDoor_kanim", 30, 60f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.ALL_METALS,
                1600f, BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NONE);

            def.ThermalConductivity = 0f;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.IsFoundation = true;
            def.TileLayer = (ObjectLayer)9;
            def.AudioCategory = "Metal";
            def.PermittedRotations = PermittedRotations.R90;
            def.SceneLayer = (Grid.SceneLayer)30;
            def.ForegroundLayer = (Grid.SceneLayer)16;

            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_gear_LP", NOISE_POLLUTION.NOISY.TIER1);
            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_open", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume("door_manual_kanim", "ManualPressureDoor_close", NOISE_POLLUTION.NOISY.TIER2);

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            Door door = go.AddOrGet<Door>();
            door.hasComplexUserControls = true;
            door.poweredAnimSpeed = 1f * SpeedMultiplier;
            door.unpoweredAnimSpeed = 1f * SpeedMultiplier;
            door.doorType = Door.DoorType.ManualPressure;

            go.AddOrGet<ZoneTile>();
            go.AddOrGet<AccessControl>();
            go.AddOrGet<KBoxCollider2D>();
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            Prioritizable.AddRef(go);
            Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<AccessControl>().controlEnabled = true;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
        }
    }
}
