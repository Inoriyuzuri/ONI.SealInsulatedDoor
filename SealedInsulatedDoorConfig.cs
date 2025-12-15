using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace SealedInsulatedDoor
{
    public class SealedInsulatedDoorConfig : IBuildingConfig
    {
        public const string ID = "SealedInsulatedDoor";
        public const string DisplayName = "Sealed Insulated Door";
        public const string Description = "A heavy-duty door engineered with advanced sealing technology that maintains perfect gas and liquid isolation even while open, combined with absolute thermal insulation.";
        public const string Effect = "Blocks all gas, liquid, and thermal transfer regardless of door state. Duplicants can pass through freely.";

        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(
                ID, 1, 2, "door_external_kanim", 100, 60f,
                new float[] { 100f }, new string[] { "RefinedMetal" },
                2400f, BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER1, NOISE_POLLUTION.NONE);

            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.IsFoundation = true;
            def.ThermalConductivity = 0f;
            def.AudioCategory = "Metal";
            def.PermittedRotations = PermittedRotations.R90;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.ForegroundLayer = Grid.SceneLayer.InteriorWall;
            def.TileLayer = ObjectLayer.FoundationTile;
            def.ReplacementLayer = ObjectLayer.ReplacementTile;
            def.ReplacementCandidateLayers = new List<ObjectLayer> { ObjectLayer.FoundationTile, ObjectLayer.Backwall };
            def.ReplacementTags = new List<Tag> { GameTags.FloorTiles };
            def.LogicInputPorts = DoorConfig.CreateSingleInputPortList(new CellOffset(0, 0));

            SoundEventVolumeCache.instance.AddVolume("door_external_kanim", "Open_DoorPressure", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume("door_external_kanim", "Close_DoorPressure", NOISE_POLLUTION.NOISY.TIER2);

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            var door = go.AddOrGet<Door>();
            door.hasComplexUserControls = true;
            door.unpoweredAnimSpeed = 0.65f;
            door.doorType = Door.DoorType.ManualPressure;
            door.doorOpeningSoundEventName = "Open_DoorPressure";
            door.doorClosingSoundEventName = "Close_DoorPressure";

            go.AddOrGet<ZoneTile>();
            go.AddOrGet<AccessControl>();
            go.AddOrGet<KBoxCollider2D>();
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            Prioritizable.AddRef(go);
            Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<SealedDoorController>();
            go.AddOrGet<TileTemperature>();
            go.GetComponent<AccessControl>().controlEnabled = true;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
        }
    }
}
