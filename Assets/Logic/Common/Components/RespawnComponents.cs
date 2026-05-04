using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct RespawnEntityTag : IComponentData { }

    public struct RespawnBufferElement : IBufferElementData
    {
        [GhostField] public NetworkTick RespawnTick;
        [GhostField] public Entity NetworkEntity;
        [GhostField] public int NetworkId;
    }

    public struct RespawnTickCount : IComponentData
    {
        public uint Value;
    }

    public struct PlayerRespawnInfo : IComponentData
    {
        public Entity Champion;
        public TeamType Team;
        public float3 SpawnPosition;
    }

    public struct NetworkEntityReference : IComponentData
    {
        public Entity Value;
    }
}