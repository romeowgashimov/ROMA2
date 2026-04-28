using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;
using static Unity.Mathematics.float3;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using float3 = Unity.Mathematics.float3;

namespace Logic.Common
{
    public struct AgentData
    {
        public float2 Position;
        public float2 Velocity;
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct RVOSystem : ISystem
    {
        private EntityQuery _mainQuery;
        private EntityQuery _allAgentsQuery;

        public void OnCreate(ref SystemState state)
        {
            _mainQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed, 
                         PathPositionElement, FollowPathIndex, PhysicsVelocity, NpcAttackRadius>()
                .WithAll<MinionTag, NpcTargetEntity>()
                .WithNone<NeedPath>()
                .Build();

            _allAgentsQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity, NpcAttackRadius>()
                .Build();

            state.RequireForUpdate<GameplayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int agentCount = _allAgentsQuery.CalculateEntityCount();
            if (agentCount <= 0) return;

            // Снимок данных всех агентов для безопасного чтения в параллельном Job
            NativeArray<AgentData> allAgents = new(agentCount, Allocator.TempJob);
            
            state.Dependency = new CollectDataJob 
            { 
                Data = allAgents 
            }.ScheduleParallel(_allAgentsQuery, state.Dependency);

            state.Dependency = new MinionMoveJob
            {
                AllAgents = allAgents,
                K = 3.5f,
                MaxSamples = 12,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(_mainQuery, state.Dependency);
            
            // Очистка после выполнения всех зависимостей
            allAgents.Dispose(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct CollectDataJob : IJobEntity
    {
        public NativeArray<AgentData> Data;
        public void Execute([EntityIndexInQuery] int index, in LocalTransform transform, in PhysicsVelocity velocity, in NpcAttackRadius radius)
        {
            Data[index] = new()
            {
                Position = transform.Position.xz,
                Velocity = velocity.Linear.xz,
            };
        }
    }

    [BurstCompile]
    public partial struct MinionMoveJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public NativeArray<AgentData> AllAgents;
        public float K;
        public int MaxSamples;
        public float DeltaTime;
    
        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity,
            in MoveSpeed moveSpeed,
            ref FollowPathIndex followPathIndex,
            in DynamicBuffer<PathPositionElement> pathPositions,
            in NpcAttackRadius radius,
            in NpcTargetEntity attackTarget)
        {
            if (pathPositions.IsEmpty || followPathIndex.Value < 0)
            {
                velocity.Linear = zero;
                return;
            }
    
            float3 myPos = transform.Position;
    
            if (attackTarget.Value != Entity.Null)
            {
                float3 targetPos = TransformLookup[attackTarget.Value].Position;
                float distSq = distancesq(targetPos, myPos);
                float stopDist = radius.Value - 1f;
    
                if (distSq <= stopDist * stopDist)
                {
                    velocity.Linear = zero;
                    float3 dir = normalizesafe(targetPos - myPos);
                    quaternion targetRot = LookRotationSafe(new(dir.x, 0, dir.z), up());
                    transform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                    return;
                }
            }
    
            float2 selfPosXZ = myPos.xz;
            float2 currentVXZ = velocity.Linear.xz;
            float2 targetPathXZ = pathPositions[followPathIndex.Value].Value;
            
            float2 vGoal = math.normalizesafe(targetPathXZ - selfPosXZ) * moveSpeed.Value;
            float2 vBest = vGoal;
            float minPenalty = float.MaxValue;
    
            // Постоянные для RVO
            const float bodyRadius = 0.5f;
            const float combinedRadius = bodyRadius + 0.5f;
            const float combinedRadiusSq = combinedRadius * combinedRadius;
    
            // Кэшируем данные, чтобы не пересчитывать в цикле
            int agentsCount = AllAgents.Length;
    
            for (int i = 0; i <= MaxSamples; i++)
            {
                float2 vCand;
                if (i == 0) vCand = vGoal;
                else
                {
                    sincos(PI2 / MaxSamples * i, out float sin, out float cos);
                    vCand = new float2(cos, sin) * moveSpeed.Value;
                }
    
                float penalty = CalculatePenalty(vCand, vGoal, selfPosXZ, currentVXZ, combinedRadiusSq, agentsCount);
                penalty += distance(vCand, currentVXZ) * 1.2f;
    
                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    vBest = vCand;
                }
            }
    
            float2 finalV = lerp(currentVXZ, vBest, 0.2f);
            velocity.Linear = new(finalV.x, velocity.Linear.y, finalV.y);
    
            if (lengthsq(finalV) > 0.01f)
                transform.Rotation = LookRotationSafe(new(finalV.x, 0, finalV.y), math.up());
    
            // Логика переключения (без изменений)
            float2 futurePos = followPathIndex.Value > 0 && followPathIndex.Value < pathPositions.Length
                ? pathPositions[followPathIndex.Value - 1].Value
                : selfPosXZ;
            float2 segmentDir = normalize(targetPathXZ - futurePos);
            float2 vectorToTarget = targetPathXZ - selfPosXZ;
    
            if (math.dot(vectorToTarget, segmentDir) >= -0.2f)
            {
                followPathIndex.Value--;
                if (followPathIndex.Value < 0) velocity.Linear = zero;
            }
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculatePenalty(float2 vCand, float2 vGoal, float2 posA, float2 vA, float combinedRadiusSq, int agentsCount)
        {
            float tc = 5.0f;
            float2 vRVO = (vCand + vA) * 0.5f;
    
            for (int i = 0; i < agentsCount; i++)
            {
                AgentData agentB = AllAgents[i];
                float2 relPos = agentB.Position - posA;
                float distSq = math.lengthsq(relPos);
    
                // Оптимизация: быстрый отсев по дистанции (4.0 м -> 16.0 м^2)
                if (distSq < 0.001f || distSq > 16f) continue;
    
                float2 relVel = vRVO - agentB.Velocity;
                
                // Векторная проверка на столкновение (Ray-Sphere Intersection)
                float dotRV = dot(relVel, relPos);
                
                // Если движемся в сторону друг друга
                if (dotRV > 0)
                {
                    float2 rayClosest = relPos - dotRV / lengthsq(relVel) * relVel;
                    if (lengthsq(rayClosest) < combinedRadiusSq)
                    {
                        // Вычисляем точное время до касания через дискриминант (упрощенно)
                        float vMagSq = lengthsq(relVel);
                        float b = -2f * dotRV;
                        float c = distSq - combinedRadiusSq;
                        float disc = b * b - 4f * vMagSq * c;
    
                        if (disc >= 0)
                        {
                            float t = (dotRV - sqrt(disc)) / vMagSq;
                            if (t > 0 && t < tc) tc = t;
                        }
                    }
                }
            }
    
            return K / tc + distance(vCand, vGoal) * 1.5f;
        }
    }
}

