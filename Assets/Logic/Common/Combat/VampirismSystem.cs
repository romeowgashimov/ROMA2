using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ROMA2.Logic.Common.Combat
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct VampirismSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((Vampirism vamp, DynamicBuffer<ProcessedDamageElement> processedDamage,
                         RefRW<CurrentHealthPoints> currHP, MaxHealthPoints maxHP) in SystemAPI
                         .Query<Vampirism, DynamicBuffer<ProcessedDamageElement>, RefRW<CurrentHealthPoints>,
                             MaxHealthPoints>())
            {
                if (currHP.ValueRO.Value >= maxHP.Value 
                    || processedDamage.IsEmpty 
                    || vamp.Value == 0) return;

                foreach (ProcessedDamageElement element in processedDamage)
                {
                    if (element.AbilityIndex != -1) continue;
                    if (currHP.ValueRO.Value >= maxHP.Value) return;

                    float vampHeal = element.PhysicalDamage * vamp.Value / 100f;
                    currHP.ValueRW.Value = math.min(maxHP.Value, currHP.ValueRO.Value + vampHeal);
                }
            }
        }
    }
}