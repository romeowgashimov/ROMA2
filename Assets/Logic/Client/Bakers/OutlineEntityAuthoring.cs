using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class OutlineEntityAuthoring : MonoBehaviour
    {
        private class OutlineEntityBaker : Baker<OutlineEntityAuthoring>
        {
            public override void Bake(OutlineEntityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<OutlineWidth>(entity);
                AddComponent<OutlineColor>(entity);
            }

        }
    }
}