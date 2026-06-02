using System;
using ROMA2.Logic.Data;
using Unity.Entities;

namespace ROMA2.Logic.Common.GameProcesses
{
    public partial class GameOverSystem : SystemBase
    {
        public Action<TeamType> OnGameOver;

        protected override void OnCreate()
        {
            RequireForUpdate<GameOverTag>();
            RequireForUpdate<GameplayingTag>();
        }

        protected override void OnUpdate()
        {
            Entity gameOverEntity = SystemAPI.GetSingletonEntity<GameOverTag>();
            TeamType winningTeam = SystemAPI.GetComponent<WinningTeam>(gameOverEntity).Value;
            OnGameOver?.Invoke(winningTeam);
            
            Entity gamePlayingEntity = SystemAPI.GetSingletonEntity<GameplayingTag>();
            EntityManager.DestroyEntity(gamePlayingEntity);

            Enabled = false;
        }
    }
}