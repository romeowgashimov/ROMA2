using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

namespace Logic.Common
{
    public struct AgentData
    {
        public float2 Position;
        public float2 Velocity;
        public float Radius;
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
                K = 25.0f,      // Константа из Eq. 8 (влияет на дистанцию реагирования)
                MaxSamples = 10, // Количество лучей для поиска лучшей скорости
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
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
                Radius = radius.Value
            };
        }
    }

    [BurstCompile]
    public partial struct MinionMoveJob : IJobEntity
    {
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public NativeArray<AgentData> AllAgents;
        public float K;
        public int MaxSamples;

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
                velocity.Linear = float3.zero;
                return;
            }
            
            if (attackTarget.Value != Entity.Null)
            {
                float3 targetPos = TransformLookup[attackTarget.Value].Position;
                if (distance(targetPos, transform.Position) <= radius.Value - 1f)
                {
                    velocity.Linear = float3.zero;
                    return;
                }
            }

            const float bodyRadius = 0.5f; // Фиксированный радиус тела миньона
            float2 selfPos = transform.Position.xz;
            float2 currentV = velocity.Linear.xz;
            int currentIndex = followPathIndex.Value;
            float2 targetPath = pathPositions[currentIndex].Value;
            
            float2 vGoal = normalizesafe(targetPath - selfPos) * moveSpeed.Value;
            float2 vBest = vGoal;
            float minPenalty = float.MaxValue;
    
            // Добавляем текущую скорость в сэмплы, чтобы уменьшить тряску
            for (int i = 0; i <= MaxSamples; i++)
            {
                float2 vCandidate;
                if (i == 0) vCandidate = vGoal; // Сначала проверяем идеальный путь
                else 
                {
                    float angle = (PI * 2f / MaxSamples) * i;
                    vCandidate = new float2(cos(angle), sin(angle)) * moveSpeed.Value;
                }
    
                // ВАЖНО: Передаем bodyRadius (0.5)
                float penalty = CalculatePenalty(vCandidate, vGoal, selfPos, currentV, bodyRadius);
                
                penalty += distance(vCandidate, currentV) * 1.2f; 
                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    vBest = vCandidate;
                }
            }
    
            // --- 4. ПРИМЕНЕНИЕ И ПЛАВНОСТЬ ---
            // Плавная интерполяция скорости, чтобы убрать тряску (lerp)
            float2 finalV = lerp(currentV, vBest, 0.2f); 
            velocity.Linear = new(finalV.x, velocity.Linear.y, finalV.y);
            
            if (lengthsq(finalV) > 0.01f)
                transform.Rotation = quaternion.LookRotationSafe(new float3(finalV.x, 0, finalV.y), math.up()); 
            
            // --- Логика переключения вейпоинтов (Dot Product) ---
            float2 futurePos = currentIndex > 0 && currentIndex < pathPositions.Length
                ? pathPositions[currentIndex - 1].Value 
                : selfPos;

            float2 segmentDir = normalize(targetPath - futurePos);
            float2 vectorToTarget = targetPath - selfPos;

            // Если пролетели плоскость вейпоинта (даже если RVO оттолкнул в бок)
            if (dot(vectorToTarget, segmentDir) >= -0.2f)
            {
                followPathIndex.Value--;
                if (followPathIndex.Value < 0)
                {
                    velocity.Linear = float3.zero;
                }
            } 
        }

        private float CalculatePenalty(float2 vCand, float2 vGoal, float2 posA, float2 vA, float radA)
        {
            float tc = 5.0f; // Уменьшим горизонт планирования для резкости
    
            for (int i = 0; i < AllAgents.Length; i++)
            {
                AgentData agentB = AllAgents[i];
                float2 relPos = agentB.Position - posA;
                float distSq = lengthsq(relPos);
                
                // Игнорируем себя и тех, кто далеко
                if (distSq < 0.001f || distSq > 16f) continue; 
    
                // Используем МАЛЕНЬКИЙ радиус для всех (0.5 + 0.5 = 1.0)
                float combinedRadius = radA + 0.5f; 
                
                float2 vRVO = (vCand + vA) * 0.5f; 
                float2 relVel = vRVO - agentB.Velocity;
    
                float dist = sqrt(distSq);
                float alphaAB = atan2(relPos.y, relPos.x);
                float phiAB = asin(clamp(combinedRadius / dist, -1f, 1f));
                float betaAB = atan2(relVel.y, relVel.x);
    
                float psiAB = abs(betaAB - alphaAB);
                if (psiAB > PI) psiAB = 2f * PI - psiAB;
    
                if (psiAB < phiAB)
                {
                    float vRelMag = length(relVel);
                    // Формула времени до столкновения
                    float tcCand = (dist * cos(psiAB) - sqrt(max(0, combinedRadius * combinedRadius - distSq * sin(psiAB) * sin(psiAB)))) / max(vRelMag, 0.01f);
                    
                    if (tcCand > 0 && tcCand < tc) tc = tcCand;
                }
            }
    
            // Увеличиваем значимость отклонения от vGoal, чтобы меньше "гуляли"
            return K / tc + distance(vCand, vGoal) * 1.5f;
        }
    }
}

