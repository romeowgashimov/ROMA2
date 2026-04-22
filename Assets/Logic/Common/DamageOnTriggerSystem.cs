using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using static Unity.Entities.SystemAPI;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(AbilityCommandSystemGroup))]
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
            
            DamageOnTriggerJob job = new()
            {
                DamageOnTriggerLookup = GetComponentLookup<DamageOnTrigger>(true),
                TeamLookup = GetComponentLookup<Team>(true),
                AlreadyDamagedLookup = GetBufferLookup<AlreadyDamagedEntity>(true),
                DamageBufferLookup = GetBufferLookup<DamageBufferElement>(true),
                AttackTargetLookup = GetComponentLookup<DefaultAttackTarget>(true),
                OwnerLookup = GetComponentLookup<Owner>(true),
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
        [ReadOnly] public ComponentLookup<DefaultAttackTarget> AttackTargetLookup;
        [ReadOnly] public ComponentLookup<Owner> OwnerLookup;

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
            
            bool reachedTheTarget = false;
            if (AttackTargetLookup.TryGetComponent(damageDealingEntity, out DefaultAttackTarget target))
                if (damageReceivingEntity != target.Value) return;
                else reachedTheTarget = true;
            
            DynamicBuffer<AlreadyDamagedEntity> alreadyDamagedBuffer = AlreadyDamagedLookup[damageDealingEntity];
            foreach (AlreadyDamagedEntity alreadyDamagedEntity in alreadyDamagedBuffer)
                if (alreadyDamagedEntity.Value.Equals(damageReceivingEntity)) return;
            
            if (TeamLookup.TryGetComponent(damageReceivingEntity, out Team receivingTeam) && 
                TeamLookup.TryGetComponent(damageDealingEntity, out Team dealingTeam))
                if (receivingTeam.Value == dealingTeam.Value) return;
                
            DamageOnTrigger damageOnTrigger = DamageOnTriggerLookup[damageDealingEntity];
            OwnerLookup.TryGetComponent(damageDealingEntity, out Owner owner);
            ECB.AppendToBuffer(damageReceivingEntity, new DamageBufferElement
            {
                Value = damageOnTrigger.Value,
                DealingDamageEntity = owner.Value
            });
            ECB.AppendToBuffer(damageDealingEntity, new AlreadyDamagedEntity { Value = damageReceivingEntity });
            
            if (reachedTheTarget) ECB.AddComponent<DestroyEntityTag>(damageDealingEntity);
        }
    } 
}