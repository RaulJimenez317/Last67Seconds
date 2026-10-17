# Correcciones de controles y presentación del jugador

Base: `81cd410`, coincidente con `origin/dev` al comenzar. Trabajo en el mismo proyecto y en `Raul_Branches`.

## Cambios

- Clic izquierdo ataca con las diez armas y las tres clases. Clic derecho bloquea para el guerrero o al equipar escudo; no dispara ni activa especiales. Q conserva la habilidad especial. Atacar con el arma escudo ya no activa automáticamente la defensa.
- Espacio salta (altura configurable, 1,25 m por defecto); Shift conserva el dash. No se permite salto repetido en el aire, durante derribo, dash o pausa. Se corta la velocidad vertical al chocar con un techo.
- Hacha y guadaña: corrección de orientación respecto al agarre animado. Los barridos ignoran colliders propios y pueden alcanzar varios enemigos; paredes y objetos sólidos siguen bloqueando el daño. Los enemigos pegados al jugador se evalúan por su volumen, evitando rechazos por un punto de contacto debajo de la cámara.
- Arco: modelo FBX para Unity, escala corregida y posición visible en primera persona. Flecha preparada visible con munición y modelo visible en los proyectiles disparados, conservando la colisión por barrido y el daño existente.
- Cofres: el HUD y la tecla E usan `InteractionTarget.Find`, con alcance, cono de mirada, colliders hijos y comprobación de obstáculos compartidos. Se reconocen cofres `chest_gold` y `chest`, excluyendo los huesos de personajes llamados `chest`. Se conservan precios, apertura única y botín.
- Monedas: modelo centrado sobre el collider y apoyado sobre el suelo. El pivote importado provocaba una órbita de varios metros al girar. Se retiró el desplazamiento ascendente hacia el jugador; siguen recogiéndose por cercanía sin atravesar paredes ni acreditar oro dos veces.
- Ayuda del menú, pausa, HUD y aviso de habilidad actualizados con los controles reales.

## Validación

Unity 6000.5.6f1. Resultados obtenidos en Play:

- `PLAYER_POLISH_PASSED: 90`: clics para cada arma/clase, defensa, impactos, obstáculos, interacción con cofre, monedas, salto/aterrizaje, pausa y arco/flechas visibles.
- `ECONOMY_CLASSES_PASSED: 20`: selección de clases, oro, cofres, compras y combate de área.
- `GAMEPLAY_REPAIRS_PASSED: 26`: pasivas, recarga, disparos, tienda/menús y estados del boss.
- Inspección de capturas de hacha, guadaña y arco en primera persona.

Las utilidades son comprobaciones de Play, no pruebas de un ejecutable standalone. Para repetir la nueva suite, iniciar una partida con el jugador vivo y ejecutar `PlayerPolishChecks.Run()` desde Unity CLI; consultar `PlayerPolishChecks.Result`. Usa una zona temporal de pruebas y dispositivos de entrada simulados. Salir de Play después de ejecutarla.

`PlayerWeaponRepairSetup.Install()` permite regenerar el arco del prefab del jugador y `Resources/PlayerArrow.prefab` fuera de Play. No regenera los mapas ni el resto de las mecánicas.
