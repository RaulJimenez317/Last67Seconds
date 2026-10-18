# Ajuste de oleadas y volumen de música

Base actualizada desde `origin/Hiber_Branches`: **ea76c9e** (música y efectos). La carpeta estaba limpia; se actualizó por fast-forward sin descartar archivos ni crear otro proyecto.

## Música

Se añadió `musicVolume = 0.85` a `GameAudioLibrary`, aplicado a las dos fuentes de música. Reduce un **15%** la música de menú, Calabozo y Coliseo, incluyendo sus estados de pausa y resultados:

- Normal: 0,32 → **0,272**.
- Pausa: 0,12 → **0,102**.
- Derrota / ronda completada: 0,18 → **0,153**.

El volumen guardado en Opciones continúa funcionando. No se modificaron los volúmenes de efectos, pistas asignadas, bucles ni transiciones. El multiplicador se puede ajustar en `Assets/Resources/GameAudioLibrary.asset`.

## Cantidad de enemigos

`RoundRules` calcula el **total de enemigos de una oleada normal**, distinto del máximo simultáneo de ocho enemigos que ya controla `BasicEnemyEncounter`.

| Niveles | Máximo por oleada normal |
|---|---:|
| 1–10 | 10 |
| 11–20 | 20 |
| 21–30 | 30 |
| 31–40 | 40 |
| Cada bloque posterior de diez niveles | +10 |

Se conservan las cantidades introductorias: **5 enemigos en nivel 1**, **8 en nivel 2**. Los demás niveles normales usan el máximo de su bloque. Los múltiplos de cinco siguen siendo encuentros de jefe y no reciben una oleada normal.

El mercader continúa apareciendo en `ceil(total/2)` bajas; el contador de UI y la finalización utilizan el total real de la nueva composición.

## Desbloqueos y probabilidades

- Nivel 1: solamente espadachines.
- Nivel 6: se habilita el enemigo a distancia existente (`Crossbowman`).
- Nivel 11: se habilitan magos.
- Nivel 16: se habilitan tanques (`Heavy`).

Cada posición de la oleada se sortea con probabilidades normalizadas. Nunca se selecciona un tipo bloqueado, tampoco si el generador devuelve exactamente 1.

Desde nivel 11 aumenta gradualmente la probabilidad del mago; desde 16 también aumenta la del tanque. El peso restante se reparte 65/35 entre espada y arco. Ambas frecuencias débiles disminuyen conforme crecen las fuertes. La curva se suaviza durante 30 niveles y tiende a 40% magos, 50% tanques y 10% enemigos débiles, conservando variedad.

| Nivel normal | Espada | Arco | Mago | Tanque |
|---|---:|---:|---:|---:|
| 6 | 65,00% | 35,00% | 0% | 0% |
| 11 | 55,25% | 29,75% | 15,00% | 0% |
| 16 | 46,26% | 24,91% | 18,84% | 10,00% |
| 21 | 40,15% | 21,62% | 22,09% | 16,14% |
| 31 | 30,61% | 16,48% | 27,16% | 25,74% |

Son probabilidades por aparición, no cuotas garantizadas por oleada. Los sorteos pueden producir composiciones distintas entre partidas.

**Los jefes y sus invocaciones no se modificaron:** conservan alternancia, estadísticas, evolución, fases, ataques, cantidad y tipos de invocados. Tampoco se modificaron mapas ni prefabs.

## Validación

- `RoundBalanceChecks.Run()` / menú `Codice/Mechanics/Validate wave balance`: límites de bloques, rondas 1–200, exclusión de jefes, desbloqueos exactos, probabilidades normalizadas y monótonas, selección por intervalos y 100.000 sorteos con semilla fija. Restaura el estado aleatorio global al terminar.
- `MechanicsChecks.Data()`: se actualizaron las expectativas antiguas de cantidades y del tanque de nivel 3 a las reglas nuevas. Se conserva la cobertura de inventario, pasivas, munición y persistencia.
- `GameAudioChecks.Run()`: incluye la comprobación del multiplicador 0,85 y del volumen real de la fuente, además de reproducción, efectos, pausa, transiciones y victoria.

### Resultados obtenidos

- Compilación en Unity 6000.5.6f1: **sin errores**.
- `RoundBalanceChecks`: **1.641** comprobaciones aprobadas.
- `MechanicsChecks.Data`: **28** comprobaciones aprobadas.
- `GameAudioChecks`: **19** comprobaciones aprobadas.
- `RoundBalancePlayChecks`: **31** comprobaciones aprobadas de rondas 1–4, mercader, jefe de nivel 5 con 1.200 HP, regreso a nivel 6 y generación/completado real de oleadas de niveles 16 y 21 con límites 20/30 y concurrencia limitada.
- Total de esta validación: **1.719** comprobaciones. No se generó un ejecutable independiente ni se realizó una evaluación auditiva del balance; se verificaron las fuentes y sus volúmenes en Unity.
