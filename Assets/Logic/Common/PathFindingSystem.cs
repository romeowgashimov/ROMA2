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
        private const int GRID_BIAS = 60;
        [ReadOnly] public int2 GridSize;
        [ReadOnly] public NativeArray<PathNode> Grid;
        public EntityCommandBuffer.ParallelWriter ECB;
    
        private struct NodeSearchData
        {
            public float GCost;
            public int CameFromIndex;
        }
        
        private struct HeapNode
        {
            public int Index;
            public float FCost;
        }
    
        public void Execute([EntityIndexInQuery] int key, Entity entity, in MoveTargetPosition target, 
            LocalTransform transform, ref DynamicBuffer<PathPositionElement> buffer)
        {
            int2 startPos = (int2)math.round(transform.Position.xz) + GRID_BIAS;
            int2 endPos = (int2)math.round(target.Value.xz) + GRID_BIAS;
    
            if (IsOutside(startPos) || IsOutside(endPos)) return;
    
            int startIndex = CalculateIndex(startPos.x, startPos.y, GridSize.x);
            int endIndex = CalculateIndex(endPos.x, endPos.y, GridSize.x);
    
            if (!Grid[endIndex].IsWalkable) return;
            
            NativeParallelHashMap<int, NodeSearchData> visitedNodes = new(128, Allocator.Temp);
            NativeList<HeapNode> openList = new(128, Allocator.Temp);
            NativeHashSet<int> closedSet = new(128, Allocator.Temp);
    
            visitedNodes.Add(startIndex, new() { GCost = 0, CameFromIndex = -1 });
            PushHeap(ref openList, new() { Index = startIndex, FCost = GetH(startPos, endPos) });
    
            bool pathFound = false;
            int iterations = 0;
    
            while (openList.Length > 0 && iterations < 2000)
            {
                iterations++;
                
                HeapNode currentHeapNode = PopHeap(ref openList);
                int currentIndex = currentHeapNode.Index;
    
                if (currentIndex == endIndex) { pathFound = true; break; }
    
                closedSet.Add(currentIndex);
                int2 curPos = GetPos(currentIndex);
                float currentGCost = visitedNodes[currentIndex].GCost;
    
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        
                        int2 nPos = curPos + new int2(x, y);
                        if (IsOutside(nPos)) continue;
    
                        int nIdx = CalculateIndex(nPos.x, nPos.y, GridSize.x);
                        if (closedSet.Contains(nIdx) || !Grid[nIdx].IsWalkable) continue;
                        
                        float moveCost = (x != 0 && y != 0) ? 1.4142f : 1f;
                        float newGCost = currentGCost + moveCost;
    
                        if (!visitedNodes.TryGetValue(nIdx, out NodeSearchData neighborData) || newGCost < neighborData.GCost)
                        {
                            float hCost = GetH(nPos, endPos);
                            visitedNodes[nIdx] = new() { GCost = newGCost, CameFromIndex = currentIndex };
                            PushHeap(ref openList, new() { Index = nIdx, FCost = newGCost + hCost });
                        }
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
                ECB.SetComponentEnabled<NeedPath>(key, entity, false);
                ECB.SetComponent(key, entity, new FollowPathIndex { Value = buffer.Length - 1 });
            }
    
            visitedNodes.Dispose();
            openList.Dispose();
            closedSet.Dispose();
        }
        
        private void PushHeap(ref NativeList<HeapNode> heap, HeapNode node)
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
    
        private HeapNode PopHeap(ref NativeList<HeapNode> heap)
        {
            HeapNode root = heap[0];
            heap[0] = heap[^1];
            heap.RemoveAtSwapBack(heap.Length - 1);
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
    
        private float GetH(int2 a, int2 b)
        {
            int dx = math.abs(a.x - b.x);
            int dy = math.abs(a.y - b.y);
            return (dx > dy) ? (1.4142f * dy + (dx - dy)) : (1.4142f * dx + (dy - dx));
        }
    
        private bool IsOutside(int2 pos) => math.any(pos < 0) || math.any(pos >= GridSize);
        private int CalculateIndex(int x, int y, int w) => x + y * w;
        private int2 GetPos(int i) => new(i % GridSize.x, i / GridSize.x);
    }
}