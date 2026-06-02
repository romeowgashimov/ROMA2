using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace ROMA2.Logic.Client.Initialization
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InitializeChampLocalSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            foreach((RefRO<LocalTransform> localTransform, Entity owner) in SystemAPI
            .Query<RefRO<LocalTransform>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithNone<OwnerChampTag>()
            .WithEntityAccess())
            {
                ecb.AddComponent<OwnerChampTag>(owner);
                ecb.SetComponentEnabled<PathFindingRequest>(owner, false);
                ecb.SetComponent(owner, new InputMoveTargetPosition
                {
                    Value = localTransform.ValueRO.Position, Flag = false
                });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}