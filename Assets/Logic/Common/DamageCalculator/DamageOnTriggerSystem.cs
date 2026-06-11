using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using static Unity.Entities.SystemAPI;

namespace ROMA2.Logic.Common.DamageCalculator
{
    [UpdateInGroup(typeof(DamageCalculatorSystemGroup))]
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
                GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            SimulationSingleton simulationSingleton = GetSingleton<SimulationSingleton>();
            
            state.Dependency = new DamageOnTriggerJob
            {
                TriggerLookup = GetComponentLookup<TriggerEntityInfo>(true),
                AlreadyDamagedLookup = GetBufferLookup<AlreadyDamagedEntity>(true),
                TeamLookup = GetComponentLookup<Team>(true),
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }

    [BurstCompile]
    public struct DamageOnTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<TriggerEntityInfo> TriggerLookup;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        [ReadOnly] public BufferLookup<AlreadyDamagedEntity> AlreadyDamagedLookup;
        public EntityCommandBuffer ECB;
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity damageDealingEntity = Entity.Null;
            Entity damageReceivingEntity = Entity.Null;

            if (TriggerLookup.HasComponent(triggerEvent.EntityA))
            {
                damageDealingEntity = triggerEvent.EntityA;
                damageReceivingEntity = triggerEvent.EntityB;
            }
            else if (TriggerLookup.HasComponent(triggerEvent.EntityB))
            {
                damageDealingEntity = triggerEvent.EntityB;
                damageReceivingEntity = triggerEvent.EntityA;
            }

            if (TeamLookup.TryGetComponent(damageReceivingEntity, out Team receivingTeam) && 
                TeamLookup.TryGetComponent(damageDealingEntity, out Team dealingTeam))
                if (receivingTeam.Value == dealingTeam.Value) return;

            if (!AlreadyDamagedLookup.TryGetBuffer(damageDealingEntity,
                    out DynamicBuffer<AlreadyDamagedEntity> alreadyDamagedBuffer))
                return;
            
            foreach (AlreadyDamagedEntity alreadyDamagedEntity in alreadyDamagedBuffer)
                if (alreadyDamagedEntity.Value.Equals(damageReceivingEntity)) return;
            
            ECB.AppendToBuffer<AlreadyDamagedEntity>(damageDealingEntity, new() { Value = damageReceivingEntity });
            ECB.SetComponent<TriggerEntityInfo>(damageDealingEntity, new() { Value = damageReceivingEntity });
        }
    } 
}