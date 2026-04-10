using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class GameOverEntityAuthoring : MonoBehaviour
    {
        public class GameOverEntityBaker : Baker<GameOverEntityAuthoring>
        {
            public override void Bake(GameOverEntityAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<GameOverTag>(entity);
                AddComponent<WinningTeam>(entity);
            }
        }
    }
}