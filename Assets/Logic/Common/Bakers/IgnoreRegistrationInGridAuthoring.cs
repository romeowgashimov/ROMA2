using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class IgnoreRegistrationInGridAuthoring : MonoBehaviour
    {
        private class IgnoreAuthorizationInGridBaker : Baker<IgnoreRegistrationInGridAuthoring>
        {
            public override void Bake(IgnoreRegistrationInGridAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}