using Logic.Client;
using Unity.Entities;
using Unity.Mathematics;


namespace Logic.Common
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializeGridSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GridTag>();
        }

        protected override void OnUpdate()
        {
            Enabled = false;
            
            Entity gridEntity = SystemAPI.GetSingletonEntity<GridTag>();
            int2 gridSize = EntityManager.GetComponentData<GridSize>(gridEntity).Value;
            DynamicBuffer<PathNode> buffer = EntityManager.AddBuffer<PathNode>(gridEntity);
            buffer.ResizeUninitialized(gridSize.x * gridSize.y);

            for(int x = 0; x < gridSize.x; x++)
                for(int y = 0; y < gridSize.y; y++)
                {
                        PathNode pathNode = new()
                        {
                            X = x,
                            Y = y,
                            Index = PathFindingJob.CalculateIndex(x, y, gridSize.x),
                            IsWalkable = true,
                            CameFromNodeIndex = -1
                        };
                        buffer[pathNode.Index] = pathNode;
                }
            
            EntityManager.SetName(gridEntity, "Grid");
        }
    }
}