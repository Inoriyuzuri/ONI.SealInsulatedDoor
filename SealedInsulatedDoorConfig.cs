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
            string id = ID;
            int width = 1;
            int height = 2;
            string anim = "door_external_kanim";
            int hitpoints = 100;
            float construction_time = 60f;
            float[] tier = { 5f };
            string[] materials = { "Metal" };
            float melting_point = 2400f;
            BuildLocationRule build_location_rule = BuildLocationRule.Tile;
            EffectorValues none = NOISE_POLLUTION.NONE;
            
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id, width, height, anim, hitpoints, construction_time,
                tier, materials, melting_point, build_location_rule,
                BUILDINGS.DECOR.PENALTY.TIER1, none);

            buildingDef.Overheatable = false;
            buildingDef.Floodable = false;
            buildingDef.Entombable = false;
            buildingDef.IsFoundation = true;
            
            // Perfect thermal insulation
            buildingDef.ThermalConductivity = 0.0f;
            
            buildingDef.AudioCategory = "Metal";
            buildingDef.PermittedRotations = PermittedRotations.R90;
            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.ForegroundLayer = Grid.SceneLayer.InteriorWall;
            
            buildingDef.TileLayer = ObjectLayer.FoundationTile;
            buildingDef.ReplacementLayer = ObjectLayer.ReplacementTile;
            buildingDef.ReplacementCandidateLayers = new List<ObjectLayer>
            {
                ObjectLayer.FoundationTile,
                ObjectLayer.Backwall
            };
            
            buildingDef.ReplacementTags = new List<Tag>
            {
                GameTags.FloorTiles
            };

            buildingDef.LogicInputPorts = DoorConfig.CreateSingleInputPortList(new CellOffset(0, 0));

            SoundEventVolumeCache.instance.AddVolume("door_external_kanim", "Open_DoorPressure", NOISE_POLLUTION.NOISY.TIER2);
            SoundEventVolumeCache.instance.AddVolume("door_external_kanim", "Close_DoorPressure", NOISE_POLLUTION.NOISY.TIER2);

            return buildingDef;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            Door door = go.AddOrGet<Door>();
            door.hasComplexUserControls = true;
            door.unpoweredAnimSpeed = 0.65f;
            door.doorType = Door.DoorType.ManualPressure;
            
            go.AddOrGet<ZoneTile>();
            go.AddOrGet<AccessControl>();
            go.AddOrGet<KBoxCollider2D>();
            
            Prioritizable.AddRef(go);
            
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.Door;
            go.AddOrGet<Workable>().workTime = 5f;

            Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<SealedDoorController>();
            
            Door door = go.GetComponent<Door>();
            door.hasComplexUserControls = true;
            door.doorType = Door.DoorType.ManualPressure;

            go.AddOrGet<TileTemperature>();

            go.GetComponent<AccessControl>().controlEnabled = true;
            go.GetComponent<KBatchedAnimController>().initialAnim = "closed";
        }
    }
}
