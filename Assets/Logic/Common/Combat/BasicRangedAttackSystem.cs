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
    [WithNone(typeof(AbilityIndex))]
    public partial struct BasicRangedAttackJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(
            [ChunkIndexInQuery] int key,
            in CombineCharsComponent combineCharsComponent,
            ref DynamicBuffer<TriggerEntityInfo> triggerInfoBuffer,
            in BasicAttackTarget target,
            in Owner owner,
            ref DynamicBuffer<SendDamageElement> sendDamages,
            Entity attack)
        {
            if (triggerInfoBuffer.IsEmpty) return;

            for (int i = 0; i < triggerInfoBuffer.Length; ++i)
            {
                Entity triggerEntity = triggerInfoBuffer[i].Value;
                if (triggerEntity == Entity.Null 
                    || triggerEntity != target.Value) continue;

                int totalDamage = combineCharsComponent.PhysicalPower;

                sendDamages.Add(new()
                {
                    PhysicalDamage = totalDamage,
                    Receiver = triggerInfoBuffer[i].Value,
                    Owner = owner.Value,
                    AbilityIndex = -1
                });
                triggerInfoBuffer.Clear();
                ECB.AddComponent<DestroyEntityTag>(key, attack);
            }
        }
    }
}