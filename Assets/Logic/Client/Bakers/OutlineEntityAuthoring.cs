using ROMA2.Logic.Client.UI;
using Unity.Entities;
using UnityEngine;

namespace Logic.Client
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