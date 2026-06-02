using TMPro;
using UnityEngine;
using static Unity.Entities.World;
using RespawnChampionSystem = ROMA2.Logic.Common.GameProcesses.RespawnChampionSystem;

namespace ROMA2.Logic.Client.Controllers
{
    public class RespawnUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _respawnPanel;
        [SerializeField] private TextMeshProUGUI _respawnCountdownText;

        private void OnEnable()
        {
            _respawnPanel.SetActive(false);

            if (DefaultGameObjectInjectionWorld == null) return;
            RespawnChampionSystem respawnSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<RespawnChampionSystem>();
            if (respawnSystem != null)
            {
                respawnSystem.OnUpdateRespawnCountdown += UpdateRespawnCountdownText;
                respawnSystem.OnRespawn += CloseRespawnPanel;
            }
        }

        private void OnDisable()
        {
            if (DefaultGameObjectInjectionWorld == null) return;
            RespawnChampionSystem respawnSystem = DefaultGameObjectInjectionWorld
                .GetExistingSystemManaged<RespawnChampionSystem>();
            if (respawnSystem != null)
            {
                respawnSystem.OnUpdateRespawnCountdown -= UpdateRespawnCountdownText;
                respawnSystem.OnRespawn -= CloseRespawnPanel;
            }
        }

        private void UpdateRespawnCountdownText(int countdownText)
        {
            if (!_respawnPanel.activeSelf) _respawnPanel.SetActive(true);
            _respawnCountdownText.text = countdownText.ToString(); 
        }

        private void CloseRespawnPanel()
        {
            _respawnPanel.SetActive(false);
        }
    }
}