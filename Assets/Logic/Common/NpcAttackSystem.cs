using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NpcAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            state.Dependency = new NpcAttackJob
            {
                CurrentTick = networkTime.ServerTick,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct NpcAttackJob : IJobEntity
    {
        [ReadOnly] public NetworkTick CurrentTick;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int sortKey, ref DynamicBuffer<NpcAttackCooldown> attackCooldown,
            in NpcAttackProperties attackProperties, in NpcTargetEntity targetEntity, Entity npcEntity, Team team)
        {
            if (!TransformLookup.HasComponent(targetEntity.Value)) return;
            if (!attackCooldown.GetDataAtTick(CurrentTick, out NpcAttackCooldown cooldownExpirationTick))
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

            NetworkTick newCooldownTick = CurrentTick;
            newCooldownTick.Add(attackProperties.CooldownTickCount);
            attackCooldown.AddCommandData(new() { Tick = CurrentTick, Value = newCooldownTick });
        }
    }
}