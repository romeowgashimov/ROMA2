using System.Runtime.CompilerServices;
using Logic.Client;
using Logic.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;
using static Unity.Mathematics.math;

namespace ROMA2.Logic.Common.Movement
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PathFindingSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = QueryBuilder()
                .WithAll<MoveTargetPosition, LocalTransform, MoveSpeed, 
                    PathPositionElement, PathFindingRequest, Simulate, FollowPathProperties>()
                .WithNone<DestroyEntityTag>()
                .Build();

            state.RequireForUpdate(_query);
            state.RequireForUpdate<GridTag>();
            state.RequireForUpdate<PathNode>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = GetSingletonEntity<GridTag>();
            int2 gridSize = state.EntityManager
                .GetComponentData<GridSize>(gridEntity).Value;
            NativeArray<PathNode> buffer = state.EntityManager
                .GetBuffer<PathNode>(gridEntity, true)
                .AsNativeArray();

            EntityCommandBuffer.ParallelWriter ecb = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            // Передаем управление памятью внутрь Execute. Цикл очистки на главном потоке УДАЛЕН.
            state.Dependency = new PathFindingJob
            {
                GridSize = gridSize,
                Grid = buffer,
                ECB = ecb
            }.ScheduleParallel(_query, state.Dependency);
        }
    }

    public struct NodeSearchData
    {
        public int GCost; // Изменено на int для ускорения А*
        public int CameFromIndex;
    }

    public struct HeapNode
    {
        public int Index;
        public int FCost; // Изменено на int для ускорения кучи
    }

    /* Сделать рейкасты, если путь чист, то ecb.SetComponentEnabled<CleanPath>, иначе ищем путь.
     Сделать систему очередей на создание пути RequestHandler */
    [BurstCompile]
    public partial struct PathFindingJob : IJobEntity
    {
        private const int GRID_BIAS = 60;
        private const int MAX_ITERATIONS = 14400;

        [ReadOnly] public int2 GridSize;
        [ReadOnly] public NativeArray<PathNode> Grid;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute(
            [ChunkIndexInQuery] int key, 
            in Entity entity, 
            in MoveTargetPosition target, 
            in LocalTransform transform, 
            ref DynamicBuffer<PathPositionElement> buffer,
            ref FollowPathProperties pathProperties)
        {
            int2 startPos = (int2)round(transform.Position.xz) + GRID_BIAS;
            int2 endPos = (int2)round(target.Value.xz) + GRID_BIAS;

            if (IsOutside(startPos)) return;

            // 10 клеток — максимальный радиус поиска альтернативной точки 
            if (!FindNearestWalkablePosition(endPos, 10, out int2 walkableEndPos))
            {
                // Если даже рядом нет свободных точек, отменяем запрос
                ECB.SetComponentEnabled<PathFindingRequest>(key, entity, false);
                ECB.SetComponent(key, entity, new IncorrectPathProperties
                {
                    TargetPositionIsNotWalkable = true,
                    NotEnoughIterations = false
                });
                ECB.SetComponentEnabled<IncorrectPathProperties>(key, entity, true);
                return;
            }

            // Перезаписываем конечную точку на найденную проходимую
            endPos = walkableEndPos; 
            int startIndex = CalculateIndex(startPos.x, startPos.y, GridSize.x);
            int endIndex = CalculateIndex(endPos.x, endPos.y, GridSize.x);
            
            UnsafeHashMap<int, NodeSearchData> visitedNodes = new(1024, Allocator.Temp);
            UnsafeList<HeapNode> openList = new(1024, Allocator.Temp);
            UnsafeHashSet<int> closedSet = new(1024, Allocator.Temp);
            
            visitedNodes.TryAdd(startIndex, new() { GCost = 0, CameFromIndex = -1 });
            PushHeap(ref openList, new() { Index = startIndex, FCost = GetH(startPos, endPos) });

            bool pathFound = false;
            int iterations = 0;

            while (openList.Length > 0 && iterations < MAX_ITERATIONS)
            {
                iterations++;

                HeapNode currentHeapNode = PopHeap(ref openList);
                int currentIndex = currentHeapNode.Index;

                if (currentIndex == endIndex) { pathFound = true; break; }

                closedSet.Add(currentIndex);
                int2 curPos = GetPos(currentIndex);
                int currentGCost = visitedNodes[currentIndex].GCost;

                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
    
                        int2 nPos = curPos + new int2(x, y);
                        if (IsOutside(nPos)) continue;
    
                        int nIdx = CalculateIndex(nPos.x, nPos.y, GridSize.x);
                        if (closedSet.Contains(nIdx) || !Grid[nIdx].IsWalkable) continue;
    
                        // Оптимизация стоимости перемещения: 10 за прямые, 14 за диагонали
                        int moveCost = x != 0 && y != 0 ? 14 : 10; 
                        int newGCost = currentGCost + moveCost;
    
                        if (!visitedNodes.TryGetValue(nIdx, out NodeSearchData neighborData) || newGCost < neighborData.GCost)
                        {
                            int hCost = GetH(nPos, endPos);
                            visitedNodes[nIdx] = new() { GCost = newGCost, CameFromIndex = currentIndex };
                            PushHeap(ref openList, new() { Index = nIdx, FCost = newGCost + hCost });
                        }
                    }
            }
            
            if (pathFound)
            {
                buffer.Clear();
                int curr = endIndex;
                while (curr != -1)
                {
                    int2 p = GetPos(curr);
                    buffer.Add(new() { Value = new(p.x - GRID_BIAS, p.y - GRID_BIAS) });
                    curr = visitedNodes[curr].CameFromIndex;
                }
                ECB.SetComponentEnabled<PathFindingRequest>(key, entity, false);
                ECB.SetComponent(key, entity, new FollowPathProperties
                {
                    // Персонаж дёргается из-за отсечения позиции в int, первая позиция может быть чуть позади
                    Index = buffer.Length - 2,
                    IsNewPath = true
                });
                ECB.SetComponentEnabled<IncorrectPathProperties>(key, entity, false);
            }
            else
            {
                ECB.SetComponentEnabled<PathFindingRequest>(key, entity, false);
                ECB.SetComponent(key, entity, new IncorrectPathProperties
                {
                    TargetPositionIsNotWalkable = false,
                    NotEnoughIterations = true
                });
                ECB.SetComponentEnabled<IncorrectPathProperties>(key, entity, true);
            }
            
            visitedNodes.Dispose();
            openList.Dispose();
            closedSet.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushHeap(ref UnsafeList<HeapNode> heap, HeapNode node)
        {
            heap.Add(node);
            int i = heap.Length - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (heap[p].FCost <= heap[i].FCost) break;
                (heap[p], heap[i]) = (heap[i], heap[p]);
                i = p;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HeapNode PopHeap(ref UnsafeList<HeapNode> heap)
        {
            HeapNode root = heap[0];
            heap[0] = heap[^1];
            heap.RemoveAt(heap.Length - 1);
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1;
                int r = i * 2 + 2;
                int smallest = i;
                if (l < heap.Length && heap[l].FCost < heap[smallest].FCost) smallest = l;
                if (r < heap.Length && heap[r].FCost < heap[smallest].FCost) smallest = r;
                if (smallest == i) break;

                (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
                i = smallest;
            }
            return root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetH(int2 a, int2 b)
        {
            int dx = abs(a.x - b.x);
            int dy = abs(a.y - b.y);
            // Целочисленный подсчет Манхэттена с учетом диагоналей (10 и 14)
            return dx > dy ? 14 * dy + 10 * (dx - dy) : 14 * dx + 10 * (dy - dx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutside(int2 pos) => any(pos < 0) || any(pos >= GridSize);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateIndex(int x, int y, int w) => x + y * w;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int2 GetPos(int i) => new(i % GridSize.x, i / GridSize.x);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FindNearestWalkablePosition(int2 startPos, int maxRadius, out int2 foundPos)
        {
            // Если точка вне сетки, мы не можем проверить её проходимость напрямую
            if (!IsOutside(startPos))
            {
                int startIndex = CalculateIndex(startPos.x, startPos.y, GridSize.x);
                if (Grid[startIndex].IsWalkable)
                {
                    foundPos = startPos;
                    return true;
                }
            }
        
            // Поиск по слоям (окружающим квадратам) наружу
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                // Проходим по верхней и нижней сторонам квадрата
                for (int x = -radius; x <= radius; x++)
                {
                    // Верхняя грань
                    int2 checkPos = startPos + new int2(x, radius);
                    if (!IsOutside(checkPos) && Grid[CalculateIndex(checkPos.x, checkPos.y, GridSize.x)].IsWalkable)
                    {
                        foundPos = checkPos;
                        return true;
                    }
        
                    // Нижняя грань
                    checkPos = startPos + new int2(x, -radius);
                    if (!IsOutside(checkPos) && Grid[CalculateIndex(checkPos.x, checkPos.y, GridSize.x)].IsWalkable)
                    {
                        foundPos = checkPos;
                        return true;
                    }
                }
        
                // Проходим по левой и правой сторонам квадрата (уголки уже проверены выше)
                for (int y = -radius + 1; y < radius; y++)
                {
                    // Правая грань
                    int2 checkPos = startPos + new int2(radius, y);
                    if (!IsOutside(checkPos) && Grid[CalculateIndex(checkPos.x, checkPos.y, GridSize.x)].IsWalkable)
                    {
                        foundPos = checkPos;
                        return true;
                    }
        
                    // Левая грань
                    checkPos = startPos + new int2(-radius, y);
                    if (!IsOutside(checkPos) && Grid[CalculateIndex(checkPos.x, checkPos.y, GridSize.x)].IsWalkable)
                    {
                        foundPos = checkPos;
                        return true;
                    }
                }
            }
        
            // Если в пределах радиуса ничего не нашли
            foundPos = startPos;
            return false;
        }
    }
}