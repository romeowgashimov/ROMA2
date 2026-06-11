using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace ROMA2.Logic.Common.Combat
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct InstBasicRangedAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new InstBasicRangedAttackJob
            {
                CurrentTick = networkTime.ServerTick,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate), typeof(InAttackArea))]
    public partial struct InstBasicRangedAttackJob : IJobEntity
    {
        private const int SIMULATION_TICK_RATE = 60;
        
        [ReadOnly] public NetworkTick CurrentTick;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        private void Execute(
            [ChunkIndexInQuery] int sortKey,
            ref DynamicBuffer<AttackCooldown> attackCooldown,
            in RangedAttackProperties attackProperties,
            in TargetEntity targetEntity, 
            Entity npcEntity,
            in Team team,
            in AttackSpeed attackSpeed, 
            in PhysicalPower physicalPower)
        {
            if (!TransformLookup.HasComponent(targetEntity.Value)) return;
            if (!attackCooldown.GetDataAtTick(CurrentTick, out AttackCooldown cooldownExpirationTick))
                cooldownExpirationTick.Value = NetworkTick.Invalid;
            
            bool canAttack = !cooldownExpirationTick.Value.IsValid
                             || CurrentTick.IsNewerThan(cooldownExpirationTick.Value);
            if (!canAttack) return;

            float3 spawnPosition = TransformLookup[npcEntity].Position + attackProperties.FirePointOffset;
            float3 targetPosition = TransformLookup[targetEntity.Value].Position;

            Entity newAttack = ECB.Instantiate(sortKey, attackProperties.AttackPrefab);
            LocalTransform newAttackTransform = LocalTransform.FromPositionRotation(spawnPosition,
                quaternion.LookRotationSafe(targetPosition - spawnPosition, math.up()));
            
            ECB.SetComponent(sortKey, newAttack, newAttackTransform);
            ECB.SetComponent(sortKey, newAttack, team);
            ECB.SetComponent(sortKey, newAttack, new BasicAttackTarget { Value = targetEntity.Value });
            ECB.SetComponent(sortKey, newAttack, new Owner { Value = npcEntity });
            ECB.SetComponent<CombineCharsComponent>(sortKey, newAttack, new()
            {
                PhysicalPower = physicalPower.Value
            });
            
            NetworkTick newCooldownTick = CurrentTick;
            newCooldownTick.Add((uint)attackSpeed.Value * SIMULATION_TICK_RATE);
            attackCooldown.AddCommandData(new() { Tick = CurrentTick, Value = newCooldownTick });
        }
    }
}