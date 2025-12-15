using System.Collections.Generic;

namespace SealedInsulatedDoor
{
    public class SealedDoorController : KMonoBehaviour, ISim200ms
    {
#pragma warning disable CS0649
        [MyCmpGet] private Building building;
#pragma warning restore CS0649
        private List<int> doorCells = new List<int>();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            foreach (int cell in building.PlacementCells)
                doorCells.Add(cell);
            ApplySeal();
        }

        protected override void OnCleanUp()
        {
            foreach (int cell in doorCells)
            {
                if (!Grid.IsValidCell(cell)) continue;
                SimMessages.ClearCellProperties(cell, (byte)Sim.Cell.Properties.GasImpermeable);
                SimMessages.ClearCellProperties(cell, (byte)Sim.Cell.Properties.LiquidImpermeable);
                SimMessages.SetInsulation(cell, 0f);
            }
            base.OnCleanUp();
        }

        public void Sim200ms(float dt) => ApplySeal();

        private void ApplySeal()
        {
            foreach (int cell in doorCells)
            {
                if (!Grid.IsValidCell(cell)) continue;
                SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.GasImpermeable);
                SimMessages.SetCellProperties(cell, (byte)Sim.Cell.Properties.LiquidImpermeable);
                SimMessages.SetInsulation(cell, float.MaxValue);
                Grid.Foundation[cell] = true;
            }
        }
    }
}
