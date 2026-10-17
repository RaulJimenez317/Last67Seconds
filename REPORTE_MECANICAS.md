# Entrega del punto 5: mecánicas

Proyecto: **Last67Seconds / Códice de Ceniza**. Rama de entrega: **Hiber_Branches**.
Base integrada antes de implementar: `052b013`, que incorpora los jefes y la corrección de derrota del Coliseo. Se actualizó la carpeta existente mediante avance directo; no se creó otro proyecto.

## Probar la partida

Abrir `Assets/00_Scenes/Maps/Calabozo.unity` y pulsar Play. La clase inicial sigue configurándose en el Inspector del jugador antes de Play.

- WASD/ratón, clic izquierdo, cambio de arma y dash mantienen sus controles.
- **Q o clic derecho**: especial del arma activa al llegar al 100%. La consola avisa cuando está listo. La carga pertenece a cada instancia de arma, incluido al soltarla o viajar.
- **E**: mercader, armas, pasivas, munición y portales. Usar un arma con munición faltante para recoger suministros.
- Con tres pasivas: **Shift + E**, **Ctrl + E** o **Alt + E** reemplazan la ranura 1, 2 o 3 del pickup. En la tienda, usar esos modificadores con **3** para comprar reemplazando la pasiva elegida. Sin elección no se sustituye ninguna ni se cobra.
- En la conversación del mercader, **Enter** continúa la ronda; E/Escape solamente cierra la conversación.
- Tras morir, **R** comienza una nueva partida en el Calabozo. En el Coliseo también funciona el botón ya existente, actualizado para el ciclo completo.

Los avisos de ronda, tienda, bonus, portal, especial listo y derrota se escriben en la consola. La interfaz completa de estos datos corresponde al punto 7. Esta entrega no añade un HUD nuevo.

## Rondas, tiempo y viajes

`RunDirector` coordina una partida entre las dos escenas existentes. `RunScene` proporciona sus referencias, sin duplicar jugador, mercader, enemigos ni jefes.

- Rondas 1–4: 5, 8, 12 y 16 enemigos, con la composición exacta del GDD.
- Rondas 6–9: 20, 25, 30 y 36; 11–14: 42, 48, 55 y 62; 16–19: 70, 78, 86 y 95.
- Desde 21, cada ronda de calabozo agrega 10 enemigos respecto a la anterior de calabozo como progresión provisional. Se mantiene un máximo configurable de ocho vivos simultáneamente, sin reducir el total de la ronda.
- La primera mitad termina en `ceil(total/2)` bajas. Se retiran disparos hostiles, aparece el mercader existente y no hay enemigos vivos durante las compras. Continuar genera la segunda mitad.
- Tras una ronda normal hay tres segundos de descanso y comienza la siguiente. Al finalizar nivel 4, 9, 14… se habilita el portal al jefe.
- Los niveles 5, 10, 15, 20… usan `BossEncounter`, con la alternancia y evolución ya implementadas. Al derrotarlo se habilita el portal de regreso al siguiente nivel del Calabozo.
- El viaje conserva clase, vida, oro, ambas armas con tier/munición/carga, ranura activa y tres pasivas. Los efectos temporales de una habilidad no atraviesan escenas.
- `Elapsed` mide tiempo de partida sin cargas de escena ni derrota; `RoundElapsed` mide combate y se congela durante la tienda. Completar combate en menos de 180 segundos entrega **100 oro**, una sola vez por ronda, también en rondas de jefe.
- La muerte pausa enemigos e impide progreso. Reiniciar recarga Calabozo y restablece nivel, vida, inventario inicial, oro, pasivas y temporizadores. No se implementa guardado de partidas en disco.

## Pasivas

| Pasiva | Efecto implementado |
|---|---|
| Frenesí | +25% cadencia y regeneración de una flecha cada 1,125 s en lugar de 1,5 s. |
| Corazón Vampírico | Cura 4% de vida máxima por enemigo básico eliminado y cada diez impactos directos contra un jefe. |
| Sobrecarga | +50% a toda la carga de ultimate: tiempo, bajas e impactos contra jefe. |
| Botas de Mercurio | +20% movimiento y segunda carga de dash, con enfriamiento independiente de 1,5 s; no permite superponer dos dashes en el mismo instante. |
| Coraza de Espinas | Reduce todo daño recibido un 20% y refleja 35 al atacante cuerpo a cuerpo. Proyectiles no provocan reflexión; dash e invulnerabilidad evitan el impacto completo. |

Reemplazar una pasiva elimina inmediatamente sus modificadores persistentes. No se admiten duplicados ni más de tres ranuras.

## Combate y especiales

La carga activa aumenta **1,5%/s durante combate**, **6% por baja básica**, **12% por pesado** y **2% por impacto directo a jefe**. Mientras un especial está ejecutándose no se recarga. Los ticks secundarios de sangrado y vórtice no producen carga por golpe, evitando recarga multiplicada artificialmente.

| Arma | Especial elegido del catálogo del GDD |
|---|---|
| Espada | Danza de Acero: ocho cortes de área durante dos segundos e invulnerabilidad. Cada corte escala con el daño base del tier. |
| Hacha | Hendidura Sísmica: cono frontal de 9 m, 180–450 de daño, ruptura de guardia, empuje y aturdimiento de tres segundos. |
| Arco | Lluvia Perforante: corredor de disparo de hasta 60 m que perfora enemigos, impacto de 260–600 y cuatro pulsos adicionales en su recorrido. Las paredes lo detienen. |
| Daga | Paso Sombrío: durante 3,5 s, +75% velocidad y críticos garantizados. |
| Martillo | Pulso Gravitatorio: atrae enemigos a corta distancia, golpea en radio de 7 m y aturde tres segundos. |
| Lanza | Empalamiento Imperial: golpe perforante en corredor de 12 m y avance de 3 m mediante CharacterController, respetando colisiones. |
| Guadaña | Tajo Sangriento: corredor de 15 m, empuje y curación del 15% del daño efectivo causado. |
| Escudo | Bastión Inmortal: invulnerabilidad cuatro segundos; termina con daño y empuje en radio de 6 m. |
| Báculo | Singularidad Arcana: vórtice de cuatro segundos, radio de 5 m, atracción y daño periódico. |
| Mosquete | Disparo Perforatitanes: penetración e ignorar guardia, explosión de radio 2,5 m contra objetivos laterales y sangrado durante tres segundos a los alcanzados por el disparo. No duplica el daño de explosión sobre el objetivo directo. |

Cuando el GDD ofrece variantes con «o», se implementa una de ellas por categoría. Las habilidades de impacto inmediato no crean una animación ni un VFX final: se resuelve su área/corredor de daño y se exponen estado y eventos para la siguiente fase.

Se completó además:

- Cadencia por tier de las diez categorías y carcaj de 8 a 12 flechas según tier.
- Críticos de arco (10%) y daga (25%), con daño x2; probabilidades provisionales.
- Mosquete básico perforante hasta una pared/cobertura sólida.
- Escudo básico: 50% de reducción en su cono frontal durante 0,65 s después de atacar.
- Aturdimiento para enemigos y jefes, guardia ignorada por los especiales penetrantes y protección frente al derribo durante invulnerabilidad.
- Regeneración de flechas independiente de capturar el cursor; se suspende durante controles bloqueados/tienda y muerte.

## Botín y escenario

- Las cuatro criptas existentes generan armas T2–T4 y pasivas alternadas, renovadas al comenzar cada ronda. Se limpian pickups anteriores al renovar la ronda.
- Las bajas entregan el oro ya definido por cada enemigo y pueden dejar armas (12%) o munición (33% adicional). Son probabilidades provisionales. No hay moneda física adicional que duplique la recompensa.
- Los pickups usan las dos ranuras y el intercambio existentes. Las pasivas usan su transacción de sustitución explícita.
- Los cuatro pilares del Coliseo reciben daño de armas y proyectiles. Al agotar 180 HP se desactivan sus colisiones y obstáculos de navegación.
- Se rehízo únicamente la navegación del Coliseo para excluir los pilares del horneado y usar obstáculos dinámicos con carving. La arquitectura del mapa se conserva.
- Los tres puntos de suministros existentes entregan ocho unidades a la munición del arma activa y vuelven a estar disponibles a los 15 s. No se consumen si el arma no usa munición o está llena.
- Portales y pickups tienen indicadores geométricos sencillos para poder interactuar; no son los efectos visuales finales.

## Archivos principales y API

| Archivo/carpeta | Responsabilidad |
|---|---|
| `Assets/02_Scripts/Mechanics/RunDirector.cs` | Nivel, fase, tiempo, bonus, continuación, viaje y reinicio. `Changed` permite conectar UI. |
| `RunScene.cs`, `RunPortal.cs`, `RoundRules.cs` | Referencias por escena, interacción con portal y composición de rondas. |
| `PlayerMechanics.cs` | Pasivas de combate, carga y diez especiales. `TryUltimate`, `Charge`, `UltimateActive`, `Changed`. |
| `CombatTargets.cs` | Consultas de daño con cobertura, aturdimiento y atracción. |
| `LootPickup.cs`, `DestructiblePillar.cs` | Recolección, sustitución, suministros regenerables y destrucción de cobertura. |
| `Assets/Editor/Mechanics/MechanicsSetup.cs` | Integración en escenas existentes; rechaza escenas con cambios sin guardar. |
| `Assets/Editor/Mechanics/MechanicsChecks.cs` | Validación de datos, combate y ciclo completo entre mapas. |
| `Assets/01_Prefabs/Mechanics/` | Material de indicadores y nueva navegación del Coliseo. |

Se ampliaron los contratos existentes de daño para distinguir melee, ignorar guardia y daño secundario. Los cambios en scripts de personajes/enemigos/jefes conectan estas mecánicas; sus modelos, IA base y ataques originales se conservan.

Los generadores antiguos de mapas/personajes no conocen todos los componentes de fases posteriores. No regenerar mapas ni reconstruir el prefab del jugador como parte de una prueba normal. Las dos escenas guardadas ya están integradas y no necesitan un generador al abrir el proyecto.

## Validación reproducible

Con Calabozo abierto y fuera de Play:

```powershell
unity command set_autotick
unity command eval 'MechanicsChecks.Arm(); return "Prueba preparada";'
unity command editor_play
# Tras finalizar el recorrido de prueba:
unity command eval 'return MechanicsChecks.Result;'
unity command editor_stop
```

La prueba arma su inicio después de la recarga del editor, usa objetos temporales, completa rondas y viaja entre escenas. Dejarla terminar y salir de Play antes de jugar normalmente. También se pueden ejecutar las 28 comprobaciones de datos mediante `MechanicsChecks.Data()` fuera de Play.

Resultados en Unity 6000.5.6f1:

- **MECHANICS_CHECKS_PASSED: 113**: incluye las 28 comprobaciones de datos, las cinco pasivas, los diez especiales, escudo frontal/trasero, perforación y explosión, botín, el ciclo real 1–6 entre escenas, conservación de vida/armas/munición, bonus único, muerte y reinicio.
- **CHARACTER_DATA_TESTS_OK: 26** y **CHARACTER_PLAY_TESTS_OK: 19** de las utilidades existentes.
- **ENEMY_CHECKS_PASSED: 32** y **BOSS_CHECKS_PASSED: 46**, con el coordinador de partida temporalmente desactivado para aislar esos encuentros; esa configuración de prueba no se guardó.
- **236 comprobaciones automatizadas superadas en total**, compilación de scripts sin errores.
- Dos verificaciones adicionales del pilar real del Coliseo: su centro no era navegable con el obstáculo activo y volvió a ser navegable después de destruirlo.
- Capturas antes/después revisadas: desaparición del pilar, limpieza del jefe y su lodo, suministros visibles y portal de regreso habilitado.

Son utilidades de integración propias, no una suite NUnit ni una certificación de balance o de un ejecutable standalone. No se generó una build distribuible. Se dejó Calabozo abierto fuera de Play con la integración normal guardada.

## Alcance y valores pendientes de balance

Esta entrega se limita a las mecánicas. No añade HUD completo, menús nuevos, animaciones finales, VFX, música ni sonido. Los datos y eventos permiten conectarlos después.

Valores no concretados por el GDD: críticos, defensa básica del escudo, radios/alcances de varias habilidades, velocidad extra de Paso Sombrío, vida de pilares, probabilidades de botín y cantidades después de nivel 19. Se documentan arriba para su ajuste. Las habilidades usan interpolación por tier; salvo espada y arco, el impacto principal parte de 180 y llega a 450. Sangrado del mosquete: 20% adicional del daño principal; explosión lateral: 50%. No se implementaron todas las variantes alternativas de cada especial.

Los ajustes locales ajenos a esta entrega se conservan fuera del commit.
