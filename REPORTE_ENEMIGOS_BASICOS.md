# Entrega: enemigos básicos (punto 3)

Proyecto existente: Last67Seconds / Códice de Ceniza. Rama: `Hiber_Branches`.

## Contenido

Se integraron cuatro prefabs con los modelos y armas de `NormalEnemies/KayKit_Skeletons`, materiales URP, animación idle/caminar y preparación visible del ataque:

| Prefab | Vida | Daño | Oro | Comportamiento |
|---|---:|---:|---:|---|
| Swordsman | 65 | 12 | 10 | Persigue y ataca cuerpo a cuerpo; preparación de 0,35 s y recuperación de 0,7 s. |
| Crossbowman | 55 | 18 | 15 | Busca línea de visión, mantiene distancia, apunta 0,8 s y recarga 2 s; virote a 24 m/s. |
| Mage | 80 | 22 por orbe | 20 | Mantiene distancia y dispara tres orbes en abanico, separados 12 grados, a 9 m/s. |
| Heavy | 220 | 32 | 35 | Avance lento, hachazo preparado durante 0,9 s, reducción frontal del 80% y escudo rompible. |

La variante del mago elegida es el abanico que permite el GDD. No tiene explosión de área. Los tres orbes pueden dañar por separado si el jugador recibe más de uno.

El pesado recibe daño completo desde atrás; hachas y martillos rompen permanentemente su guardia, ocultan su escudo e interrumpen el ataque. Se aplica el empuje comunicado por las armas existentes, limitado a 12 m/s. El bloqueo considera un cono frontal de 120 grados y afecta los impactos ordinarios del contrato actual, que no distingue la categoría del proyectil.

## Integración

- Se añadió el grupo `Basic enemies (Phase 3)` a **Calabozo**, con navegación horneada y un encuentro inicial de **3 espadachines + 2 ballesteros**.
- Aparecen primero tres enemigos. Al eliminarlos, se llama a `Merchant.ReportRoundProgress(1, 3, 5)`, se retiran proyectiles hostiles y queda un intervalo seguro para comprar. `ContinueRound()` genera los dos restantes.
- No se duplicaron jugador ni mercader. La arquitectura del mapa y Coliseo se mantienen.
- El daño al jugador usa `PlayerCharacter.ReceiveDamage`, por lo que respeta el dash. El jugador golpea enemigos mediante `IDamageReceiver`.
- La baja desactiva collider/navegación, entrega oro directamente al jugador atacante una sola vez, notifica `Died` y elimina el objeto. No se implementan botines físicos ni sonido de muerte en esta fase.
- Los proyectiles usan barrido de colisión, se detienen en paredes y otros colliders sólidos, y caducan a los cinco segundos. No dañan a otros enemigos.

## Cómo probar

1. Abrir `Assets/00_Scenes/Maps/Calabozo.unity` y pulsar Play.
2. Jugar con los controles existentes: WASD, ratón, clic izquierdo y dash con Espacio/Shift.
3. Después de tres bajas, ir al centro, mirar al mercader y pulsar E. Enter desde su conversación continúa el encuentro.
4. Para probar mago y pesado, salir de Play y cambiar `Composition` en `Basic enemies (Phase 3)` por los cuatro valores del enum; es una configuración del encuentro, no hace falta regenerar mapas. Restaurar la composición inicial al finalizar si se quiere conservar el nivel 1 del GDD.

## Archivos y API para las siguientes fases

- `Assets/02_Scripts/Enemies/BasicEnemy.cs`: vida, defensa, persecución, retirada, preparación de ataques y recompensa. `Initialize(player, level)` se llama al crear una instancia; `SetPaused(bool)` permite pausarla. No es una API de reutilización de cadáveres/pooling.
- `EnemyProjectile.cs`: desplazamiento e impacto de virotes/orbes.
- `BasicEnemyEncounter.cs`: únicamente el encuentro configurado, contadores `Defeated/Total` y evento `Completed`. El evento no inicia otra ronda automáticamente.
- `Assets/01_Prefabs/Enemies/`: cuatro enemigos, dos proyectiles, materiales, clips/controlador y navegación guardada.
- `Assets/Editor/Enemies/EnemySetup.cs`: integración inicial y actualización de presentación. La integración se niega a duplicar el grupo existente y a sobrescribir una escena con cambios sin guardar.
- `Assets/Editor/Enemies/EnemyPlayChecks.cs`: comprobaciones de integración en Play.

`Initialize(player, level)` admite el escalado de vida del GDD: +15% desde nivel 6, +30% desde 11 y +50% desde 16. Desde 16 se aplica +10% de velocidad como valor provisional, pues el GDD no concreta ese porcentaje. Velocidades base provisionales: 4,2 / 3 / 2,5 / 1,8 m/s. No se aumenta el daño, al no existir una fórmula definida.

El evento `Died` y `UltimateChargeReward` (6 o 12 para el pesado) permiten conectar la futura carga de ultimate; esta entrega no aplica esa mecánica. Para invocar enemigos en otro mapa será necesario disponer de navegación allí y llamar a `Initialize` después de instanciar el prefab.

## Validación

- Compilación en Unity 6000.5.6f1 sin errores de scripts.
- **32 comprobaciones de enemigos en Play**: valores base, posiciones sobre navegación, recompensa y muerte únicas, defensa frontal/trasera, ruptura de guardia, rechazo de NaN, escalado, avance real de fotogramas, proyectil contra jugador/pared, dash, persecución, pausa, preparación y daño melee, tres orbes del mago e interludio/continuación/finalización del encuentro.
- **26 comprobaciones existentes de transacciones de personajes** ejecutadas de nuevo.
- Comprobación adicional del ataque real del jugador contra `Swordsman`: un golpe de espada resta 18 HP.
- Revisión visual de los cuatro prefabs dentro del Calabozo y prueba de persecución real desde las entradas del mapa.

Para repetir las pruebas de enemigos, iniciar una sesión nueva de Play en Calabozo, esperar que el editor termine la transición y ejecutar:

```powershell
unity command eval 'EnemyPlayChecks.Begin(); return EnemyPlayChecks.Result;'
# Después de unos segundos:
unity command eval 'return EnemyPlayChecks.Result;'
```

El resultado esperado es `ENEMY_CHECKS_PASSED: 32`. La utilidad crea objetos temporales y consume el encuentro para comprobar su finalización; salir y volver a entrar en Play después de probar. Las pruebas son utilidades propias, no una suite NUnit ni una prueba de un ejecutable standalone.

## Alcance pendiente

Se entrega solo el punto 3. El encuentro no incorpora progresión automática de rondas, portales ni jefes; tampoco HUD, efectos especiales, música, pasivas o ultimates. Los proyectiles visibles y la animación básica sirven para leer los ataques. No se certificó balance final ni navegación exhaustiva bajo todas las configuraciones posibles.

Se conservaron fuera del commit los ajustes locales del editor y los cambios de configuración ajenos a esta entrega.
