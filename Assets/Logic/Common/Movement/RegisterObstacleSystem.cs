using Logic.Client;
using Logic.Common;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace ROMA2.Logic.Common.Movement
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(PathFindingSystem))]
    public partial struct RegisterObstacleSystem : ISystem
    {
        private EntityQuery _nonAuthorizedObstaclesQuery;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GridTag>();

            _nonAuthorizedObstaclesQuery = QueryBuilder()
                .WithAll<PhysicsVelocity, LocalToWorld, PhysicsCollider>()
                .WithNone<MinionTag, ChampTag, RegisteredObstacleInGrid, IgnoreRegistrationInGrid>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Вот здесь это реально полезно
            if (_nonAuthorizedObstaclesQuery.CalculateEntityCount() == 0) return;

            Entity gridEntity = GetSingletonEntity<GridTag>();
            int2 gridSize = state.EntityManager.GetComponentData<GridSize>(gridEntity).Value;
            DynamicBuffer<PathNode> buffer = state.EntityManager.GetBuffer<PathNode>(gridEntity);

            EntityCommandBuffer ecb = GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            // Запускаем джобу и передаем ей буфер и размер сетки
            state.Dependency = new RegisterObstacleJob
            {
                PathNodes = buffer,
                GridSize = gridSize,
                GridBias = -60,
                ecb = ecb
            }.Schedule(_nonAuthorizedObstaclesQuery, state.Dependency);
            // Никто этого не видит
            state.Dependency.Complete();
        }
    }
    
    [BurstCompile]
    public partial struct RegisterObstacleJob : IJobEntity
    {
        public DynamicBuffer<PathNode> PathNodes;
        public int2 GridSize;
        public float3 GridBias;
        public EntityCommandBuffer ecb;
            
        // Добавляем PhysicsCollider в параметры, чтобы прочитать его форму
        private unsafe void Execute(
            in LocalToWorld transform, 
            in PhysicsCollider collider, 
            Entity obstacle)
        {
            // 1. Получаем выровненные по осям границы (AABB) из физического коллайдера с учетом его мирового положения и поворота
            Aabb worldAABB = collider.ColliderPtr->CalculateAabb(new(transform.Rotation, transform.Position));

            // 2. Переводим мировые координаты Min и Max границ в индексы сетки
            // Вычитаем GridOrigin и делим на CellSize, чтобы правильно спозиционировать на сетке
            int startX = (int)floor(worldAABB.Min.x - GridBias.x);
            int endX = (int)ceil(worldAABB.Max.x - GridBias.x);

            int startZ = (int)floor(worldAABB.Min.z - GridBias.z);
            int endZ = (int)ceil(worldAABB.Max.z - GridBias.z);

            // 3. Ограничиваем индексы, чтобы не выйти за пределы массива сетки (IndexOutOfRangeException)
            startX = max(0, startX);
            endX = min(GridSize.x - 1, endX);
            startZ = max(0, startZ);
            endZ = min(GridSize.y - 1, endZ);

            // 4. Бежим циклом по всем ячейкам, которые физически перекрывает этот объект
            for (int z = startZ; z <= endZ; z++)
                for (int x = startX; x <= endX; x++)
                {
                    int flatIndex = x + z * GridSize.x;
                        
                    PathNode node = PathNodes[flatIndex];
                    node.IsWalkable = false;
                    PathNodes[flatIndex] = node;
                }
            
            ecb.AddComponent<RegisteredObstacleInGrid>(obstacle);
        }
    }
}