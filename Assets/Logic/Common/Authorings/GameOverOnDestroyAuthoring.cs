using Unity.Entities;
using UnityEngine;

namespace Logic.Common.Authorings
{
    public class GameOverOnDestroyAuthoring : MonoBehaviour
    {
        private class GameOverOnDestroyBaker : Baker<GameOverOnDestroyAuthoring>
        {
            public override void Bake(GameOverOnDestroyAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<GameOverOnDestroyTag>(entity);
            }
        }
    }
}