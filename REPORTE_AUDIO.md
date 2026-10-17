# Música y efectos del juego

Se usan los recursos existentes en `Assets/05_Music` (la carpeta referida como 005_musicas).

## Música

- MainMenu: Castle of Intrigue.
- Calabozo: Medieval.
- Coliseo: Battle Warrior Fighting Drums.

Cada pista se reproduce en bucle. Dos fuentes hacen la transición gradual entre escenas. La música baja durante pausa y resultados; el volumen general sigue el ajuste guardado de Opciones. La carga de música usa streaming.

## Efectos

23 categorías: botones, espada, arco, fusca, magia, armas pesadas, impactos, escudo, daño, muerte, pasos, oro, recogida, cofre, compra, error, recarga, pasivas, dash, explosión, victoria, derrota y jefe.

Los efectos se disparan desde las acciones confirmadas: no suenan disparos rechazados por cadencia ni compras fallidas como compras exitosas. Enemigos y efectos del mundo usan sonido 3D con alcance de 24 metros; acciones personales y avisos usan sonido 2D. Los pasos tienen cuatro variantes. El escudo utiliza un choque corto, evitando el archivo de 79 segundos.

El gestor persiste entre escenas, limita los efectos a 24 fuentes (cuatro reservadas a interfaz), controla repeticiones y detiene los efectos al cambiar de escena. La pausa suspende efectos del combate y permite los botones. La variación sonora usa su propio generador aleatorio para no cambiar el botín ni otros resultados del juego.

## Mantenimiento

- `GameAudioLibrary`: referencias a pistas, clips, volúmenes y tiempos mínimos entre repeticiones.
- `GameAudio`: reproducción, transiciones, pausa y observación de resultados de ronda.
- `AudioButton`: sonido de botones, tanto con ratón como con teclado.
- `GameAudioSetup.Install`: reconstruye `Assets/Resources/GameAudioLibrary.asset` desde los archivos originales; menú `Codice/Audio/Install library` (fuera de Play).
- `GameAudioChecks.Run`: comprobaciones de pistas, acciones, pausa, transiciones, fuentes y victoria dentro de Play.

No se añadieron archivos de audio externos ni se modificaron las reglas de daño, recompensas o controles.

## Validación

- Unity 6000.5.6f1: compilación sin errores.
- `GameAudioChecks`: 17 comprobaciones aprobadas de asignación y reproducción, acciones, volumen, pausa, transiciones y victoria.
- `PlayerPolishChecks`: 90 comprobaciones de regresión del jugador aprobadas.
- Las pruebas comprueban el estado de reproducción de Unity; el ajuste fino de la mezcla puede hacerse en GameAudioLibrary según los altavoces del equipo.
