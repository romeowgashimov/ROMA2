using ROMA2.Logic.Data;
using Unity.Entities;

namespace ROMA2.Logic.Client.Network
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }
}