using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class AoeAbilityAuthoring : MonoBehaviour
    {
        public GameObject AbilityPrefab;
        
        public class AoeAbilityBaker : Baker<AoeAbilityAuthoring>
        {
            public override void Bake(AoeAbilityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AbilityPrefabs
                {
                    AoeAbility = GetEntity(authoring.AbilityPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}