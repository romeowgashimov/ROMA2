using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Logic.Common
{
    public struct EntryConnectionRequest : IRpcCommand
    {
        public TeamType Team;
        public uint ChampionId;
    }
    
    public struct PendingSpawn : IComponentData
    {
        public Entity RequestSourceConnection;
        public float3 SpawnPos;
        public int CharacterId;
        public TeamType Team;
        public int ClientId;
    }

    public struct LoadCharacterRequest : IRpcCommand
    {
        public uint CharacterId;
    }

    public struct LoadedEntity : IComponentData
    {
        public Entity Value;
    }

    public struct PlayersRemainingToStart : IRpcCommand
    {
        public int Value;
    }

    public struct GameStartTickRpc : IRpcCommand
    {
        public NetworkTick Value;
    }
}