<div align="center">
  <img src="https://upload.wikimedia.org/wikipedia/commons/thumb/1/19/Unity_Technologies_logo.svg/1024px-Unity_Technologies_logo.svg.png" width="200"/>
  <h1>🐺 Lobo Game 🐺</h1>
  <p><strong>Un juego multijugador asimétrico de engaño, supervivencia y traición.</strong></p>
  <p>Desarrollado en <b>Unity 6</b> + <b>Netcode for GameObjects (NGO)</b></p>

  ![Versión actual](https://img.shields.io/badge/Versión-v0.1_Alfa-orange.svg)
  ![Plataforma](https://img.shields.io/badge/Plataforma-PC_Windows-blue.svg)
  ![Motor](https://img.shields.io/badge/Motor-Unity_6-black.svg?logo=unity)
</div>

---

## 📖 De qué trata el juego

**Lobo Game** es un proyecto inspirado en el clásico juego social del "Hombre Lobo" o "Among Us", pero llevado a un entorno 3D en tercera persona con un diseño oscuro y atmosférico. 

Los jugadores se dividen en dos bandos en secreto al inicio de la partida:
1. 🧑‍🌾 **Los Aldeanos (Sobrevivientes):** Su objetivo es sobrevivir, deducir quién es el traidor y expulsarlo durante las asambleas de votación de día.
2. 🐺 **El Lobo (Traidor):** Su objetivo es cazar a todos los aldeanos en las sombras de la noche sin ser descubierto, usando el engaño y sus habilidades especiales.

El juego alterna entre ciclos de **Día** (seguros, pero donde se ejecuta a los sospechosos de la manada) y **Noche** (donde la visión de los aldeanos se reduce y el Lobo ataca).

---

## 🛠️ Tecnologías Utilizadas

Este proyecto utiliza una **Arquitectura de Red Estrictamente Autorizada por el Servidor** para prevenir trampas. Ningún cliente confía en la lógica local, lo que previene que los jugadores usen *hacks* para saber quién es el Lobo o modificar sus votos.

*   **Motor Gráfico:** Unity 6 (URP - Universal Render Pipeline).
*   **Networking (Multijugador):** Unity Netcode for GameObjects (NGO).
*   **Gestión de Servidores Cloud:** Unity Relay (Para partidas online gratuitas sin abrir puertos `P2P`).
*   **Cámaras:** Cinemachine Virtual Cameras (Tercera Persona).
*   **Animación Sinfónica:** Sincronización de Locomoción avanzada Cliente-Servidor.

---

## 🚀 Estado Actual del Desarrollo (v0.1 Alpha)

Hemos cimentado las bases más complejas del código multijugador que hacen posible el funcionamiento asimétrico, incluyendo:

- [x] **Arquitectura Multijugador Segura:** Separación estricta de variables en la red para que un hacker aldeano no pueda acceder al cliente y saber quién es el Lobo.
- [x] **Sistema de Lobbies en la Nube:** Interfaz conectada a *Unity Relay* para crear salas de hasta 10 jugadores usando un código secreto, sin necesidad de LAN.
- [x] **Ciclos Temporales (Día / Noche):** Reloj sincronizado centralizado desde el Host, de forma que todos los jugadores vean la noche caer exactamente en el mismo fotograma.
- [x] **Asambleas de Votación Democrática:** UI dinámica construida con diccionarios de datos que calcula las mayorías, previene los empates y aplica un veredicto de muerte (`[ServerRpc]`).
- [x] **Sistema Anti-Caídas / Anti-Stuck:** Gestión sólida del `NetworkManager`. Si el servidor se apaga en medio de un asesinato, los clientes volverán sanos y salvos al Lobby inicial.

### ⏱️ Próximas Implementaciones (Roadmap v0.2 / v0.3)
- 🔴 **Sensor Nocturno de Cazador:** Visión infrarroja / Rayos X temporal para que el Lobo pueda distinguir aldeanos brillantes a través de los bosques en fase nocturna de baja visibilidad.
- 🔴 **Audio Tridimensional Sincronizado:** Incorporación de pasos sigilosos, chillidos al asesinar que solo viajen a la red en una distancia corta alrededor del grito.
- 🔴 **Refinamiento de Entorno Base:** Sustituir la geometría gris actual de prueba por un mundo texturizado que aproveche Iluminación URP dinámica.

---

### 👨‍💻 Cómo Clonar y Jugar el Proyecto Localmente

Si deseas descargar el proyecto y trastear con el código maestro:

1. Asegúrate de tener instalado **Unity 2022.3 LTS o Unity 6** vía Unity Hub.
2. Abre tu terminal de comandos (o GitHub Desktop) y clona el código fuente:
   ```bash
   git clone https://github.com/hpx380x/LoboGame.git
   ```
3. Añade la carpeta clonada a tu lista de proyectos en **Unity Hub**.
4. ¡Inicia el Editor! Al abrir la carpeta `Assets/Scenes`, empieza **siempre** abriendo la escena `Scene_Menu` para probar la conexión Multiplayer real, no la vista deGameplay sin el Servidor maestro activo.
5. Usa un emulador de red en Unity o compila (`Build And Run`) el proyecto para tener 2 ventanas del cliente corriendo en tu PC, emulando la figura del Host y el Cliente. 

---
*Este proyecto está impulsado por curiosidad técnica, arquitectura limpia y la diversión que suponen los juegos sociales asimétricos.*
