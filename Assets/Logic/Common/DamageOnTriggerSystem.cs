using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(BeginAoeAbilitySystem))]
    public partial struct DamageOnTriggerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EndPredictedSimulationEntityCommandBufferSystem.Singleton ecbSingleton =
                SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            
            DamageOnTriggerJob job = new()
            {
                DamageOnTriggerLookup = SystemAPI.GetComponentLookup<DamageOnTrigger>(true),
                TeamLookup = SystemAPI.GetComponentLookup<Team>(true),
                AlreadyDamagedLookup = SystemAPI.GetBufferLookup<AlreadyDamagedEntity>(true),
                DamageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true),
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            };
            
            state.Dependency = job.Schedule(simulationSingleton, state.Dependency);
        }
    }

    [BurstCompile]
    public struct DamageOnTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<DamageOnTrigger> DamageOnTriggerLookup;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        [ReadOnly] public BufferLookup<AlreadyDamagedEntity> AlreadyDamagedLookup;
        [ReadOnly] public BufferLookup<DamageBufferElement> DamageBufferLookup;

        public EntityCommandBuffer ECB;
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity damageDealingEntity;
            Entity damageReceivingEntity;

            if (DamageBufferLookup.HasBuffer(triggerEvent.EntityA) &&
                DamageOnTriggerLookup.HasComponent(triggerEvent.EntityB))
            {
                damageReceivingEntity = triggerEvent.EntityA;
                damageDealingEntity = triggerEvent.EntityB;
            }
            else if (DamageBufferLookup.HasBuffer(triggerEvent.EntityB) &&
                     DamageOnTriggerLookup.HasComponent(triggerEvent.EntityA))
            {
                damageReceivingEntity = triggerEvent.EntityB;
                damageDealingEntity = triggerEvent.EntityA;
            }
            else return;
            
            DynamicBuffer<AlreadyDamagedEntity> alreadyDamagedBuffer = AlreadyDamagedLookup[damageDealingEntity];
            foreach (AlreadyDamagedEntity alreadyDamagedEntity in alreadyDamagedBuffer)
                if (alreadyDamagedEntity.Value.Equals(damageReceivingEntity)) return;
            
            if (TeamLookup.TryGetComponent(damageReceivingEntity, out Team receivingTeam) && 
                TeamLookup.TryGetComponent(damageDealingEntity, out Team dealingTeam))
                if (receivingTeam.Value == dealingTeam.Value) return;
                
            DamageOnTrigger damageOnTrigger = DamageOnTriggerLookup[damageDealingEntity];
            ECB.AppendToBuffer(damageReceivingEntity, new DamageBufferElement { Value = damageOnTrigger.Value });
            ECB.AppendToBuffer(damageDealingEntity, new AlreadyDamagedEntity { Value = damageReceivingEntity });
        }
    } 
}