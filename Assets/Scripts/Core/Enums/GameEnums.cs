namespace Core.Enums
{
    public enum TipoObjeto 
    { 
        Ninguno,
        
        // --- OBJETOS TASK (Recompensas de Misiones) ---
        ArcoCupido,
        TrampaOso,
        Pocion,
        FaroLuminiscente,
        
        // Antiguos / Existentes
        Antorcha,       
        DagaCazador,    
        TrampaSonora,   
        
        // --- OBJETOS DE TIENDA (Compras) ---
        PocionVelocidad,
        PocionVision,
        LupaHuella,
        CotaDeMalla,
        BombaApestosa,
        BotasSilenciosas,

        // --- OBJETOS LEGENDARIOS ---
        ManzanaOro,
        LoboAlbino,
        SombreroTonto,
        RelicarioNina,
        AntifazLadron,

        // --- PERGAMINOS DE MISIONES (Tienda) ---
        PergaminoCupido,
        PergaminoDaga,
        PergaminoManzana,
        PergaminoPocion,
        PergaminoAntifaz,
        PergaminoRelicario,
        PergaminoSombrero
    }

    public enum RolAldea
    {
        Ninguno,
        Herrero,
        Herbalista,
        Alquimista,
        Cazador,
        Vigia,
        Sereno
    }

    public enum MaterialType
    {
        Ninguno,
        AceroSierra,
        MaderaCorazon,
        AzufreTumba,
        HierbaSombria,
        LenteCristal,
        EsenciaAntigua,
        
        // --- Materiales de Misiones Básicas ---
        Leña,
        Afilado,
        Armadura,
        Antorcha,
        Frasco,
        Huerto,
        Lapida,
        Vela,
        Escoba,
        Reparacion,
        BaldeAgua,
        
        // Uso genérico reservado para interacciones donde no importa el material:
        Cualquiera 
    }

    public enum ZoneID
    {
        Desconocida,
        Cementerio,
        CasaBruja,
        Altar,
        Aserradero,
        Cueva,
        Torre,
        GranArbol,
        Aldea,
        Pozo,
        MesaBruja,
        Herrero
    }

    /// <summary>Modo de resultado de la interacción: otorga oro o items físicos.</summary>
    public enum InteractionMode { AccionPura, Recoleccion }

    /// <summary>Mecánica de input que usa el jugador para completar la interacción.</summary>
    public enum MecanicaInteraccion { MantenerPulsado, AporrearBoton, Transportar, PuntoEntrega, Timing, RitmoAfilar, Restregar }
}
