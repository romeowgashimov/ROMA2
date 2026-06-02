using ROMA2.Logic.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
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