using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [BurstCompile]
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup))]
    public partial struct ClearProcessedDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach(DynamicBuffer<ProcessedDamageElement> damageElements in SystemAPI
                    .Query<DynamicBuffer<ProcessedDamageElement>>())
            {
                if (!damageElements.IsEmpty) damageElements.Clear();
            }
        }
    }
}