using System.Runtime.CompilerServices;
using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;
using static Unity.Mathematics.quaternion;
using float2 = Unity.Mathematics.float2;
using float3 = Unity.Mathematics.float3;

namespace ROMA2.Logic.Common.Movement
{
    public struct AgentData
    {
        public float2 Position;
        public float2 Velocity;
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(PathFindingSystem))]
    public partial struct RVOSystem : ISystem
    {
        private EntityQuery _mainMinionQuery;
        private EntityQuery _mainChampionQuery;
        private EntityQuery _agentsQueryForMinions;
        private EntityQuery _agentsQueryForChampions;

        public void OnCreate(ref SystemState state)
        {
            _mainMinionQuery = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed, 
                         PathPositionElement, FollowPathProperties, PhysicsVelocity, AttackRadius>()
                .WithAll<MinionTag, TargetEntity>()
                .WithDisabled<PathFindingRequest, IncorrectPathProperties>()
                .Build();
            
            _mainChampionQuery = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed, 
                    PathPositionElement, FollowPathProperties, PhysicsVelocity, AttackRadius>()
                .WithAll<ChampTag, TargetEntity>()
                .WithDisabled<PathFindingRequest, IncorrectPathProperties>()
                .Build();
            
            _agentsQueryForMinions = QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity, RVOAgent>()
                .Build();
            
            // Мелкие препятствия не будут регистрироваться на сетке,
            // их обход легче реализовать через RVO. Например: деревья из доты
            _agentsQueryForChampions = QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity, RVOAgent>()
                .WithNone<MinionTag>()
                .Build();
            
            state.RequireForUpdate<GameplayingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Снимок данных всех агентов для безопасного чтения в параллельном Job
            NativeArray<AgentData> agentsForMinions = 
                new(_agentsQueryForMinions.CalculateEntityCount(), Allocator.TempJob);
            NativeArray<AgentData> agentsForChampions = 
                new(_agentsQueryForChampions.CalculateEntityCount(), Allocator.TempJob);
            
            state.Dependency = new CollectRVOAgentDataJob 
            { 
                Data = agentsForMinions 
            }.ScheduleParallel(_agentsQueryForMinions, state.Dependency);

            state.Dependency = new CollectRVOAgentDataJob 
            { 
                Data = agentsForChampions 
            }.ScheduleParallel(_agentsQueryForChampions, state.Dependency);
            
            state.Dependency = new RVOJob
            {
                AllAgents = agentsForMinions,
                K = 3.5f,
                MaxSamples = 12,
                TransformLookup = GetComponentLookup<LocalTransform>(true),
                DeltaTime = Time.DeltaTime,
                CleanPathLookup = GetComponentLookup<CleanPath>(true)
            }.ScheduleParallel(_mainMinionQuery, state.Dependency);
            
            state.Dependency = new RVOJob
            {
                AllAgents = agentsForChampions,
                K = 3.5f,
                MaxSamples = 12,
                TransformLookup = GetComponentLookup<LocalTransform>(true),
                DeltaTime = Time.DeltaTime,
                CleanPathLookup = GetComponentLookup<CleanPath>(true)
            }.ScheduleParallel(_mainChampionQuery, state.Dependency);
            
            // Очистка после выполнения всех зависимостей
            agentsForMinions.Dispose(state.Dependency);
            agentsForChampions.Dispose(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct CollectRVOAgentDataJob : IJobEntity
    {
        public NativeArray<AgentData> Data;

        private void Execute(
            [EntityIndexInQuery] int index, 
            in LocalTransform transform, 
            in PhysicsVelocity velocity)
        {
            Data[index] = new()
            {
                Position = transform.Position.xz,
                Velocity = velocity.Linear.xz,
            };
        }
    }

    /* Когда башни уничтожаются, они оставляют за собой закрытые клетки,
     поэтому некоторые миньоны застревают на позициях уничтоженных башен.
     Миньонов выталкивают свои же миньоны */
    [BurstCompile]
    [WithDisabled(typeof(IncorrectPathProperties))]
    public partial struct RVOJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction] [ReadOnly] 
        public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public NativeArray<AgentData> AllAgents;
        [ReadOnly] public ComponentLookup<CleanPath> CleanPathLookup;
        public float K;
        public int MaxSamples;
        public float DeltaTime;
        public bool IsCharacter; // По этому свойству можем разделять логику миньонов и персонажей
    
        private void Execute(
            ref LocalTransform transform,
            ref PhysicsVelocity velocity,
            in MoveSpeed moveSpeed,
            ref FollowPathProperties followPathProperties,
            ref DynamicBuffer<PathPositionElement> pathPositions,
            in AttackRadius radius,
            in TargetEntity attackTarget,
            in MoveTargetPosition goalPos,
            Entity owner)
        {
            float3 myPos = transform.Position;
            if (attackTarget.Value != Entity.Null 
                && TransformLookup.TryGetComponent(attackTarget.Value, out LocalTransform targetTransform))
            {
                float3 targetPos = targetTransform.Position;
                float distSq = distancesq(targetPos, myPos);
                float stopDist = radius.Value - 1;
    
                if (distSq <= stopDist * stopDist)
                {
                    if (!pathPositions.IsEmpty) pathPositions.Clear();
                    velocity.Linear = float3.zero;
                    float3 dir = normalizesafe(targetPos - myPos);
                    quaternion targetRot = LookRotationSafe(new(dir.x, 0, dir.z), up());
                    transform.Rotation = slerp(transform.Rotation, targetRot, DeltaTime * 10f);
                    return;
                }
            }
            
            if (pathPositions.IsEmpty || followPathProperties.Index < 0)
            {
                velocity.Linear = float3.zero;
                return;
            }
    
            float2 selfPosXZ = myPos.xz;
            float2 currentVXZ = velocity.Linear.xz;
            float2 targetPathXZ;

            // Чтобы не дрифтил
            if (followPathProperties.IsNewPath)
            {
                velocity.Linear = float3.zero;
                followPathProperties.IsNewPath = false;
            }
            
            if (CleanPathLookup.IsComponentEnabled(owner)) targetPathXZ = goalPos.Value.xz;
            else
            {
                targetPathXZ = pathPositions[followPathProperties.Index].Value;

                // Упростил логику, в любом случае пропускаем два индекса
                if (lengthsq(targetPathXZ - selfPosXZ) <= 2.25 && followPathProperties.Index >= 2)
                    followPathProperties.Index -= 2;
                
                // Сделать дополнительную проверку прохождения вейпоинтов через dot

                // <= 1 обусловлено тем, что при нечётном количестве вейпоинтов концом пути будет 1
                if (followPathProperties.Index <= 1)
                    targetPathXZ = new(goalPos.Value.x, goalPos.Value.z);
            }
            
            // Считаем чистый вектор и расстояние до цели
            float2 vectorToTarget = targetPathXZ - selfPosXZ;
            float distanceToTarget = length(vectorToTarget);

            // Направление движения 
            float2 moveDirection = normalizesafe(vectorToTarget);

            // Рассчитываем желаемую скорость
            float desiredSpeed = moveSpeed.Value;

            // Скорость за кадр (Speed * DeltaTime) не должна превышать расстояние до цели
            // Делим расстояние на DeltaTime, чтобы получить максимально допустимую скорость для этого кадра
            float maxAllowedSpeed = distanceToTarget / DeltaTime;
            float finalSpeed = min(desiredSpeed, maxAllowedSpeed);
            
            float2 vGoal = moveDirection * finalSpeed;
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
                // Штраф за отклонение от нынешнего вектора, направления
                penalty += distance(vCand, currentVXZ) * 1.2f;
    
                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    vBest = vCand;
                }
            }

            // Можем разделить логику поворотов на логику миньонов и персонажей булевым признаком при создании джобы 
            float delta = lengthsq(goalPos.Value.xz - selfPosXZ) > 1 ? 0.2f : 1;
            float2 finalV = lerp(currentVXZ, vBest, delta);
            velocity.Linear = new(finalV.x, velocity.Linear.y, finalV.y);
    
            if (lengthsq(finalV) > 0.01f)
                transform.Rotation = LookRotationSafe(new(finalV.x, 0, finalV.y), up());
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculatePenalty(float2 vCand, float2 vGoal, float2 posA, float2 vA, float combinedRadiusSq, int agentsCount)
        {
            float tc = 5.0f;

            for (int i = 0; i < agentsCount; i++)
            {
                AgentData agentB = AllAgents[i];
                float2 relPos = agentB.Position - posA;
                float distSq = lengthsq(relPos);
    
                // Оптимизация: быстрый отсев по дистанции (4.0 м -> 16.0 м^2)
                if (distSq < 0.001f || distSq > 16f) continue;

                /* Если цель статична, то 100% ответственности за уклонение.
                 Статичные цели могут быть с большим радиусом, чем у миньонов, это нужно будет учесть */
                float2 vRVO;
                if (!agentB.Velocity.Equals(float2.zero))
                    vRVO = (vCand + vA) * 0.5f;
                else vRVO = vCand + vA;
                float2 relVel = vRVO - agentB.Velocity;
                
                // Векторная проверка на столкновение (Ray-Sphere Intersection)
                float dotRV = dot(relVel, relPos);
                
                // Если движемся в сторону друг друга
                if (dotRV >= 0)
                {
                    float vMagSq = lengthsq(relVel);
                    if (vMagSq < 0.001f) continue;
                    
                    float2 rayClosest = relPos - dotRV / vMagSq * relVel;

                    if (lengthsq(rayClosest) < combinedRadiusSq)
                    {
                        float b = -2f * dotRV;
                        float c = distSq - combinedRadiusSq;
                        float disc = b * b - 4f * vMagSq * c;

                        if (disc >= 0)
                        {
                            float t = (-b - sqrt(disc)) / (2f * vMagSq);
                            if (t > 0 && t < tc) tc = t;
                        }
                    }
                }
            }
            
            return K / tc + distance(vCand, vGoal) * 1.5f;
        }
    }
}