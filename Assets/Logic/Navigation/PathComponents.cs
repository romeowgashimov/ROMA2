using Unity.Entities;
using Unity.Mathematics;

namespace ROMA2.Logic.Navigation
{
    public struct PathFindingRequest : IEnableableComponent, IComponentData { }
    
    public struct IncorrectPathProperties : IEnableableComponent, IComponentData
    {
        // Ради дебага святое дело
        public bool PositionIsNotWalkable;
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
        public bool IsCleanPath;
        public bool ReachedTheTarget;
    }
    
    public struct RegisteredObstacleInGrid : IComponentData { }
    
    public struct IgnoreRegistrationInGrid : IComponentData { }

    public struct RVOAgent : IComponentData
    {
        public float3 BestVelocity;
        public float BodyRadius;
    }
    
    public struct AgentData
    {
        public float2 Position;
        public float2 Velocity;
    }
}