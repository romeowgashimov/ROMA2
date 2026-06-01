using System.Runtime.CompilerServices;
using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

namespace ROMA2.Logic.Common.Movement
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PathFindingSystem))]
    [UpdateBefore(typeof(MoveSystem))]
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
                .WithAll<MinionTag, TargetEntity, RVOAgent>()
                .WithDisabled<PathFindingRequest, IncorrectPathProperties>()
                .Build();
            
            _mainChampionQuery = QueryBuilder()
                .WithAll<LocalTransform, MoveTargetPosition, MoveSpeed, 
                    PathPositionElement, FollowPathProperties, PhysicsVelocity, AttackRadius>()
                .WithAll<ChampTag, TargetEntity, RVOAgent>()
                .WithDisabled<PathFindingRequest, IncorrectPathProperties>()
                .Build();
            
            _agentsQueryForMinions = QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity, RVOAgent>()
                .Build();
            
            // Мелкие препятствия не будут регистрироваться на сетке,
            // их обход легче реализовать через RVO. Например: деревья из доты
            _agentsQueryForChampions = QueryBuilder()
                .WithAll<LocalTransform, PhysicsVelocity, RVOAgent>()
                .WithNone<MinionTag, ChampTag>()
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
                DeltaTime = Time.DeltaTime
            }.ScheduleParallel(_mainMinionQuery, state.Dependency);
            
            state.Dependency = new RVOJob
            {
                AllAgents = agentsForChampions,
                K = 3.5f,
                MaxSamples = 12,
                DeltaTime = Time.DeltaTime
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
            in PhysicsVelocity velocity,
            in RVOAgent agent)
        {
            Data[index] = new()
            {
                Position = transform.Position.xz,
                Velocity = velocity.Linear.xz,
            };
        }
    }
    
    [BurstCompile]
    public partial struct RVOJob : IJobEntity
    {
        [ReadOnly] public NativeArray<AgentData> AllAgents;
        public float K;
        public int MaxSamples;
        public float DeltaTime;

        private void Execute(
            in LocalTransform transform,
            in PhysicsVelocity velocity,
            in MoveSpeed moveSpeed,
            ref FollowPathProperties followPathProperties,
            in DynamicBuffer<PathPositionElement> pathPositions,
            in MoveTargetPosition goalPos,
            in TargetEntity targetEntity,
            ref RVOAgent agent)
        {
            // Если дошли до цели, останавливаемся и отключаем RVO
            if (targetEntity.InAttackArea || followPathProperties.ReachedTheTarget
                || (pathPositions.IsEmpty && !followPathProperties.IsCleanPath)) return;

            float2 selfPosXZ = transform.Position.xz;
            float2 currentVXZ = velocity.Linear.xz;
            float2 targetPathXZ;

            // <= 1 обусловлено тем, что при нечётном количестве вейпоинтов концом пути будет 1
            if (followPathProperties.IsCleanPath || followPathProperties.Index <= 1) targetPathXZ = goalPos.Value.xz;
            else
            {
                targetPathXZ = pathPositions[followPathProperties.Index].Value;
                // Упростил логику, в любом случае пропускаем два индекса
                if (lengthsq(targetPathXZ - selfPosXZ) <= 2.25 && followPathProperties.Index >= 2)
                    followPathProperties.Index -= 2; 
                // Дополнительная проверка прохождения вейпоинтов через dot,
                // если случайно промазали и не попали в радиус
                else
                {
                    // Гарантируем, что не выйдем за массив, так как index - 2 в pathfinding
                    float2 pastTargetPathXZ = pathPositions[followPathProperties.Index + 1].Value;
                    float2 pastVector = normalizesafe(targetPathXZ - pastTargetPathXZ);
                    float2 currentVector = normalizesafe(selfPosXZ - targetPathXZ);
                    if (dot(currentVector, pastVector) >= 0)
                    {
                        followPathProperties.Index -= 1;
                        targetPathXZ = pathPositions[followPathProperties.Index].Value;
                    }
                }
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
            float combinedRadius = agent.BodyRadius + 0.5f;
            float combinedRadiusSq = combinedRadius * combinedRadius;
    
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
    
                float penalty = 
                    CalculatePenalty(vCand, vGoal, selfPosXZ, currentVXZ, combinedRadiusSq, agentsCount);
                // Штраф за отклонение от нынешнего вектора, направления
                penalty += distance(vCand, currentVXZ) * 1.2f;
    
                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    vBest = vCand;
                }
            }

            agent.BestVelocity = new(vBest.x, velocity.Linear.y, vBest.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float CalculatePenalty(float2 vCand, float2 vGoal, float2 posA, 
            float2 vA, float combinedRadiusSq, int agentsCount)
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