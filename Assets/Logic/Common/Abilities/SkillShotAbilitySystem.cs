using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct SkillShotSystem : ISystem
    {
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
            
            state.Dependency = new SkillShotAbilityJob
            {
                AttackCommandPrefab = attackCommandPrefab,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct SkillShotAbilityJob : IJobEntity
    {
        public Entity AttackCommandPrefab;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(
            [ChunkIndexInQuery] int key,
            in SkillShotAbility ability,
            ref TriggerEntityInfo triggerInfo,
            in CombineCharsComponent combineCharsComponent,
            in DefaultDamage damage,
            in Owner owner)
        {
            if (triggerInfo.Value == Entity.Null) return;
            
            float physicalDamage = damage.PhysicalDamage;
            physicalDamage += ability.PhysicalPercentage / 100 * combineCharsComponent.PhysicalPower;

            Entity attackCommand = ECB.Instantiate(key, AttackCommandPrefab);
            ECB.SetComponent<AttackCommand>(key, attackCommand, new()
            {
                PhysicalDamage = physicalDamage,
                Owner = owner.Value,
                Receiver = triggerInfo.Value
            });
            ECB.SetComponentEnabled<BasicAttackCommand>(key, attackCommand, false);
            
            triggerInfo.Value = Entity.Null;
        }
    }
}