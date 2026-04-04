using Unity.NetCode;

namespace Logic.Common
{
    public struct TeamRequest : IRpcCommand
    {
        public TeamType Value;
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