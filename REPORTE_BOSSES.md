# Entrega de bosses (punto 4)

Implementación en el proyecto existente Last67Seconds, rama `Raul_Branches`.

## Contenido

- **Rey Goblin**: rocas de 35 de daño con derribo de 0,6 s, proyectiles de lodo que dejan charcos de 8 s y ralentizan 35%, invocación de 2–3 magos cada 15 s y al cruzar cada umbral de 25% de vida.
- **Goblin evolucionado desde nivel 15**: textura alternativa de su carpeta, dos rocas por ataque, lodo tóxico de 8 de daño por segundo e invocación de 3–4 magos potenciados.
- **Señor Demonio**: barridos y machacazos de 45 de daño; desde fase media salta hacia atrás y dispara con arco (40 por flecha). Tres fases con umbrales de 65% y 30%, velocidades 1 / 1,3 / 1,6 y ciclos de invocación de 20 / 15 / 10 segundos. Alterna los cuatro enemigos básicos existentes.
- **Demonio evolucionado**: textura alternativa, proyectil frontal de fuego al golpear, cinco flechas explosivas en abanico y grupos de invocación mayores con escalado de los enemigos existentes.
- Preparación visible de ataques, orientación fijada antes del impacto, colisión de proyectiles contra obstáculos y limpieza de invocaciones/proyectiles/charcos al terminar el combate. Dash evita daño y derribo.

Se utilizan Puglin e Imp de la carpeta Bosses, los enemigos básicos existentes, hacha y arco de los recursos del proyecto. Los modelos de bosses no contienen clips de animación; la presentación usa poses procedurales simples, no animación esquelética completa de combate.

## Integración y prueba manual

Abrir `Assets/00_Scenes/Maps/Coliseo.unity`. El objeto **Bosses (Phase 4)** tiene `BossEncounter` y navegación guardada. Pulsar Play inicia el combate con el jugador existente.

Cambiar `Level` antes de Play para probar:

| Nivel | Boss |
|---|---|
| 5 | Goblin base |
| 10 | Demonio base |
| 15 | Goblin evolucionado |
| 20 | Demonio evolucionado |
| 25, 30… | Alternancia de ambos evolucionados |

No se regeneraron los mapas. Calabozo conserva su encuentro de enemigos básicos. Los pilares existentes bloquean disparos; su destrucción y los suministros de flechas pertenecen a la integración posterior de mecánicas del escenario.

## API para próximas fases

- `BossEncounter.BeginEncounter(level)`: acepta múltiplos de 5 desde 5; rechaza duplicados, jugador muerto o navegación ausente. Desactivar `startOnPlay` cuando el futuro controlador de rondas gestione la entrada.
- `ActiveBoss`, `Completed`, `Spawned` y `onCompleted`: conectar el viaje de regreso y la progresión.
- `BossCharacter.Health`, `MaxHealth`, `Phase`, `CurrentAttack`, `Changed`: datos para la futura UI.
- `HitByPlayer`: conectar carga de ultimate (+2% por impacto según GDD) y pasivas. Esta fase solo emite el evento.
- `SetPaused(bool)`: pausa boss, invocaciones y sus ataques.
- `Died`: se emite una sola vez. El oro se entrega directamente al jugador atacante, como en los enemigos básicos.

Los prefabs están en `Assets/01_Prefabs/Bosses`. Los scripts de ejecución están en `Assets/02_Scripts/Bosses`; `BossSetup` permite integrar una escena aún no integrada y actualizar el arco del Demonio. Rechaza sobrescribir escenas abiertas con cambios sin guardar.

## Valores provisionales

El GDD no especifica vida, oro, daño tóxico, radio explosivo ni límite de invocados. Se usan 1200 HP / 300 oro para Goblin, 1800 HP / 450 oro para Demonio; evolución multiplica vida por 1,5. Fuego: 20 de daño; explosión: radio 2,5 m; máximo 12 invocados vivos por boss. Los grupos evolucionados del Demonio agregan dos enemigos. Los invocados evolucionados usan como mínimo el escalado de nivel 16.

## Alcance

Esta entrega cubre bosses. No añade progresión automática de rondas, portales entre mapas, HUD completo, música, efectos finales, ultimates ni pasivas. El botín se representa mediante oro directo; no se crea un sistema de botín físico. El cambio mínimo al jugador añade ralentización temporal y derribo para estos ataques. `EnemyProjectile.Owner` permite limpiar únicamente los disparos de invocados del boss.

### Corrección de derrota sin aviso

Al morir, el jugador liberaba el cursor y bloqueaba controles sin aviso ni forma de continuar. Se reprodujo con HP=0, `ControlsBlocked=true`, `timeScale=1` y editor sin pausa. Coliseo muestra ahora la vida, el estado del boss y un panel de derrota con botón **Reintentar combate (R)**. El reintento conserva el nivel configurado, vuelve al punto de entrada y restablece vida, equipo inicial, oro, munición y controles; limpia ralentización, derribo, invocados y ataques del intento anterior. Se rechazan reintentos duplicados o con el jugador vivo. Al liberar el cursor con Escape se indica cómo recuperarlo.

El cambio local previo en `Assets/00_Scenes/Game.unity` se conserva fuera del commit.

## Validación reproducible

Resultado obtenido en Unity 6000.5.6f1 tras corregir el flujo de derrota: **BOSS_CHECKS_PASSED: 46**, compilación sin errores y **CHARACTER_PLAY_TESTS_OK: 19 assertions** de `CharacterPlayChecks.Run()`. El reporte anterior mencionaba 26; la utilidad presente en esta rama ejecuta 19. Las once comprobaciones adicionales cubren derrota, pausa, reintento, limpieza, restauración y conservación del nivel. Se inspeccionaron capturas en Play de Goblin base y Demonio evolucionado dentro del Coliseo.

En una sesión nueva de Play en Coliseo, esperar a que aparezca el Goblin y ejecutar:

```powershell
unity command eval 'BossPlayChecks.Begin(); return BossPlayChecks.Result;'
# Consultar después de unos segundos de juego:
unity command eval 'return BossPlayChecks.Result;'
```

La utilidad consume varios encuentros y modifica el estado del jugador de prueba; salir y volver a Play después. Está excluida del ejecutable mediante `UNITY_EDITOR`. Comprueba navegación, alternancia, fases, materiales, invocaciones, pausa, daño inválido, recompensa/muerte únicas, dash, derribo, proyectil real y limpieza de charcos. No sustituye pruebas de balance ni una prueba de build standalone.
