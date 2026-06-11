using ROMA2.Logic.Common.Databases;
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
            state.RequireForUpdate<AttackCommandContainerTag>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity attackCommandPrefab = SystemAPI
                .GetSingleton<AttackCommandContainerTag>()
                .Value;
            
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            
            state.Dependency = new BasicRangedAttackJob
            {
                AttackCommandPrefab = attackCommandPrefab,
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
            ref TriggerEntityInfo triggerInfo,
            in BasicAttackTarget target,
            in Owner owner,
            Entity attack)
        {
            Entity triggerEntity = triggerInfo.Value;
            if (triggerEntity == Entity.Null 
                || triggerEntity != target.Value) return;

            float totalDamage = combineCharsComponent.PhysicalPower;
            
            Entity attackCommand = ECB.Instantiate(key, AttackCommandPrefab);
            ECB.SetComponent<AttackCommand>(key, attackCommand, new()
            {
                PhysicalDamage = totalDamage,
                Owner = owner.Value,
                Receiver = triggerEntity
            });
            
            triggerInfo.Value = Entity.Null; 
            ECB.AddComponent<DestroyEntityTag>(key, attack);
        }
    }
}