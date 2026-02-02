using Assets.Logic.Common;
using Unity.Entities;

namespace Assets.Logic.Client
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }
}