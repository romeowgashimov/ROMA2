using Unity.Burst;
using Logic.Client;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Mathematics.math;

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

            PathFindingJob pathFindingJob = new()
            {
                GridSize = gridSize,
                Grid = buffer,
                ECB = ecb.AsParallelWriter()
            };

            state.Dependency = pathFindingJob.ScheduleParallel(_query, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct PathFindingJob : IJobEntity
    {
        private const int GLOBAL_GRID_SIZE = 100;
        private const int GRID_BIAS = 50;
        private int _matrixBias;
        private int _localBias;
        private int4 _localGridSize;

        public int2 GridSize;
        [ReadOnly] public DynamicBuffer<PathNode> Grid;

        private void Execute([EntityIndexInQuery] int key, Entity entity, in MoveTargetPosition target, 
            LocalTransform transform, ref DynamicBuffer<PathPositionElement> buffer)
        {
            int2 startPosition = new((int)transform.Position.x, (int)transform.Position.z);
            int2 endPosition = new((int)target.Value.x, (int)target.Value.z);
            startPosition += GRID_BIAS;
            endPosition += GRID_BIAS;
            
            int startX = min(startPosition.x, endPosition.x);
            int endX = max(startPosition.x, endPosition.x);
            int startY = min(startPosition.y, endPosition.y);
            int endY = max(startPosition.y, endPosition.y);
            _localGridSize = new(startX, startY, endX + 2,endY + 2);
            _localBias = GridSize.x - endX;

            int startIndex = CalculateIndex(startPosition.x, startPosition.y, GridSize.x);
            int endIndex = CalculateIndex(endPosition.x, endPosition.y, GridSize.x);
            startIndex = LocalizeIndex(startIndex);
            endIndex = LocalizeIndex(endIndex);

            int bias = CalculateIndex(startX, startY, GridSize.x);
            bias = LocalizeIndex(bias);
            _matrixBias = bias;
            startIndex -= _matrixBias;
            endIndex -= _matrixBias;

            int length = CalculateIndex(endX + 2, endY + 2, GridSize.x);
            length = LocalizeIndex(length) + 1;
            NativeArray<PathNode> localPathNodes = new(length, Allocator.Temp);
            for(int localX = startX; localX <= endX; localX++)
            for(int localY = startY; localY <= endY; localY++)
            {
                int globalIndex = CalculateIndex(localX, localY, GridSize.x);
                int localIndex = LocalizeIndex(globalIndex);
                PathNode localPathNode = Grid[globalIndex];
                localPathNode.Index = localIndex;
                
                localPathNode.CameFromNodeIndex = -1;
                localPathNode.GCost = int.MaxValue;
                localPathNode.HCost = CalculateDistanceCost(new(localPathNode.X, localPathNode.Y), endPosition);
                localPathNode.CalculateFCost();
                
                localPathNodes[localIndex] = localPathNode;
            }

            NativeArray<int2> neighbourOffsetArray = new(8, Allocator.Temp);
            neighbourOffsetArray[4] = new(-1, -1);
            neighbourOffsetArray[5] = new(-1, +1);
            neighbourOffsetArray[6] = new(+1, -1);
            neighbourOffsetArray[7] = new(+1, +1);
            neighbourOffsetArray[0] = new(-1, 0);
            neighbourOffsetArray[1] = new(+1, 0);
            neighbourOffsetArray[2] = new(0, +1);
            neighbourOffsetArray[3] = new(0, -1);

            PathNode startNode = localPathNodes[startIndex];
            startNode.GCost = 0;
            startNode.CalculateFCost();
            localPathNodes[startNode.Index] = startNode;

            NativeList<int> openList = new(Allocator.Temp);
            NativeList<int> closedList = new(Allocator.Temp);

            openList.Add(startNode.Index);

            while(openList.Length > 0)
            {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, localPathNodes);
                PathNode currentPathNode = localPathNodes[currentNodeIndex];

                if(currentNodeIndex == endIndex)
                    break;

                for(int i = 0; i < openList.Length; i++)
                {
                    if(openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }
            
                closedList.Add(currentNodeIndex);

                for(int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new(currentPathNode.X + neighbourOffset.x, currentPathNode.Y + neighbourOffset.y);

                    if(!IsPositionInsideGrid(neighbourPosition)) continue;

                    int neighbourIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, GridSize.x);
                    neighbourIndex = LocalizeIndex(neighbourIndex);
                    
                    if(closedList.Contains(neighbourIndex)) continue;

                    PathNode neighbourNode = localPathNodes[neighbourIndex];
                    if(!neighbourNode.IsWalkable) continue;

                    int2 currentNodePosition = new(currentPathNode.X, currentPathNode.Y);
                    float tentativeGCost = currentPathNode.GCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                    if(tentativeGCost < neighbourNode.GCost)
                    {
                        neighbourNode.CameFromNodeIndex = currentNodeIndex;
                        neighbourNode.GCost = tentativeGCost;
                        neighbourNode.CalculateFCost();
                        localPathNodes[neighbourIndex] = neighbourNode;

                        if(!openList.Contains(neighbourIndex))
                            openList.Add(neighbourIndex);
                    }
                }
            }

            buffer.Clear();

            PathNode endNode = localPathNodes[endIndex];
            if(endNode.CameFromNodeIndex != -1)
            {
                CalculatePath(localPathNodes, endNode, buffer);
                ECB.SetComponentEnabled<NeedPath>(key, entity, false);
                ECB.SetComponent<FollowPathIndex>(key, entity, new()
                {
                    Value = buffer.Length - 1
                });
            }

            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
            localPathNodes.Dispose();
        }

        public EntityCommandBuffer.ParallelWriter ECB;

        private void CalculatePath(NativeArray<PathNode> pathNodes, PathNode endNode, DynamicBuffer<PathPositionElement> buffer)
        {
            if(endNode.CameFromNodeIndex == -1) return;

            buffer.Add(new() { Value = new(endNode.X - GRID_BIAS, endNode.Y - GRID_BIAS) });
            
            PathNode currentNode = endNode;
            while(currentNode.CameFromNodeIndex != -1)
            {
                PathNode cameFromNode = pathNodes[currentNode.CameFromNodeIndex];
                buffer.Add(new() { Value = new(cameFromNode.X - GRID_BIAS, cameFromNode.Y - GRID_BIAS) });
                currentNode = cameFromNode;
            }
            
            buffer.RemoveAtSwapBack(buffer.Length - 1);
        }

        private float CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            float distance = math.distance(aPosition, bPosition);
            return distance;
        }

        private bool IsPositionInsideGrid(int2 gridPosition) => 
            gridPosition.x >= _localGridSize[0] && gridPosition.y >= _localGridSize[1]
            && gridPosition.x <= _localGridSize[2] && gridPosition.y <= _localGridSize[3];

        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodes)
        {
            PathNode lowestFCostNode = pathNodes[openList[0]];
            foreach(int index in openList)
            {
                PathNode testPathNode = pathNodes[index];
                if(testPathNode.FCost < lowestFCostNode.FCost)
                    lowestFCostNode = testPathNode;
            }

            return lowestFCostNode.Index;
        }

        public static int CalculateIndex(int x, int y, int gridWidth) =>
            x + y * gridWidth;

        private int LocalizeIndex(int globalIndex)
        {
            globalIndex = abs(globalIndex - _localBias * (globalIndex / GLOBAL_GRID_SIZE));
            return globalIndex - _matrixBias;
        }
    }
}