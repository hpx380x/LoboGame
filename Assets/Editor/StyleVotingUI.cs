using UnityEngine;
using UnityEditor;

namespace Core.EditorTools
{
    public class StyleVotingUI : EditorWindow
    {
        [MenuItem("Tools/Inspeccionar Votacion")]
        public static void Inspect()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Scene_Gameplay")
            {
                Debug.LogWarning("[StyleVotingUI] Abre 'Scene_Gameplay' primero.");
                return;
            }

            VotingUI voting = Object.FindAnyObjectByType<VotingUI>();
            if (voting == null)
            {
                Debug.LogError("[StyleVotingUI] No se encontró el componente 'VotingUI' en la escena.");
                return;
            }

            Debug.Log($"[StyleVotingUI] Encontrado VotingUI. Referencias actuales:\n" +
                      $"- panelVotacion: {(voting.panelVotacion != null ? voting.panelVotacion.name : "NULO")}\n" +
                      $"- botonJugadorMolde: {(voting.botonJugadorMolde != null ? voting.botonJugadorMolde.name : "NULO")}\n" +
                      $"- contenedorBotones: {(voting.contenedorBotones != null ? voting.contenedorBotones.name : "NULO")}");
        }
    }
}
