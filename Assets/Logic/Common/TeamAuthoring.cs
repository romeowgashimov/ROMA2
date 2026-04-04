using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class TeamAuthoring : MonoBehaviour
    {
        public TeamType Team;
        
        public class TeamBaker : Baker<TeamAuthoring>
        {
            public override void Bake(TeamAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Team>(entity, new() { Value = authoring.Team });
            }
        }
    }
}