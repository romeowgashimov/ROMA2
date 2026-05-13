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
                         PathPositionElement, FollowPathIndex, PhysicsVelocity, AttackRadius>()
                .WithAll<MinionTag, TargetEntity>()
                .WithNone<PathFindingRequest>()
                .Build();

            _allAgentsQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity, AttackRadius>()
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
        public void Execute([EntityIndexInQuery] int index, in LocalTransform transform, in PhysicsVelocity velocity, in AttackRadius radius)
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
        in AttackRadius radius,
        in TargetEntity attackTarget)
    {
        if (pathPositions.IsEmpty || followPathIndex.Value < 0)
        {
            velocity.Linear = zero;
            return;
        }

        float3 myPos = transform.Position;

        // Логика атаки цели
        if (attackTarget.Value != Entity.Null)
        {
            float3 targetPos = TransformLookup[attackTarget.Value].Position;
            float distSq = distancesq(targetPos, myPos);
            float stopDist = radius.Value - 1f;

            if (distSq <= stopDist * stopDist)
            {
                velocity.Linear = zero;
                float3 dir = normalizesafe(targetPos - myPos);
                quaternion targetRot = LookRotationSafe(new float3(dir.x, 0, dir.z), up());
                transform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                return;
            }
        }

        float2 selfPosXZ = myPos.xz;
        float2 currentVXZ = velocity.Linear.xz;
        float2 targetPathXZ = pathPositions[followPathIndex.Value].Value;
        
        float2 vGoal = normalizesafe(targetPathXZ - selfPosXZ) * moveSpeed.Value;
        float2 vBest = vGoal;
        float minPenalty = float.MaxValue;

        // Постоянные для RVO
        const float bodyRadius = 0.5f;
        const float combinedRadius = bodyRadius + 0.5f;
        const float combinedRadiusSq = combinedRadius * combinedRadius;

        int agentsCount = AllAgents.Length;

        // Адаптивный К: если миньон зажат или замедлен, он наглее прет вперед
        float adaptiveK = K;
        if (lengthsq(currentVXZ) < (moveSpeed.Value * moveSpeed.Value * 0.04f))
        {
            adaptiveK *= 0.15f; // Снижаем страх столкновения на 85%, заставляя давить вперед
        }

        // Цикл поиска оптимальной скорости
        for (int i = 0; i <= MaxSamples; i++)
        {
            float2 vCand;
            if (i == 0) vCand = vGoal;
            else
            {
                sincos(PI * 2f / MaxSamples * i, out float sin, out float cos);
                vCand = new float2(cos, sin) * moveSpeed.Value;
            }

            float penalty = CalculatePenalty(vCand, vGoal, selfPosXZ, combinedRadiusSq, agentsCount, moveSpeed.Value, adaptiveK);

            if (penalty < minPenalty)
            {
                minPenalty = penalty;
                vBest = vCand;
            }
        }

        // Плавная интерполяция к лучшей скорости
        float2 finalV = lerp(currentVXZ, vBest, 0.2f);
        velocity.Linear = new float3(finalV.x, velocity.Linear.y, finalV.y);

        if (lengthsq(finalV) > 0.01f)
            transform.Rotation = LookRotationSafe(new float3(finalV.x, 0, finalV.y), up());

        // Логика переключения вейпоинтов
        float2 futurePos = followPathIndex.Value > 0 && followPathIndex.Value < pathPositions.Length
            ? pathPositions[followPathIndex.Value - 1].Value
            : selfPosXZ;
        float2 segmentDir = normalize(targetPathXZ - futurePos);
        float2 vectorToTarget = targetPathXZ - selfPosXZ;

        if (dot(vectorToTarget, segmentDir) >= -0.2f)
        {
            followPathIndex.Value--;
            if (followPathIndex.Value < 0) velocity.Linear = zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculatePenalty(
        float2 vCand, 
        float2 vGoal, 
        float2 posA, 
        float combinedRadiusSq, 
        int agentsCount, 
        float maxSpeed, 
        float currentK)
    {
        float tc = 5.0f;
        float2 vRVO = vCand; // Для обхода игрока оцениваем чистую скорость-кандидат

        for (int i = 0; i < agentsCount; i++)
        {
            AgentData agentB = AllAgents[i];
            float2 relPos = agentB.Position - posA;
            float distSq = lengthsq(relPos);

            // Игнорируем себя
            if (distSq < 0.001f) continue;

            // Если УЖЕ произошло пересечение коллайдеров (зажали) — выдаем огромный штраф
            if (distSq < combinedRadiusSq)
            {
                return (currentK / 0.01f) + maxSpeed * 10f;
            }

            // Дистанция отсева (увеличена до 6 метров для быстрого игрока)
            if (distSq > 36f) continue;

            float2 relVel = vRVO - agentB.Velocity;
            float dotRV = dot(relPos, relVel);
            
            // Если векторы направлены друг к другу
            if (dotRV > 0)
            {
                float vMagSq = lengthsq(relVel);
                if (vMagSq > 0.001f)
                {
                    // Исправленная проекция центра на луч относительной скорости
                    float2 rayClosest = relPos - (dotRV / vMagSq) * relVel;
                    
                    if (lengthsq(rayClosest) < combinedRadiusSq)
                    {
                        // Исправленный дискриминант квадратного уравнения (без лишних знаков и четверок)
                        float b = dotRV;
                        float c = distSq - combinedRadiusSq;
                        float disc = b * b - vMagSq * c;

                        if (disc >= 0)
                        {
                            float t = (b - sqrt(disc)) / vMagSq;
                            if (t > 0 && t < tc) tc = t;
                        }
                    }
                }
            }
        }

        // Расчет штрафа за отклонение от курса
        float2 normalizedGoal = normalizesafe(vGoal);
        float2 normalizedCand = normalizesafe(vCand);
        float dotProduct = dot(normalizedGoal, normalizedCand);
        
        // Квадратичный штраф за угол: не дает уходить в бок и выстраиваться шеренгой
        float forwardPenalty = (1.0f - dotProduct) * (1.0f - dotProduct) * maxSpeed * 5.0f;
        
        // Мягкий штраф за разницу модулей скоростей
        float speedDiff = distancesq(vCand, vGoal) * 0.2f;

        return (currentK / tc) + forwardPenalty + speedDiff;
    }
}

}

