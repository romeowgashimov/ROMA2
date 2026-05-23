using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct PathFindingRequest : IEnableableComponent, IComponentData { }
    
    public struct PathFindingProcessing  : IEnableableComponent, IComponentData { }

    public struct CleanPath : IEnableableComponent, IComponentData { }
    
    public struct IncorrectPathProperties : IEnableableComponent, IComponentData
    {
        public bool TargetPositionIsNotWalkable;
        public bool NotEnoughIterations;
    }
    
    [InternalBufferCapacity(4)]
    public struct PathPositionElement : IBufferElementData
    {
        public int2 Value;
    }
    
    public struct FollowPathProperties : IComponentData
    {
        public int Index;
        public bool IsNewPath;
        public float WaitingTime;
    }
    
    public struct RegisteredObstacleInGrid : IComponentData { }
    
    public struct IgnoreRegistrationInGrid : IComponentData { }
    
    public struct RVOAgent : IComponentData { }
}