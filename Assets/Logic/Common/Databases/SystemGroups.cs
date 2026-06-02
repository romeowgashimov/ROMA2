using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Databases
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class AbilityCommandSystemGroup : ComponentSystemGroup { }
}