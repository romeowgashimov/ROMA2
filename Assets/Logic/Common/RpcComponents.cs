using Assets.Logic.Common;
using Unity.NetCode;

namespace Logic.Common
{
    public struct TeamRequest : IRpcCommand
    {
        public TeamType Value;
    }
}