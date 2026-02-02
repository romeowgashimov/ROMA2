using Assets.Logic.Client;
using Assets.Logic.Common;
using Logic.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Logic.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameEntrySystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;

        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);

            state.RequireForUpdate(_pendingNetworkIdQuery);
            state.RequireForUpdate<ClientTeamRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            TeamType requestedTeam = SystemAPI.GetSingleton<ClientTeamRequest>().Value;
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> pendingNetworkIds = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity pendingNetworkId in pendingNetworkIds)
            {
                ecb.AddComponent<NetworkStreamInGame>(pendingNetworkId);
                Entity requestTeamEntity = ecb.CreateEntity();
                ecb.AddComponent(requestTeamEntity, new TeamRequest { Value = requestedTeam });
                ecb.AddComponent(requestTeamEntity, new SendRpcCommandRequest { TargetConnection = pendingNetworkId });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}