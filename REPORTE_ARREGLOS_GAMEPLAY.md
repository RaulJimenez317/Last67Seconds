# Arreglos de gameplay — Anthony_Branches

## Primer jefe

`BossCharacter.State` separa Idle, Moving, Windup, Recovery, Stunned y Dead. El ataque fija dirección y objetivo durante su preparación, se ejecuta una sola vez y pasa a recuperación con el agente detenido. Aturdir cancela el ataque pendiente; al terminar el aturdimiento hay recuperación antes de volver a atacar. Pausar conserva el estado. Inicializar limpia temporizadores previos. La muerte cancela el ataque, desactiva colliders y limpia invocaciones y peligros.

Se conserva la alternancia roca/lodo del Rey Goblin, sus invocaciones por tiempo y umbrales de salud, y la progresión existente del Demonio. Los estados de combate son distintos de las fases por salud del Demonio.

## Pasivas con T / Y / U

Se usan las cinco pasivas existentes, no un inventario nuevo. T activa la ranura 1, Y la 2 y U la 3. Duran 10 segundos y pueden reactivarse a los 20 segundos desde su activación. No se consumen. `CharacterState.HasPassive` ahora indica que el efecto está **activo**, no simplemente equipado.

`LootPickup.OnTriggerEnter` recoge pasivas y munición. Los pickups creados llevan Rigidbody cinemático sin gravedad y SphereCollider trigger. Los duplicados o inventarios llenos no consumen el pickup: para reemplazar una ranura se conserva Shift/Ctrl/Alt + E. Los temporizadores se guardan con el estado de la partida, se congelan en pausa/tienda y se reinician al comenzar una partida. La UI indica tecla, disponibilidad y tiempo restante.

## Fusca / mosquete

Mantiene el raycast desde la cámara y la cadencia original de 0,5 disparos/s (Frenesí activo la multiplica por 1,25). Se verificó daño único por objetivo y bloqueo por paredes. Cargador de 6, reserva inicial de 18 y máximo de reserva 60. R recarga en 2 segundos; disparar vacío inicia la recarga si hay reserva. No dispara durante la recarga. Cambiar arma, morir o desactivar el componente la cancela; pausar congela el progreso. Los suministros del mosquete rellenan la reserva. Arco y báculo conservan sus reglas anteriores.

## Menús

Se conserva Canvas uGUI con GraphicRaycaster y EventSystem/InputSystemUIInputModule. La pausa incluye regreso al menú principal. Al cerrar un panel se limpia la selección del EventSystem; reanudar no permite que ese mismo clic dispare. `RunNavigation` evita cargas repetidas y `SceneTransition` realiza un fundido de 0,2 segundos por lado con tiempo no escalado, bloqueando clics durante la carga. Regresar al menú limpia el director persistente y restaura el tiempo.

## Validación

- `GameplayRepairChecks.Run()` en Play: 26 comprobaciones aprobadas de triggers reales, buffs, cadencia, daño con raycast, paredes, reserva, recarga, pausa, transición al menú y estados/daño/muerte del primer jefe.
- `MechanicsChecks.Data()`: 28 comprobaciones aprobadas.
- `UIChecks.Begin()` en Calabozo nuevo: 50 comprobaciones aprobadas de UI y recorrido Calabozo/Coliseo.
- HUD y pausa revisados mediante capturas del Game View.

Las pruebas anteriores de pasivas se actualizaron para activarlas explícitamente. No se probó un ejecutable independiente. Los cambios de este trabajo quedan locales en Anthony_Branches hasta que se solicite subirlos.
