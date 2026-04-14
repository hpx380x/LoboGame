using System;

/// <summary>
/// Contenedor de datos para una tarea del jugador local.
/// Solo vive en el cliente (no es NetworkBehaviour).
/// </summary>
[Serializable]
public class TaskInfo
{
    public string taskId;
    public string nombreTarea;
    public EstadoTarea estado;

    public TaskInfo(string id, string nombre)
    {
        taskId      = id;
        nombreTarea = nombre;
        estado      = EstadoTarea.Disponible;
    }

    public bool EstaActiva      => estado == EstadoTarea.EnProgreso;
    public bool EstaDisponible  => estado == EstadoTarea.Disponible;
    public bool EstaTerminada   => estado == EstadoTarea.Completada || estado == EstadoTarea.Fallada;
}

public enum EstadoTarea
{
    Disponible,   // La tarea está en el pool pero no se ha empezado
    EnProgreso,   // El jugador está haciéndola RIGHT NOW (bloqueada)
    Completada,   // ¡Éxito!
    Fallada       // Se alejó mientras estaba en progreso
}
