using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct DeathShotAbilitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            
            state.Dependency = new DeathShotAbilityJob
            {
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DeathShotAbilityJob : IJobEntity
    {
        public Entity AttackCommandPrefab;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(
            [ChunkIndexInQuery] int key,
            in DeathShotAbility ability,
            ref DynamicBuffer<TriggerEntityInfo> triggerInfoBuffer,
            in CombineCharsComponent combineCharsComponent,
            in DefaultDamage damage,
            in Owner owner)
        {
            if (triggerInfoBuffer.IsEmpty) return;

            for (int i = 0; i < triggerInfoBuffer.Length; ++i)
            {
                float physicalDamage = damage.PhysicalDamage;
                physicalDamage += ability.PhysicalPercentage / 100 * combineCharsComponent.PhysicalPower;

                ECB.AppendToBuffer<IncomingDamageElement>(key, triggerInfoBuffer[i].Value, new()
                {
                    Owner = owner.Value,
                    Receiver = triggerInfoBuffer[i].Value,
                    PhysicalDamage = physicalDamage
                });
                triggerInfoBuffer.RemoveAtSwapBack(i);
            }
        }
    }
}