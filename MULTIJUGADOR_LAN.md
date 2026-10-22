# Cooperativo LAN (hasta 4 jugadores)

## Jugar

1. Copiar la carpeta completa `Builds/LAN` a cada ordenador Windows. Todos deben usar la misma compilación.
2. Conectar los equipos al mismo router o red local. Una red Wi-Fi de invitados puede impedir la comunicación entre equipos.
3. Abrir `Last67Seconds.exe` y pulsar **COOPERATIVO LAN** en el menú principal.
4. Elegir nombre y clase. Un jugador pulsa **CREAR PARTIDA** y comparte la IPv4 que muestra la sala.
5. Los otros tres introducen esa IPv4 y pulsan **UNIRSE**.
6. Cuando aparezcan todos en la sala, el anfitrión pulsa **INICIAR PARTIDA**.

Si Windows muestra el diálogo de firewall, permitir el juego en la red privada. El servidor utiliza TCP 26767. No se necesita abrir puertos en el router para jugar dentro de la misma red. No usar la dirección pública de internet ni `127.0.0.1` entre ordenadores; esta última solo sirve para probar varias instancias en una misma máquina.

## Reglas

- El anfitrión participa y controla la simulación: enemigos, daño, botín, compras y rondas.
- Cada jugador elige su clase y tiene vida, maná e inventario propios.
- Sin daño entre compañeros.
- El menú de pausa no detiene el mundo. Abrirlo no protege al jugador.
- Un jugador caído espera a la siguiente ronda; si cae todo el equipo, termina el intento.
- El anfitrión controla la continuación de rondas y los portales; los invitados pueden comprar al mercader. Solo un cliente compra a la vez, con stock compartido.
- La sala admite un anfitrión y tres invitados. Hay que reunirse antes de iniciar la partida.
- Salir como anfitrión termina la conexión de todos. No hay migración del servidor ni reconexión a una partida en curso.

## Validación

`Library/LanChecks.txt`: un servidor real y tres conexiones TCP en el editor; límite de cuatro, clases independientes, movimiento remoto, pausa, fuego amigo, muerte/reaparición y desconexión.

`Library/LanClientChecks.txt`: un servidor de prueba y el cliente Unity; representación de jugadores/enemigos/jefes/botín, estado de inventario, mercader, efectos, rondas y cambio de mapa.

La prueba en dos ordenadores físicos requiere la red y permisos de firewall de esos equipos. Las pruebas locales no verifican ese entorno.

## Implementación

Código aislado en `Assets/02_Scripts/Multiplayer`. Conexión TCP con mensajes JSON delimitados y límites de tamaño/cola; los hilos de red no acceden a Unity. El servidor aplica las acciones recibidas mediante las reglas de combate existentes. Los clientes reciben estados y muestran réplicas visuales sin ejecutar otra IA. Sin cuentas, Relay ni servidor externo.

Esta es una primera integración LAN: se debe probar la experiencia con el equipo antes de publicar una versión final del juego. El modo individual sigue disponible desde INICIAR JUEGO.
