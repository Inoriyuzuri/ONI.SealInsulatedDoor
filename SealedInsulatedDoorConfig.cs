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
                ID, 1, 2, "door_external_kanim", 100, 60f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2, MATERIALS.ALL_METALS,
                2400f, BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER1, NOISE_POLLUTION.NONE);

            def.ThermalConductivity = 0f;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.IsFoundation = true;
            def.TileLayer = ObjectLayer.FoundationTile;
            def.AudioCategory = "Metal";
            def.PermittedRotations = PermittedRotations.R90;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ForegroundLayer = Grid.SceneLayer.InteriorWall;

            SoundEventVolumeCache.instance.AddVolume("door_external_kanim", "Open_DoorPressure", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume("door_external_kanim", "Close_DoorPressure", NOISE_POLLUTION.NOISY.TIER2);

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
