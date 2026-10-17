# Entrega del punto 7: interfaz, HUD y menús

Proyecto: **Last67Seconds / Códice de Ceniza**. Rama: **Hiber_Branches**. Base: `f83e0a6` (mecánicas).
Unity **6000.5.6f1**, URP, uGUI y Input System existentes. Se trabajó en la misma carpeta y proyecto.

## Probar la entrega

Abrir `Assets/00_Scenes/Maps/Calabozo.unity` o `Coliseo.unity` y pulsar **Play**. No hay que regenerar mapas ni ejecutar los instaladores de jugador, enemigos o jefes.

- **1 / 2 / Tab**: cambiar arma; la hotbar muestra las dos instancias reales, tier, daño y cadencia.
- **Q / clic derecho**: especial; medallón radial, nombre, porcentaje, estado activo y pulso al estar listo.
- **Espacio / Shift**: dash; cooldown real y segunda carga si está equipada Botas de Mercurio.
- **E**: mercader, botín o portal. La tarjeta próxima compara armas en dos columnas.
- **Shift + E / Ctrl + E / Alt + E**: reemplazar pasiva 1/2/3 cuando las tres ranuras están ocupadas.
- Tienda: botones o **1–4** para comprar; **Shift+3 / Ctrl+3 / Alt+3** o botones de ranura para elegir el reemplazo. **Enter** continúa; **E/Escape** cierra la conversación.
- **Escape**: pausa real y libera cursor; Escape o el botón reanudan. Tras perder el foco, hacer clic para recapturar el cursor.
- Derrota: **R** o botón para reiniciar desde Calabozo, usando `RunDirector.RestartRun`.

**Clase inicial:** se eligió la alternativa solicitada de sincronización con el Inspector. Configurar `PlayerCharacter.initialClass` antes de Play. La UI identifica la clase real y muestra las estadísticas en pausa; no reinicializa el inventario ni abre un selector que interfiera con las pruebas.

## HUD y dirección visual

`ProceduralUISprites` genera y reutiliza texturas/sprites en memoria: marcos de esquinas recortadas con doble borde, hexágonos, diamantes, anillos, viñeta y diez siluetas de arma. Los marcos usan slicing para conservar los biseles al cambiar tamaño. No se requieren imágenes, fuentes descargadas, materiales externos ni paquetes adicionales.

- Paleta de tiers: ceniza `#8C8C8C`, cobalto `#2D7FF9`, púrpura `#9B4DFF` y dorado `#FFB800`; fondo oscuro, cian para habilidades y oro para economía.
- Vida carmesí inmediata y rally/ghost amarilla con espera de **0,35 s** y convergencia exponencial mediante `Mathf.Lerp`. Se aplica al jugador, jefe, enemigos y pilares.
- Hotbar activa a **1,15×**, elevación de **10 px**, pequeño rebote y borde del tier. Tres hexágonos muestran las pasivas equipadas.
- Munición real y regeneración del arco en anillos de la mira y hotbar; intervalo de **1,5 s / 1,125 s con Frenesí**. El mosquete conserva su sistema anterior de suministros; la UI no inventa una recarga automática.
- Sacudida de vida, viñeta al recibir daño, oro con pop y estados de lodo, derribo, invulnerabilidad, escudo frontal y Paso Sombrío.
- Cronómetros de partida y ronda `MM:SS.cc`, indicador de elegibilidad del bono, progreso de bajas, hito del mercader y aviso de niveles de jefe.
- Barra del jefe con nombre completo, evolución Nv.15+, vida real, fase, marcas al 65%/30% y ataque telegrafiado.
- Cola de banners para inicio, mercader, bono efectivamente concedido, especial listo y portal abierto.
- Tienda conectada a ofertas y transacciones reales, precios, vendido, oro insuficiente, curación a vida completa y elección explícita de reemplazo.
- Canvas de referencia **1920×1080**, escalado con pantalla y paneles anclados a esquinas/centro.

## UI en el mundo

- Retícula según tipo de arma, recarga y hitmarkers normales, críticos ×2 o bloqueo frontal −80%.
- `WorldHealthBar`: Canvas **WorldSpace**, billboard hacia la cámara del jugador, HP real, escudo de pesado/rotura y aturdimiento. Los pilares muestran integridad solamente después de recibir daño. Las barras se ocultan a más de 35 m; el renderizado world-space respeta profundidad.
- Números de daño proyectados desde posiciones del mundo; los impactos básicos y proyectiles comunican su punto de impacto. Los ataques de área usan el centro del objetivo. Se distinguen críticos, daño bloqueado, reflexión y curación real; las curaciones propias se sitúan frente al jugador para poder leerse en primera persona.
- Comparación equipada/suelo con tier, daño, flecha de mejora/empeoramiento, cadencia y especial. Pasivas llenas muestran nombres de las tres ranuras y los modificadores de reemplazo.
- Selección de tarjeta por proximidad, dirección de mirada y visibilidad; no revela objetos detrás de paredes. La recogida sigue siendo la interacción por rayo original.
- Waypoints del mercader disponible, portal activo y suministro de munición más próximo cuando queda como máximo un tercio de capacidad. Los marcadores se limitan al área visible de pantalla.

## Integración y contratos

| Archivo | Responsabilidad |
|---|---|
| `Assets/02_Scripts/UI/RunHUD.cs` | Instalación idempotente, HUD, eventos, banners, estado y animaciones. |
| `Assets/02_Scripts/UI/RunHUD.Menus.cs` | Pausa, derrota, compra y reemplazos. |
| `Assets/02_Scripts/UI/RunHUD.World.cs` | Barras billboard, números, comparación y waypoints. |
| `Assets/02_Scripts/UI/UIWidgets.cs` | Construcción uGUI, nombres localizados y `GhostBar`. |
| `Assets/02_Scripts/UI/ProceduralUISprites.cs` | Rasterización y caché de sprites. |
| `Assets/02_Scripts/Characters/CombatFeedback.cs` | Evento observacional de daño resuelto, sin calcular ni aplicar daño. |
| `Assets/Editor/UI/UISetup.cs` | Comprueba las dos escenas y Build Settings sin reescribirlas. |
| `Assets/Editor/UI/UIChecks.cs` | Pruebas específicas de interfaz y viajes reales. |
| `Assets/Editor/UI/UIRegressionChecks.cs` | Ejecuta secuencialmente las suites anteriores y la nueva. |

`RunScene.Start` instala una UI perteneciente a la escena antes de iniciar la ronda. Viajar destruye esa UI y sus suscripciones; la siguiente escena obtiene una instancia nueva. La instalación repetida sobre la misma escena devuelve la instancia existente. El `EventSystem` utiliza `InputSystemUIInputModule` y solamente se crea si no existe uno.

Se escucha `CharacterState.Changed/Died`, `PlayerMechanics.Changed`, `RunDirector.Changed/Notice` y el resultado de daño. Los valores continuos y referencias a jefe/tienda se consultan directamente. El inventario, dinero, ofertas y reglas siguen perteneciendo a sus sistemas originales.

Cambios mínimos de soporte:

- Lectura de los dos cooldowns de dash, escudo temporal y stun de básicos.
- `DamageInfo` transporta metadatos de crítico/reflejo/punto de impacto; `PlayerProjectile.Initialize` conserva compatibilidad mediante argumento opcional.
- Enemigos, jefe y pilares emiten el daño efectivamente recibido, después de aplicar sus reglas existentes.
- `RunDirector.Notice` anuncia resultados reales. No se deduce un bono por diferencias arbitrarias de oro.
- El `OnGUI` antiguo del Coliseo se suprime solamente cuando existe la UI nueva.
- La gestión de Escape evita que el controlador vuelva a liberar el cursor en el mismo frame en que la UI reanuda.

**No se regeneraron ni guardaron cambios en Calabozo, Coliseo, NavMeshes o el prefab del jugador.** Los ajustes locales preexistentes de `ProjectSettings`, URP y `.vscode` no forman parte de esta entrega.

## Validación

Menús disponibles:

1. `Codice/UI/Validate installation (no scene rebuild)`.
2. `Codice/UI/Run UI checks (Play, fresh Calabozo)`; también `UIChecks.Arm()` antes de entrar a Play.
3. `Codice/UI/Run ALL checks (236 previous + UI)` desde Edit, con escenas guardadas. Prepara fixtures temporales, ejecuta cada suite en una sesión nueva y restaura la escena inicial. Utiliza temporalmente Free Aspect para reducir la carga del editor y restaura la resolución elegida al acabar.

| Suite | Comprobaciones |
|---|---:|
| CharacterSetup / transacciones de datos | 26 |
| CharacterPlayChecks | 19 |
| EnemyPlayChecks | 32 |
| BossPlayChecks | 46 |
| MechanicsChecks, incluidas sus 28 pruebas de datos | 113 |
| UIChecks | 50 |
| **Total** | **286** |

Las pruebas de UI cubren sprites, inventario real, barras instantánea/retardada, curación, carga radial, dash doble, pausa y reloj, comparaciones, comandos de pasivas, resultado crítico, world-space, compra única y cierre, portal al nivel 5, jefe evolucionado, integridad de pilar, regreso al nivel 6 y derrota/reinicio sin duplicar HUD.

La primera ejecución agrupada con la vista Full HD encontró un fallo temporal en la comprobación existente del escudo (espera de reloj de 4,2 s frente a 4 s de simulación). No se cambió la habilidad ni se quitaron aserciones: el runner reduce temporalmente el viewport y solicita actualizaciones del editor para ejecutar las pruebas con menos carga.

Validación visual: HUD, tienda, pausa, jefe y comparación de botín en Game View. No se generó ni probó un ejecutable independiente. Audio, música y efectos de mundo ajenos a la interfaz permanecen fuera de este punto.
