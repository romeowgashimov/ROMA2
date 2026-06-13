using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct DeathSphereAbilitySystem : ISystem
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
            
            state.Dependency = new DeathSphereAbilityJob
            {
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DeathSphereAbilityJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(
            [ChunkIndexInQuery] int key,
            in DeathSphereAbility ability,
            ref DynamicBuffer<TriggerEntityInfo> triggerInfoBuffer,
            in CombineCharsComponent combineCharsComponent,
            in DefaultDamage damage,
            in Owner owner)
        {
            if (triggerInfoBuffer.IsEmpty) return;

            for (int i = 0; i < triggerInfoBuffer.Length; ++i)
            {
                float magicalDamage = damage.MagicalDamage;
                magicalDamage += ability.MagicalPercentage / 100 * combineCharsComponent.MagicalPower;

                ECB.AppendToBuffer<IncomingDamageElement>(key, triggerInfoBuffer[i].Value, new()
                {
                    Owner = owner.Value,
                    Receiver = triggerInfoBuffer[i].Value,
                    MagicalDamage = magicalDamage
                });
                triggerInfoBuffer.RemoveAtSwapBack(i);
            }
        }
    }
}