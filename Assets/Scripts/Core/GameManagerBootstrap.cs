using UnityEngine;
using Unity.Netcode;

namespace SocialDeduction.Core
{
    /// <summary>
    /// Hace spawn del GameManager cuando el servidor arranca.
    /// Coloca este script en el mismo GameObject que el NetworkManager.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class GameManagerBootstrap : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManagerPrefab;

        private void Awake()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }

        private void OnServerStarted()
        {
            if (_gameManagerPrefab == null) return;

            var instance = Instantiate(_gameManagerPrefab);
            instance.GetComponent<NetworkObject>().Spawn();
        }
    }
}
