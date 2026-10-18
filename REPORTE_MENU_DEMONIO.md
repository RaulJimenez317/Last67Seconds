# Menú Last67Seconds y orientación del Demonio

Base de trabajo: `fed4be4`, coincidente con `origin/dev`. Se trabajó en el proyecto existente, rama `Raul_Branches`.

- Título visible del menú: **Last67Seconds**, con el número 67 destacado en ámbar. Se actualizó la identificación del juego en opciones y selección de clase.
- Botones propios dibujados con geometría UI: placa oscura biselada, borde de bronce, insignia de reloj de arena y marcas de tiempo. Selección y hover iluminan el borde; la pulsación oscurece la placa. Los botones conservan sus acciones, navegación de teclado y compatibilidad con el EventSystem.
- Demonio: rotación visual de 180 grados en su prefab y en el generador. La navegación ya no sobrescribe la orientación controlada por el combate, y aparece mirando al jugador. Se conservan las direcciones fijadas al preparar ataques, las fases y la retirada con arco.

Validación en Unity 6000.5.6f1:

- Compilación sin errores.
- `DEMON_ORIENTATION_PASSED: 48`: cuatro direcciones, niveles 10 y 20, aparición, retirada y recuperación.
- `BOSS_ORIENTATION_CHECKS_PASSED: 27`: regresión del Goblin.
- Comprobación de navegación principal, selección de tres clases, opciones y retorno; resaltado de selección.
- Capturas revisadas de las tres pantallas del menú y del Demonio de frente en Coliseo.

Las pruebas de orientación pueden repetirse en Play en Coliseo con un boss activo mediante `DemonOrientationChecks.Run()` y `BossOrientationChecks.Run()`. Son utilidades de editor, no pruebas de un ejecutable standalone.
