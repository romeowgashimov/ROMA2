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
                    PathPositionElement, PathFindingRequest, Simulate>()
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
                ECB = ecb,
                ProcessingLookup = GetComponentLookup<PathFindingRequest>(true)
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

    [BurstCompile]
    public partial struct PathFindingJob : IJobEntity
    {
        private const int GRID_BIAS = 60;
        private const int MAX_ITERATIONS = 500; // Лимит итераций для защиты от зависаний CPU

        [ReadOnly] public int2 GridSize;
        [ReadOnly] public NativeArray<PathNode> Grid;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<PathFindingRequest> ProcessingLookup;

        private void Execute(
            [EntityIndexInQuery] int key, 
            in Entity entity, 
            in MoveTargetPosition target, 
            in LocalTransform transform, 
            ref DynamicBuffer<PathPositionElement> buffer)
        {
            if (!ProcessingLookup.HasComponent(entity)) return;
            
            int2 startPos = (int2)round(transform.Position.xz) + GRID_BIAS;
            int2 endPos = (int2)round(target.Value.xz) + GRID_BIAS;

            if (IsOutside(startPos) || IsOutside(endPos)) return;

            int startIndex = CalculateIndex(startPos.x, startPos.y, GridSize.x);
            int endIndex = CalculateIndex(endPos.x, endPos.y, GridSize.x);

            // Я могу пойти на недоступную клетку,
            // потому что поиск пути никак не уведомляет о невозможности построить путь
            if (!Grid[endIndex].IsWalkable && ProcessingLookup.HasComponent(entity))
            {
                ECB.SetComponentEnabled<PathFindingRequest>(key, entity, false);
                return;
            }
            
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

            bool hasComponent = ProcessingLookup.HasComponent(entity);
            if (pathFound && hasComponent)
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
                ECB.SetComponent(key, entity, new FollowPathProperties { Index = buffer.Length - 1 });
            }
            else if (hasComponent)
                // Если путь не найден за лимит итераций, выключаем запрос
                ECB.SetComponentEnabled<PathFindingRequest>(key, entity, false);
            
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
    }
}