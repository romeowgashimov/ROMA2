using Logic.Common;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Logic.Server
{
    public class MinionPathAuthoring : MonoBehaviour
    {
        public Vector3[] TopLanePath;
        public Vector3[] MidLanePath;
        public Vector3[] BotLanePath;
        
        public class MinionPathBaker : Baker<MinionPathAuthoring>
        {
            public override void Bake(MinionPathAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                Entity topLane = CreateAdditionalEntity(TransformUsageFlags.None, false, "TopLane");
                Entity midLane = CreateAdditionalEntity(TransformUsageFlags.None, false, "MidLane");
                Entity botLane = CreateAdditionalEntity(TransformUsageFlags.None, false, "BotLane");
                
                DynamicBuffer<MinionPathPosition> topLaneBuffer = AddBuffer<MinionPathPosition>(topLane);
                foreach (float3 pathPosition in authoring.TopLanePath)
                    topLaneBuffer.Add(new() { Value = pathPosition });
                
                DynamicBuffer<MinionPathPosition> midLaneBuffer = AddBuffer<MinionPathPosition>(midLane);
                foreach (float3 pathPosition in authoring.MidLanePath)
                    midLaneBuffer.Add(new() { Value = pathPosition });

                DynamicBuffer<MinionPathPosition> botLaneBuffer = AddBuffer<MinionPathPosition>(botLane);
                foreach (float3 pathPosition in authoring.BotLanePath)
                    botLaneBuffer.Add(new() { Value = pathPosition });

                AddComponent(entity, new MinionPathContainer
                {
                    TopLane = topLane,
                    MidLane = midLane,
                    BotLane = botLane,
                });
            }
        }
    }
}