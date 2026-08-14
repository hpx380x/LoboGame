using Unity.Netcode;
using UnityEngine;
using Core.Enums;
using StarterAssets;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
            // Comprobación de ActionMap habilitado (evita usar ítems en menús, pergaminos, lobby...)
            if (TryGetComponent(out PlayerInput playerInput) && playerInput.currentActionMap != null && !playerInput.currentActionMap.enabled)
            {
                return;
            }

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
                
            case TipoObjeto.Pocion:
                if (IntentarUsarPocionMixta()) inventory.objetoEnMano.Value = TipoObjeto.Ninguno;
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
                UsarRelicarioNinaServer();
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
        Debug.Log($"[Server] ¡Bompa Apestosa estalla en {transform.position}!");
        MostrarParticulasBombaClientRpc();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject == null) continue;
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

    private bool IntentarUsarPocionMixta()
    {
        // 1. Intentar revivir a un cadáver cercano primero
        PlayerState cadaver = EncontrarJugadorCercano(3f, requerirVivo: false);
        if (cadaver != null && cadaver.isDead.Value)
        {
            cadaver.isDead.Value = false; // Revivir
            Debug.Log($"[Server] ¡Jugador {OwnerClientId} revivió a su compañero {cadaver.OwnerClientId} con la Poción!");
            return true;
        }

        // 2. Si no hay cadáveres, intentar envenenar a un jugador vivo cercano
        PlayerState victima = EncontrarJugadorCercano(3f, requerirVivo: true);
        if (victima != null)
        {
            PlayerStatusEffects se = victima.GetComponent<PlayerStatusEffects>();
            if (se != null) se.AplicarVenenoEnServidor();
            Debug.Log($"[Server] ¡Jugador {OwnerClientId} envenenó a {victima.OwnerClientId} con la Poción!");
            return true;
        }

        // 3. Si no hay nadie cerca, el usuario se la toma para ganar una segunda vida (autoresucitar si muere)
        if (statusEffects != null)
        {
            statusEffects.hasSegundaVida.Value = true;
            Debug.Log($"[Server] ¡Jugador {OwnerClientId} bebió la Poción! (AutoRevivir activado)");
            return true;
        }

        return false;
    }

    private PlayerState EncontrarJugadorCercano(float radio, bool requerirVivo)
    {
        PlayerState closest = null;
        float minDst = radio;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == OwnerClientId) continue; 
            if (client.Value.PlayerObject == null) continue;
            
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
        Vector3 pos = transform.position;
        GameObject barrera = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrera.transform.position = pos;
        barrera.transform.localScale = new Vector3(3f, 4f, 2f); 
        Collider col = barrera.GetComponent<Collider>();
        col.isTrigger = true; 
        
        barrera.AddComponent<WardContraLobo>();

        MeshRenderer mr = barrera.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Destroy(barrera, 600f); 
        NotificarManzanaPlantadaClientRpc(pos);
    }
    
    [ClientRpc]
    private void NotificarManzanaPlantadaClientRpc(Vector3 pos)
    {
        Debug.Log($"🍎 [Magia] Barrera invisible en: {pos}");
        
        GameObject barreraLocal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barreraLocal.name = "BarreraLocal";
        barreraLocal.transform.position = pos;
        barreraLocal.transform.localScale = new Vector3(3f, 4f, 2f);
        
        MeshRenderer mr = barreraLocal.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        
        Collider col = barreraLocal.GetComponent<Collider>();
        if (col != null)
        {
            bool soyLobo = false;
            if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                PlayerState localPs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerState>();
                if (localPs != null && localPs.isWolf.Value)
                {
                    soyLobo = true;
                }
            }
            col.isTrigger = !soyLobo;
        }
        
        Destroy(barreraLocal, 600f);
    }

    private void PlantarTrampaOso()
    {
        if (!IsServer) return;
        Vector3 pos = transform.position + Vector3.up * 0.1f;
        GameObject trampa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trampa.transform.position = pos;
        trampa.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        Collider col = trampa.GetComponent<Collider>();
        col.isTrigger = true; 
        MeshRenderer mr = trampa.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        
        trampa.AddComponent<TrampaOsoScript>();
        
        Debug.Log($"[Server] Jugador {OwnerClientId} colocó una Trampa.");
        NotificarTrampaPlantadaClientRpc(pos);
    }

    [ClientRpc]
    private void NotificarTrampaPlantadaClientRpc(Vector3 pos)
    {
        GameObject trampaLocal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trampaLocal.name = "TrampaLocal";
        trampaLocal.transform.position = pos;
        trampaLocal.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        
        Collider col = trampaLocal.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        MeshRenderer mr = trampaLocal.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = true;
        
        Material mat = mr != null ? mr.material : null;
        if (mat != null) mat.color = Color.gray;
    }

    [ClientRpc]
    public void NotificarDestruccionTrampaClientRpc(Vector3 pos)
    {
        Collider[] cols = Physics.OverlapSphere(pos, 0.5f);
        foreach (var col in cols)
        {
            if (col.gameObject.name.Contains("TrampaLocal"))
            {
                Destroy(col.gameObject);
            }
        }
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

    private void UsarRelicarioNinaServer()
    {
        if (!IsServer) return;
        
        bool loboCerca = false;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key == OwnerClientId) continue;
            if (client.Value.PlayerObject == null) continue;
            
            PlayerState ps = client.Value.PlayerObject.GetComponent<PlayerState>();
            if (ps != null && ps.isWolf.Value && !ps.isDead.Value)
            {
                if (Vector3.Distance(transform.position, ps.transform.position) <= 10f)
                {
                    loboCerca = true;
                    break;
                }
            }
        }

        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        UsarRelicarioNinaClientRpc(loboCerca, rpcParams);
    }

    [ClientRpc]
    private void UsarRelicarioNinaClientRpc(bool loboCerca, ClientRpcParams rpcParams = default)
    {
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
