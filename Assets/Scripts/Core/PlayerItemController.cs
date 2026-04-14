using Unity.Netcode;
using UnityEngine;
using Core.Enums;
using StarterAssets;
using System.Collections.Generic;

public class PlayerItemController : NetworkBehaviour
{
    private PlayerState playerState;
    private PlayerInventory inventory;
    private PlayerStatusEffects statusEffects;
    private ThirdPersonController thirdPersonController;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        inventory = GetComponent<PlayerInventory>();
        statusEffects = GetComponent<PlayerStatusEffects>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Input para usar el objeto consumible (Tecla F)
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (inventory != null && inventory.objetoEnMano.Value != TipoObjeto.Ninguno)
            {
                UsarObjetoEnManoServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void UsarObjetoEnManoServerRpc()
    {
        if (playerState.isDead.Value || inventory.objetoEnMano.Value == TipoObjeto.Ninguno) return;

        TipoObjeto objetoAUsar = inventory.objetoEnMano.Value;
        Debug.Log($"[Server] Jugador {OwnerClientId} usó el objeto interactivo: {objetoAUsar}");

        switch (objetoAUsar)
        {
            case TipoObjeto.PocionVelocidad:
                ActivarPocionVelocidad();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno; 
                break;
                
            case TipoObjeto.CotaDeMalla:
                if (statusEffects != null) statusEffects.tieneCotaMalla.Value = true;
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno; 
                Debug.Log($"[Server] Jugador {OwnerClientId} se ha equipado la Cota de Malla.");
                break;
                
            case TipoObjeto.BombaApestosa:
                ExplotarBombaApestosa();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.BotasSilenciosas:
                if (statusEffects != null) statusEffects.isSilencioso.Value = true;
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                Debug.Log($"[Server] Jugador {OwnerClientId} se ha equipado Botas Silenciosas.");
                break;
                
            case TipoObjeto.PocionMuerte:
                if (IntentarEnvenenarCercano()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.PocionVida:
                if (IntentarRevivirOConsumir()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.FaroLuminiscente:
                EncenderFaroClientRpc();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;
                
            case TipoObjeto.PocionVision:
                ActivarPocionVisionClientRpc();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.ArcoCupido:
                if (DispararArcoCupido()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.LupaHuella:
                ActivarLupaHuellasClientRpc(); 
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.ManzanaOro:
                PlantarManzanaOro();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.TrampaOso:
                PlantarTrampaOso();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.LoboAlbino:
                if (UsarLoboAlbino()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.SombreroTonto:
                if (LanzarSombreroTonto()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.RelicarioNina:
                UsarRelicarioNinaClientRpc();
                inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            case TipoObjeto.AntifazLadron:
                if (UsarAntifazLadron()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
                break;

            default:
                Debug.LogWarning($"[Server] El objeto {objetoAUsar} aún no tiene programado su efecto de [USO].");
                break;
        }
    }

    // --- EFECTOS ESPECÍFICOS RED ---

    private void ExplotarBombaApestosa()
    {
        Debug.Log($"<color=green>[Server] ¡Bompa Apestosa estalla en {transform.position}!</color>");
        MostrarParticulasBombaClientRpc();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            PlayerStatusEffects se = client.Value.PlayerObject.GetComponent<PlayerStatusEffects>();
            if (ps != null && !ps.isDead.Value && se != null)
            {
                if (Vector3.Distance(transform.position, ps.transform.position) <= 5.5f)
                {
                    se.AplicarStunEnServidor(5f); 
                }
            }
        }
    }

    [ClientRpc]
    private void MostrarParticulasBombaClientRpc()
    {
       Debug.Log("<color=green>💨 [Visual] *NUBARRÓN DE GAS APESTOSO INVADE EL ÁREA* 💨</color>");
    }

    private bool IntentarEnvenenarCercano()
    {
        PlayerState victima = EncontrarJugadorCercano(3f, requerirVivo: true);
        if (victima != null)
        {
            PlayerStatusEffects se = victima.GetComponent<PlayerStatusEffects>();
            if (se != null) se.AplicarVenenoEnServidor();
            Debug.Log($"[Server] ¡Jugador {OwnerClientId} envenenó a {victima.OwnerClientId}!");
            return true;
        }
        return false;
    }

    private bool IntentarRevivirOConsumir()
    {
        PlayerState cadaver = EncontrarJugadorCercano(3f, requerirVivo: false);
        if (cadaver != null && cadaver.isDead.Value)
        {
            cadaver.isDead.Value = false; // Revivir
            Debug.Log($"[Server] ¡Jugador {OwnerClientId} revivió a su compañero {cadaver.OwnerClientId}!");
            return true;
        }
        else 
        {
            if (statusEffects != null) statusEffects.hasSegundaVida.Value = true;
            Debug.Log($"[Server] ¡Jugador {OwnerClientId} bebió la Poción de Vida! (AutoRevivir)");
            return true;
        }
    }

    private PlayerState EncontrarJugadorCercano(float radio, bool requerirVivo)
    {
        PlayerState closest = null;
        float minDst = radio;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == OwnerClientId) continue; 
            
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            if (ps == null) continue;
            
            if (requerirVivo && ps.isDead.Value) continue;
            if (!requerirVivo && !ps.isDead.Value) continue;

            float dst = Vector3.Distance(transform.position, ps.transform.position);
            if (dst <= minDst)
            {
                minDst = dst;
                closest = ps;
            }
        }
        return closest;
    }

    private bool DispararArcoCupido()
    {
        if (!IsServer) return false;
        
        List<PlayerState> cercanos = new List<PlayerState>();
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == OwnerClientId) continue; 
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && !ps.isDead.Value) cercanos.Add(ps);
        }

        cercanos.Sort((a, b) => 
            Vector3.Distance(transform.position, a.transform.position).CompareTo(
            Vector3.Distance(transform.position, b.transform.position))
        );

        if (cercanos.Count >= 2)
        {
            cercanos[0].amanteId.Value = cercanos[1].OwnerClientId;
            cercanos[1].amanteId.Value = cercanos[0].OwnerClientId;
            return true;
        }
        return false;
    }

    private void PlantarManzanaOro()
    {
        if (!IsServer) return;
        GameObject barrera = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrera.transform.position = transform.position;
        barrera.transform.localScale = new Vector3(3f, 4f, 2f); 
        Collider col = barrera.GetComponent<Collider>();
        col.isTrigger = true; 
        
        MeshRenderer mr = barrera.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Destroy(barrera, 600f); 
        NotificarManzanaPlantadaClientRpc(transform.position);
    }
    
    [ClientRpc]
    private void NotificarManzanaPlantadaClientRpc(Vector3 pos) { Debug.Log($"<color=yellow>🍎 [Magia] Barrera invisible en: {pos}</color>"); }

    private void PlantarTrampaOso()
    {
        if (!IsServer) return;
        GameObject trampa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trampa.transform.position = transform.position + Vector3.up * 0.1f;
        trampa.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        Collider col = trampa.GetComponent<Collider>();
        col.isTrigger = true; 
        MeshRenderer mr = trampa.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        Debug.Log($"[Server] Jugador {OwnerClientId} colocó una Trampa.");
    }

    private bool UsarLoboAlbino()
    {
        if (!IsServer || !playerState.isWolf.Value) return false;
        if (statusEffects != null) statusEffects.hasLoboAlbinoPower.Value = true;
        return true;
    }

    private bool LanzarSombreroTonto()
    {
        if (!IsServer) return false;
        PlayerState victima = EncontrarJugadorCercano(4f, requerirVivo: true);
        if (victima != null)
        {
            PlayerStatusEffects se = victima.GetComponent<PlayerStatusEffects>();
            if (se != null) se.isTonto.Value = true;
            return true;
        }
        return false;
    }

    private bool UsarAntifazLadron()
    {
        if (!IsServer) return false;
        PlayerState victima = EncontrarJugadorCercano(3f, requerirVivo: true);
        if (victima != null)
        {
            bool miRolEraLobo = playerState.isWolf.Value;
            bool suRolEraLobo = victima.isWolf.Value;
            playerState.isWolf.Value = suRolEraLobo;
            victima.isWolf.Value = miRolEraLobo;
            
            playerState.CambioRolPrivadoClientRpc(playerState.isWolf.Value, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } } });
            victima.CambioRolPrivadoClientRpc(victima.isWolf.Value, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { victima.OwnerClientId } } });
            return true;
        }
        return false;
    }

    [ClientRpc]
    private void UsarRelicarioNinaClientRpc()
    {
        if (!IsOwner) return;
        bool loboCerca = false;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == OwnerClientId) continue;
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && ps.isWolf.Value && !ps.isDead.Value)
            {
                if (Vector3.Distance(transform.position, ps.transform.position) <= 10f) loboCerca = true;
            }
        }

        if (!loboCerca) Debug.Log("<color=green>👧 [Secreto] El relicario brilla verde tranquilo. Nadie a 10 metros es lobo.</color>");
        else Debug.Log($"<color=red>👧 [Secreto] Notas un aura oscura... ¡Lobo cerca!</color>");
    }

    private void ActivarPocionVelocidad()
    {
        AplicarEfectoVelocidadClientRpc(true);
        Invoke(nameof(DesactivarPocionVelocidad), 10f);
    }
    
    private void DesactivarPocionVelocidad() { AplicarEfectoVelocidadClientRpc(false); }

    [ClientRpc]
    private void AplicarEfectoVelocidadClientRpc(bool activar)
    {
        if (thirdPersonController != null)
        {
            thirdPersonController.MoveSpeed = activar ? 6.0f : 2.0f;
            thirdPersonController.SprintSpeed = activar ? 10.0f : 5.33f;
        }
    }

    [ClientRpc]
    private void EncenderFaroClientRpc()
    {
        if (!IsOwner) return; 
        GameObject luz = new GameObject("LuzMagicaFaro");
        luz.transform.SetParent(this.transform);
        luz.transform.localPosition = Vector3.up * 2f;
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 40f; 
        l.intensity = 2f;
        l.color = Color.cyan;
    }

    [ClientRpc]
    private void ActivarPocionVisionClientRpc()
    {
        if (!IsOwner) return;
        StartCoroutine(RutinaPocionVision());
    }

    private System.Collections.IEnumerator RutinaPocionVision()
    {
        bool nieblaOriginal = RenderSettings.fog;
        RenderSettings.fog = false;
        GameObject luz = new GameObject("OjoQueTodoLoVe");
        luz.transform.position = transform.position + Vector3.up * 50f;
        luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 2.5f;
        l.color = new Color(1f, 0.8f, 1f);

        yield return new WaitForSeconds(5f);

        RenderSettings.fog = nieblaOriginal;
        Destroy(luz);
    }

    // --- MÓDULO HUELLAS ---
    [ClientRpc]
    private void ActivarLupaHuellasClientRpc()
    {
        if (!IsOwner) return;
        StartCoroutine(RutinaLupa());
    }

    private System.Collections.IEnumerator RutinaLupa()
    {
        PlayerState.todasLasHuellas.RemoveAll(item => item == null); 
        foreach(var h in PlayerState.todasLasHuellas) if (h != null) h.GetComponent<MeshRenderer>().enabled = true;
        yield return new WaitForSeconds(5f); 
        foreach(var h in PlayerState.todasLasHuellas) if (h != null) h.GetComponent<MeshRenderer>().enabled = false;
    }
}
