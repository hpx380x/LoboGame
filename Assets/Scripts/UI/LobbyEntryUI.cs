using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System;

public class LobbyEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    [SerializeField] private Button joinButton;

    private Lobby _lobby;
    private Action<Lobby> _onJoinClicked;

    /// <summary>
    /// Configura los datos visuales de este botón en la lista.
    /// </summary>
    public void Inicializar(Lobby lobby, Action<Lobby> onJoinClicked)
    {
        _lobby = lobby;
        _onJoinClicked = onJoinClicked;

        lobbyNameText.text = lobby.Name;
        playersCountText.text = $"{lobby.Players.Count} / {lobby.MaxPlayers}";

        // Evitar que se unan a un lobby lleno
        if (lobby.Players.Count >= lobby.MaxPlayers)
        {
            joinButton.interactable = false;
        }
        else
        {
            joinButton.interactable = true;
        }

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            _onJoinClicked?.Invoke(_lobby);
        });
    }
}
