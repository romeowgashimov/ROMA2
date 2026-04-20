using Unity.Burst;
using Logic.Client;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Common
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PathFindingSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MoveTargetPosition, LocalTransform, MoveSpeed, PathPositionElement, NeedPath, Simulate>();
            _query = state.GetEntityQuery(builder);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<PathNode>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

            builder.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if(_query.CalculateEntityCount() == 0) return;
            
            Entity gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            int2 gridSize = state.EntityManager.GetComponentData<GridSize>(gridEntity).Value;
            DynamicBuffer<PathNode> buffer = state.EntityManager.GetBuffer<PathNode>(gridEntity, true);
            
            BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = 
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new PathFindingJob
            {
                GridSize = gridSize,
                Grid = buffer.AsNativeArray(),
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(_query, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct PathFindingJob : IJobEntity
    {
        private const int GRID_BIAS = 60; // Твое смещение из прошлого кода
        [ReadOnly] public int2 GridSize;
        [ReadOnly] public NativeArray<PathNode> Grid;
        public EntityCommandBuffer.ParallelWriter ECB;
    
        private struct NodeState
        {
            public float GCost;
            public int CameFromIndex;
        }
    
        public void Execute([EntityIndexInQuery] int key, Entity entity, in MoveTargetPosition target, 
            LocalTransform transform, ref DynamicBuffer<PathPositionElement> buffer)
        {
            if (entity == Entity.Null) return;
            
            // 1. ПЕРЕВОД КООРДИНАТ (Мир -> Сетка)
            // Если мир -60, то в сетке это 0.
            int2 startPos = new((int)math.round(transform.Position.x) + GRID_BIAS, (int)math.round(transform.Position.z) + GRID_BIAS);
            int2 endPos = new((int)math.round(target.Value.x) + GRID_BIAS, (int)math.round(target.Value.z) + GRID_BIAS);
    
            // 2. ПРОВЕРКИ
            if (IsOutside(startPos) || IsOutside(endPos)) return;
    
            int startIndex = CalculateIndex(startPos.x, startPos.y, GridSize.x);
            int endIndex = CalculateIndex(endPos.x, endPos.y, GridSize.x);
    
            if (!Grid[endIndex].IsWalkable) return; // Если цель в стене — не ищем
    
            // 3. ПОИСК (Оптимизированный)
            NativeArray<NodeState> nodeStates = new(Grid.Length, Allocator.Temp);
            NativeBitArray closedSet = new(Grid.Length, Allocator.Temp);
            NativeList<int> openList = new(Allocator.Temp);
    
            for (int i = 0; i < nodeStates.Length; i++)
                nodeStates[i] = new() { GCost = float.MaxValue, CameFromIndex = -1 };
    
            nodeStates[startIndex] = new() { GCost = 0, CameFromIndex = -1 };
            openList.Add(startIndex);
    
            bool pathFound = false;
            int safety = 0;
    
            while (openList.Length > 0 && safety < 10000)
            {
                safety++;
                int currentIndex = -1;
                float minF = float.MaxValue;
    
                // Выбираем лучший узел (F = G + H)
                for (int i = 0; i < openList.Length; i++)
                {
                    int idx = openList[i];
                    float h = math.distance(GetPos(idx), endPos);
                    float f = nodeStates[idx].GCost + h;
                    if (f < minF) { minF = f; currentIndex = idx; }
                }
    
                if (currentIndex == endIndex) { pathFound = true; break; }
    
                // Удаляем текущий
                for (int i = 0; i < openList.Length; i++)
                    if (openList[i] == currentIndex) { openList.RemoveAtSwapBack(i); break; }
                
                closedSet.Set(currentIndex, true);
    
                int2 curPos = GetPos(currentIndex);
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        int2 nPos = curPos + new int2(x, y);
                        if (IsOutside(nPos)) continue;
    
                        int nIdx = CalculateIndex(nPos.x, nPos.y, GridSize.x);
                        if (closedSet.IsSet(nIdx) || !Grid[nIdx].IsWalkable) continue;
    
                        float dist = (x != 0 && y != 0) ? 1.41f : 1f; // Диагональ чуть дороже
                        float tGCost = nodeStates[currentIndex].GCost + dist;
    
                        if (tGCost < nodeStates[nIdx].GCost)
                        {
                            nodeStates[nIdx] = new() { GCost = tGCost, CameFromIndex = currentIndex };
                            if (!Contains(openList, nIdx)) openList.Add(nIdx);
                        }
                    }
                }
            }
    
            // 4. ЗАПИСЬ (Мир <- Сетка)
            if (pathFound)
            {
                buffer.Clear();
                int curr = endIndex;
                while (curr != -1)
                {
                    int2 p = GetPos(curr);
                    // Возвращаем в мировые координаты: вычитаем BIAS
                    buffer.Add(new() { Value = new(p.x - GRID_BIAS, p.y - GRID_BIAS) });
                    curr = nodeStates[curr].CameFromIndex;
                }
                
                if (entity != Entity.Null)
                {
                    ECB.SetComponentEnabled<NeedPath>(key, entity, false);
                    ECB.SetComponent(key, entity, new FollowPathIndex { Value = buffer.Length - 1 });
                }
            }
    
            nodeStates.Dispose();
            closedSet.Dispose();
            openList.Dispose();
        }
    
        private bool IsOutside(int2 pos) => pos.x < 0 || pos.x >= GridSize.x || pos.y < 0 || pos.y >= GridSize.y;
        private int CalculateIndex(int x, int y, int w) => x + y * w;
        private int2 GetPos(int i) => new(i % GridSize.x, i / GridSize.x);
        private bool Contains(NativeList<int> list, int val) 
        {
            for (int i = 0; i < list.Length; i++) if (list[i] == val) 
                return true;
            return false;
        }
    }

}
