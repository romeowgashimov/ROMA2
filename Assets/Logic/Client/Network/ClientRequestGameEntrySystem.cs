using System;
using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Client.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class ClientRequestGameEntrySystem : SystemBase
    {
        private EntityQuery _pendingNetworkIdQuery;

        private uint _championId = uint.MaxValue;
        private bool _championChosen;

        public Action OnChosen;

        protected override void OnCreate()
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = GetEntityQuery(builder);

            RequireForUpdate(_pendingNetworkIdQuery);
            RequireForUpdate<ClientTeamRequest>();
        }

        protected override void OnUpdate()
        {
            if (!_championChosen) return;
            
            TeamType requestedTeam = SystemAPI.GetSingleton<ClientTeamRequest>().Value;
            EntityCommandBuffer ecb = new(Allocator.Temp);
            
            NativeArray<Entity> pendingNetworkIds = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity pendingNetworkId in pendingNetworkIds)
            {
                ecb.AddComponent<NetworkStreamInGame>(pendingNetworkId);
                Entity requestTeamEntity = ecb.CreateEntity();
                ecb.AddComponent(requestTeamEntity, new EntryConnectionRequest { Team = requestedTeam, ChampionId = _championId });
                ecb.AddComponent(requestTeamEntity, new SendRpcCommandRequest { TargetConnection = pendingNetworkId });
            }

            ecb.Playback(EntityManager);
            OnChosen?.Invoke();
        }

        public void ChoiceChampion(uint id) =>
            _championId = id;

        public void ConfirmChampion()
        {
            if (_championId == uint.MaxValue) return;
            _championChosen = true;
        }
    }
}