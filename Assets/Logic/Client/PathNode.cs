using Unity.Entities;

namespace Logic.Client
{
    [InternalBufferCapacity(0)]
    public struct PathNode : IBufferElementData
    {
        public int X;
        public int Y;
        public int Index;
        public float GCost;
        public float HCost;
        public float FCost;
        public bool IsWalkable;
        public int CameFromNodeIndex;

        public void CalculateFCost() => 
            FCost = GCost + HCost;
            
        public void SetIsWalkable(bool isWalkable) =>
            IsWalkable = isWalkable;
    }
}