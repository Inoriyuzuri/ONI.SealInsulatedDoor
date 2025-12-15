using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace SealedInsulatedDoor
{
    [RestartRequired]
    [JsonObject(MemberSerialization.OptIn)]
    public class ModSettings : SingletonOptions<ModSettings>
    {
        [Option("Speed Multiplier", "门开关动画速度倍数，基于原版手动气闸速度")]
        [Limit(1, 100)]
        [JsonProperty]
        public int SpeedMultiplier { get; set; }

        public ModSettings()
        {
            SpeedMultiplier = 5;
        }
    }
}
