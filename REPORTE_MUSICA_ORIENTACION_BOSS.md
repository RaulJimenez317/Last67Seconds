# Corrección de música y orientación del Rey Goblin

- Música: reducción adicional del 30% sobre el ajuste anterior (`0,85 × 0,70 = 0,595`). El volumen normal de las fuentes pasa de 0,272 a 0,1904. Los efectos y el volumen general guardado en Opciones no se modifican.
- Rey Goblin: se corrigió el eje visual invertido del modelo con un giro local de 180° en su prefab. La misma corrección queda en el instalador para futuras reconstrucciones.
- Al aparecer, el jefe se orienta hacia el jugador. Durante su combate activo sigue su dirección también durante preparación y recuperación de ataques; NavMesh no sobrescribe ese giro. La dirección de ataque anunciada y las estadísticas se conservan.
- La corrección visual también se aplica a su versión evolucionada. No se modificó el prefab del Señor Demonio ni los mapas.

Validación realizada en Unity 6000.5.6f1:

- Compilación sin errores ni advertencias.
- Comprobación visual del frontal del modelo durante Play Mode.
- `BossOrientationChecks.Run()`: 27 comprobaciones aprobadas; orientación inicial, cuatro direcciones en reposo/preparación/recuperación, eje visual y versión evolucionada.
- `GameAudioChecks.Run()`: 19 comprobaciones aprobadas; ganancia de música, efectos de combate/UI, pausa, volumen general y transiciones entre escenas.
- `git diff --check`: sin errores de formato.

No se regeneraron mapas ni el prefab del jugador.
