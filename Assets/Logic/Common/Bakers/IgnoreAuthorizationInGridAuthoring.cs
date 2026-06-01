using Logic.Common;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class IgnoreAuthorizationInGridAuthoring : MonoBehaviour
    {
        private class IgnoreAuthorizationInGridBaker : Baker<IgnoreAuthorizationInGridAuthoring>
        {
            public override void Bake(IgnoreAuthorizationInGridAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}