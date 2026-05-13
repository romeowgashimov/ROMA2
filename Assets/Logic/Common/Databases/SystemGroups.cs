using Unity.Entities;
using Unity.NetCode;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class AbilityCommandSystemGroup : ComponentSystemGroup { }
}