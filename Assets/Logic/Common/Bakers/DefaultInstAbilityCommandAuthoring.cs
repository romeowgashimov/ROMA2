using ROMA2.Logic.Common.Abilities;
using ROMA2.Logic.Navigation;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class DefaultInstAbilityCommandAuthoring : MonoBehaviour
    {
        public GameObject AbilityPrefab;
        
        private class DefaultInstAbilityCommandBaker : Baker<DefaultInstAbilityCommandAuthoring>
        {
            public override void Bake(DefaultInstAbilityCommandAuthoring commandAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<AbilityCommand>(entity);
                AddComponent(entity, new DefaultInstAbilityCommand
                {
                    Prefab = GetEntity(commandAuthoring.AbilityPrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent<IgnoreRegistrationInGrid>(entity);
            }
        }
    }
}