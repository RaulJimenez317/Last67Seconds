# Continuación de animaciones, equipamiento, VFX y menús

Fecha: 17 de octubre de 2026. Rama: `Leandro_Branches`. Proyecto existente, Unity 6000.5.6f1.

## Estado y cambios

Se retomó la instalación interrumpida: el controlador Goblin estaba incompleto y la biblioteca no tenía todas sus referencias. El generador ahora repara controladores existentes conservando sus GUID. El retarget de jefes compara nombres de huesos sin distinguir mayúsculas (`Head` fue la causa del fallo). El puente del editor propaga las excepciones de instalación en lugar de indicar éxito indebidamente.

- Biblioteca: `Assets/Resources/Presentation/Library.asset`.
- Controladores y clips generados: `Assets/Presentation/Animations/`. Tres perfiles (KayKit, Goblin y Demon), 17 estados por perfil: Idle, Ready, Movement, Patrol, Chase, Melee, HeavyAttack, Thrust, Shoot, ShootBow, ShootMagic, BowIdle, RifleIdle, BlockIdle, Hit, Death y Dash.
- Recursos examinados: `Assets/07_Animacion/`, animaciones incluidas con KayKit Adventurers y KayKit Skeletons, y modelos de jefes. Se usan clips compatibles del paquete KayKit; las variantes duplicadas de los paquetes de personajes no requieren duplicar estados. Animaciones de herramientas, simulación y objetos del mapa no tienen una acción equivalente en este alcance. Los jefes Puglin/Imp reciben adaptación a su esqueleto.
- `ActorPresentation` observa movimiento, ataques, daño y muerte. El daño sigue siendo responsabilidad del combate; no se aplica mediante eventos de animación. Las armas del jugador se vinculan a `handslot.r`/`handslot.l`, conservando sus ajustes de agarre. Se ajustó escala y posición de brazos tras revisión visual para evitar que ocupen casi toda la pantalla. El daño interrumpe la animación previa.
- VFX generados en `Assets/Presentation/Effects/`, reutilizando Magic effects pack con materiales URP. Impacto, polvo al desplazarse, explosión y destello. Se redujo el polvo tras revisión visual. Los destellos de jugador se sitúan en la punta del modelo; el arco no emite fogonazo. Hay un límite de 40 efectos vivos y destrucción temporal.
- `Assets/00_Scenes/MainMenu.unity` es la primera escena de Build Settings. Iniciar, Opciones y Salir están conectados. Opciones guarda volumen, sensibilidad y pantalla completa.
- Victoria y derrota tienen reinicio del nivel y regreso al menú. Los paneles seleccionan un botón al abrirse para navegación con teclado. El jugador no recaptura el cursor ni ataca mientras se muestran resultados. Se redujo el título de derrota para que quepa.

## Validación realizada

- Validación de biblioteca, 3 controladores, 51 estados con clips y curvas, partículas y materiales URP, escena inicial.
- Validación en Play de animadores de cuerpo/brazos, reproducción de estados y las diez armas bajo huesos de mano.
- `UIChecks`: **50 comprobaciones aprobadas**, incluyendo pausa, HUD, compra al mercader, cambio de escenas, jefe, derrota y reinicio.
- Invocación de los botones reales: Iniciar abre Calabozo; Opciones abre sus controles y Volver regresa; derrota permite reiniciar y volver al menú; victoria permite reiniciar Coliseo con jefe vivo y volver al menú; Salir abandona Play en el editor.
- Capturas de primera persona revisadas para ajustar brazos, agarre y polvo.

No se generó ni probó un ejecutable independiente. La revisión visual de todas las combinaciones clase/arma y poses de ambos jefes sigue siendo recomendable como ajuste artístico; la validación automatizada no sustituye esa revisión.

## Continuación y regeneración

Salir de Play y guardar la escena antes de usar `Codice > Presentation > Install animations effects and menus`. Validar con `Codice > Presentation > Validate generated assets`. Código de generación: `Assets/Editor/Presentation/PresentationSetup.cs`; comprobaciones: `PresentationChecks.cs` en esa misma carpeta. `PresentationChecks.Player()` se ejecuta en Play con un jugador existente.

Para repetir la regresión de interfaz, abrir Calabozo en una partida nueva y ejecutar `Codice > UI > Run UI checks (Play, fresh Calabozo)`. La prueba cambia escenas y estado de la partida; debe hacerse en una sesión de prueba.

Los cambios quedan locales en Leandro_Branches. Esta continuación no publica cambios ni mezcla otras ramas.
