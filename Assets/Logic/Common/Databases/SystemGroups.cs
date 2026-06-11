using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Databases
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(DamageCalculatorSystemGroup))]
    public partial class AbilityCommandSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class DamageCalculatorSystemGroup : ComponentSystemGroup { }
}