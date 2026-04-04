using Logic.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Logic.Client
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
                ecb.SetComponentEnabled<NeedPath>(owner, false);
                ecb.SetComponent(owner, new InputMoveTargetPosition
                {
                    Value = localTransform.ValueRO.Position, Flag = false
                });
            }

            ecb.Playback(state.EntityManager);
        }
    }
}