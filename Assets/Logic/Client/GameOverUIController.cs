using Logic.Common;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Entities.World;

namespace Logic.Client
{
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _gameOverText;
        [SerializeField] private Button _returnToMainButton;
        [SerializeField] private Button _rageQuitButton;

        private EntityQuery _networkConnectionQuery;

        //Remove Listeners after?
        private void OnEnable()
        {
            _returnToMainButton.onClick.AddListener(ReturnToMain);
            _rageQuitButton.onClick.AddListener(RageQuit);
            if (DefaultGameObjectInjectionWorld == null) return;
            _networkConnectionQuery = DefaultGameObjectInjectionWorld
                .EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
            GameOverSystem gameOverSystem = DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GameOverSystem>();
            gameOverSystem.OnGameOver += ShowGameOverUI;
        }

        private void ReturnToMain()
        {
            if (_networkConnectionQuery.TryGetSingletonEntity<NetworkStreamConnection>(out Entity networkConnection))
            {
                DefaultGameObjectInjectionWorld
                    .EntityManager.AddComponent<NetworkStreamRequestDisconnect>(networkConnection);
                
                DisposeAllWorlds();
                SceneManager.LoadScene(0);
            }
        }
        
        //Very important feature
        private void RageQuit()
        {
            Application.Quit();
        }

        private void OnDisable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;
            GameOverSystem gameOverSystem = DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GameOverSystem>();
            gameOverSystem.OnGameOver -= ShowGameOverUI;
        }

        private void ShowGameOverUI(TeamType winningTeam)
        {
            _gameOverPanel.SetActive(true);
            _gameOverText.text = $"{winningTeam.ToString()} Team Wins!";
        }
    }
}