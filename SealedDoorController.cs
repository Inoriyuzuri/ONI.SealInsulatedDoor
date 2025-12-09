using UnityEngine;
using System.Collections.Generic;

namespace SealedInsulatedDoor
{
    public class SealedDoorController : KMonoBehaviour, ISim200ms
    {
#pragma warning disable CS0649
        [MyCmpGet]
        private Building building;
#pragma warning restore CS0649

        private List<int> doorCells = new List<int>();
        private bool isRegistered = false;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            CacheOccupiedCells();
            ApplySeal();
            isRegistered = true;
        }

        protected override void OnCleanUp()
        {
            isRegistered = false;
            RemoveSeal();
            base.OnCleanUp();
        }

        private void CacheOccupiedCells()
        {
            doorCells.Clear();
            
            if (building != null)
            {
                foreach (int cell in building.PlacementCells)
                {
                    doorCells.Add(cell);
                }
            }
        }

        public void Sim200ms(float dt)
        {
            if (!isRegistered) return;
            ApplySeal();
        }

        private void ApplySeal()
        {
            foreach (int cell in doorCells)
            {
                if (Grid.IsValidCell(cell))
                {
                    SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.GasImpermeable);
                    SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.LiquidImpermeable);
                    Grid.Foundation[cell] = true;
                    
                    // Apply maximum insulation to block heat transfer
                    SimMessages.SetInsulation(cell, float.MaxValue);
                }
            }
        }

        private void RemoveSeal()
        {
            foreach (int cell in doorCells)
            {
                if (Grid.IsValidCell(cell))
                {
                    SimMessages.ClearCellProperties(cell, (byte)Sim.Cell.Properties.GasImpermeable);
                    SimMessages.ClearCellProperties(cell, (byte)Sim.Cell.Properties.LiquidImpermeable);
                    SimMessages.SetInsulation(cell, 0f);
                }
            }
        }
    }
}
