using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BasicRangedAttackSystem : ISystem
    {
        [BurstCompile]
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
            
            state.Dependency = new BasicRangedAttackJob
            {
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(RangedAttack))]
    public partial struct BasicRangedAttackJob : IJobEntity
    {
        public Entity AttackCommandPrefab;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(
            [ChunkIndexInQuery] int key,
            in CombineCharsComponent combineCharsComponent,
            ref DynamicBuffer<TriggerEntityInfo> triggerInfoBuffer,
            in BasicAttackTarget target,
            in Owner owner,
            Entity attack)
        {
            if (triggerInfoBuffer.IsEmpty) return;

            for (int i = 0; i < triggerInfoBuffer.Length; ++i)
            {
                Entity triggerEntity = triggerInfoBuffer[i].Value;
                if (triggerEntity == Entity.Null 
                    || triggerEntity != target.Value) continue;

                float totalDamage = combineCharsComponent.PhysicalPower;

                // Если буфер есть, то команда ничего не сделает, нужен только из-за отката состояний неткода
                ECB.AddBuffer<IncomingDamageElement>(key, triggerInfoBuffer[i].Value);
                ECB.AppendToBuffer<IncomingDamageElement>(key, triggerInfoBuffer[i].Value, new()
                {
                    Owner = owner.Value,
                    Receiver = triggerInfoBuffer[i].Value,
                    PhysicalDamage = totalDamage
                });
                triggerInfoBuffer.Clear();
                ECB.AddComponent<DestroyEntityTag>(key, attack);
            }
        }
    }
}