# HOJA DE RUTA: Minijuegos de Misiones (Fase Beta)

El sistema actual de misiones (Alfa) sirve como base para sincronizar los estados en red (Netcode) y el inventario. La **Fase Beta** transformará estas interacciones en minijuegos mecánicos activos para aumentar la tensión y la inmersión de los jugadores (especialmente para que los lobos tengan oportunidades de cazar mientras los aldeanos están distraídos).

## 1. Misión de Talar (Mina / Bosque)
- **Mecánica**: *Quick Time Event (QTE) Rítmico*.
- **Concepto**: El jugador debe pulsar la tecla `[E]` (o clic) en el momento exacto en que el hacha/pico golpea la madera/piedra durante la animación.
- **Fallo**: Si falla el ritmo, la animación se interrumpe, el personaje se cansa (pequeño aturdimiento) y hace ruido adicional.
- **Éxito**: Al encadenar *X* golpes perfectos, se completa la recolección.

## 2. Misión de Entregar Agua (Los Dos Baldes)
- **Mecánica**: *Equilibrio Dinámico (Walking Simulator)*.
- **Concepto**: El jugador equipa un balde de agua en cada mano. Al avanzar con `[W]`, el peso del agua genera un balanceo físico en el personaje que lo empuja hacia los lados.
- **Controles**: El jugador debe usar `[A]` y `[D]` para contrarrestar el peso y mantener el equilibrio mientras camina hacia el destino (ej. Gran Árbol).
- **Fallo**: Si el medidor de balanceo llega al límite, el personaje tropieza, derrama el agua y pierde la herramienta. Deberá volver al pozo a empezar de cero.
- **Éxito**: Llegar al punto de entrega sin que se caiga el agua.

## 3. Misión de las Velas (Iglesia / Altar)
- **Mecánica**: *Memoria Secuencial (Estilo Simon Dice)*.
- **Concepto**: Al interactuar con el altar, una serie de velas se encienden en un orden y patrón de ritmo específico. 
- **Controles**: El jugador debe memorizar la secuencia y encender las velas exactamente en el mismo orden (y frecuencia) para revelar el secreto o completar el paso.
- **Fallo**: Las velas se apagan de golpe con un soplido fantasmal. Vuelta a empezar.

## 4. Misión del Huerto (Regar las Plantas)
- **Mecánica**: *Cobertura Espacial Continua*.
- **Concepto**: El jugador equipa la regadera y debe moverse físicamente cubriendo toda el área de tierra seca.
- **Controles**: Mantener pulsado el botón de acción para verter agua mientras se camina por los surcos. La tierra cambiará de color a "húmeda".
- **Fallo**: Quedarse sin agua antes de cubrir el % requerido (requiere recargar) o salirse de la zona.
- **Éxito**: Cuando el área regada alcanza el umbral, la animación de las plantas floreciendo se activa en red.

---
## 5. Sincronización Visual (Prueba de Inocencia)
- **Concepto Crítico**: En un juego de deducción social, *ver* a alguien hacer una tarea demuestra que está ocupado.
- **Implementación en Red**: Aunque el minijuego se calcula en local para evitar lag, el cliente debe enviar actualizaciones constantes al servidor (mediante `NetworkVariable` o `Unreliable RPCs`) sobre su estado de animación.
- **Ejemplo Práctico**: Si el jugador se tambalea al llevar el agua, los demás jugadores deben ver su avatar tambaleándose. 
- **El Rol del Lobo**: Dado que el Lobo está en su forma humana ("estado aldeano") durante el día, **sí puede y debe hacer las misiones reales** junto al resto de jugadores. Esto elimina la necesidad de tener un botón de "Fingir Tarea"; el Lobo juega los mismos minijuegos para ganar coartadas y no levantar sospechas antes de transformarse por la noche.

---
*Nota de Arquitectura*: Todos estos minijuegos ocurrirán a nivel **Local (Cliente)** para asegurar una respuesta inmediata sin lag (Prediction). El servidor solo recibirá el aviso de "Minijuego Superado" para otorgar el oro y sincronizar los efectos visuales finales (flores naciendo, velas encendidas, etc.). La sincronización de posturas y herramientas se manejará en una capa separada puramente estética.
