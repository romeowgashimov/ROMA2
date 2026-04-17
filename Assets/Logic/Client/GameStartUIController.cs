using System.Collections;
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
    public class GameStartUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _beginGamePanel;
        [SerializeField] private GameObject _confirmQuitPanel;
        [SerializeField] private GameObject _countdownPanel;
        [SerializeField] private GameObject _choiceChampionPanel;
        
        [SerializeField] private GameObject _championsList;
        [SerializeField] private Button _confirmChoiceButton;
        [SerializeField] private Button _quitWaitingButton;
        [SerializeField] private Button _confirmQuitButton;
        [SerializeField] private Button _cancelQuitButton;
        
        [SerializeField] private TextMeshProUGUI _waitingText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private TextMeshProUGUI _countdownNumber;
        
        [SerializeField] private ChampionDatabase _championDatabase;
        
        private EntityQuery _networkConnectionQuery;
        private EntityManager _entityManager;

        private void OnEnable()
        {
            _beginGamePanel.SetActive(true);

            _quitWaitingButton.onClick.AddListener(AttemptQuitWaiting);
            _confirmQuitButton.onClick.AddListener(ConfirmQuit);
            _cancelQuitButton.onClick.AddListener(CancelQuit);

            if (DefaultGameObjectInjectionWorld == null) return;
            _entityManager = DefaultGameObjectInjectionWorld.EntityManager;
            _networkConnectionQuery = _entityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
            
            ClientStartGameSystem startGameSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<ClientStartGameSystem>();
            if (startGameSystem != null)
            {
                startGameSystem.OnUpdatePlayersRemainingToStart += UpdatePlayerRemainingText;
                //startGameSystem.OnStartGameCountdown += BeginCountdown;
            }
            
            ChoiceChampionSystem choiceChampionSystem =
                DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ChoiceChampionSystem>();
            if (choiceChampionSystem == null) return;
            choiceChampionSystem.OnReadyToChoice += BeginChoice;
            
            ClientRequestGameEntrySystem entryRequestSystem =
                DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ClientRequestGameEntrySystem>();
            if (entryRequestSystem == null) return;
            ChampionChoiceIcon[] icons = _championsList.GetComponentsInChildren<ChampionChoiceIcon>();
            for (int i = 0; i < icons.Length; ++i)
            {
                int currentId = i;
                icons[i].OnChosen += Choice;
                
                void Choice() => entryRequestSystem.ChoiceChampion((uint)_championDatabase.Configs[currentId].Id);
            }
            _confirmChoiceButton.onClick.AddListener(entryRequestSystem.ConfirmChampion);
            entryRequestSystem.OnChosen += BeginCountdown;

            CountdownToGameStartSystem countdownSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<CountdownToGameStartSystem>();
            if (countdownSystem != null)
            {
                countdownSystem.OnUpdateCountdownText += UpdateCountdownText;
                void UpdateCountdownText(int _)
                {
                    countdownSystem.OnUpdateCountdownText -= UpdateCountdownText;
                    _countdownText.text = "Игра начнётся через ";
                }
                countdownSystem.OnUpdateCountdownText += UpdateCountdownNumber;
                countdownSystem.OnCountdownEnd += EndCountdown;
            }
        }

        private void BeginCountdown()
        {
            _beginGamePanel.SetActive(false);
            _confirmQuitPanel.SetActive(false);
            _choiceChampionPanel.SetActive(false);
            _countdownPanel.SetActive(true);
        }

        private void OnDisable()
        {
            _quitWaitingButton.onClick.RemoveAllListeners();
            _confirmQuitButton.onClick.RemoveAllListeners();
            _cancelQuitButton.onClick.RemoveAllListeners();
            _confirmChoiceButton.onClick.RemoveAllListeners();

            if (DefaultGameObjectInjectionWorld == null) return;

            ClientStartGameSystem startGameSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<ClientStartGameSystem>();
            if (startGameSystem == null) return;
            startGameSystem.OnUpdatePlayersRemainingToStart -= UpdatePlayerRemainingText;
            //startGameSystem.OnStartGameCountdown -= BeginCountdown;

            CountdownToGameStartSystem countdownSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<CountdownToGameStartSystem>();
            if (countdownSystem == null) return;
            countdownSystem.OnUpdateCountdownText -= UpdateCountdownNumber;
            countdownSystem.OnCountdownEnd -= EndCountdown;

            _choiceChampionPanel.SetActive(false);
            ClientRequestGameEntrySystem entryRequestSystem =
                DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ClientRequestGameEntrySystem>();
            if(entryRequestSystem == null) return;
            ChampionChoiceIcon[] icons = _championsList.GetComponentsInChildren<ChampionChoiceIcon>();
            for (int i = 0; i < icons.Length; i++)
                icons[i].RemoveAllListeners();
            entryRequestSystem.OnChosen -= BeginCountdown;

            ChoiceChampionSystem choiceChampionSystem =
                DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ChoiceChampionSystem>();
            if (choiceChampionSystem == null) return;
            choiceChampionSystem.OnReadyToChoice -= BeginChoice;
        }

        private void UpdatePlayerRemainingText(int remainingPlayers)
        {
            string playersText = remainingPlayers == 1  ? "player" : "players";
            _waitingText.text = $"Waiting for {remainingPlayers.ToString()} more {playersText} to join";
        }

        private void UpdateCountdownNumber(int countdownTime)
        {
            _countdownNumber.text = countdownTime.ToString();
        }

        private void AttemptQuitWaiting()
        {
            _beginGamePanel.SetActive(false);
            _confirmQuitPanel.SetActive(true);
        }

        private void ConfirmQuit()
        {
            StartCoroutine(DisconnectDelay());
        }

        IEnumerator DisconnectDelay()
        {
            yield return new WaitForSeconds(1f);
            if (_networkConnectionQuery.TryGetSingletonEntity<NetworkStreamConnection>(out Entity networkConnectionEntity))
                DefaultGameObjectInjectionWorld.EntityManager
                    .AddComponent<NetworkStreamRequestDisconnect>(networkConnectionEntity);
            
            DisposeAllWorlds();
            SceneManager.LoadScene(0);
        }

        private void CancelQuit()
        {
            _confirmQuitPanel.SetActive(false);
            _beginGamePanel.SetActive(true);
        }

        private void BeginChoice()
        {
            _beginGamePanel.SetActive(false);
            _confirmQuitPanel.SetActive(false);
            _countdownPanel.SetActive(false);
            _choiceChampionPanel.SetActive(true);
        }

        private void EndCountdown()
        {
            _countdownPanel.SetActive(false);
        }
    }
}