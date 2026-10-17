# Tienda, oro, cofres y clases

Implementado en Anthony_Branches dentro del proyecto existente.

## Cómo jugar

- Menú → Iniciar juego → elegir Caballero, Arquero o Tanque.
- Caballero: 120 vida, velocidad 6.5; espada con clic derecho. Mantener clic izquierdo con la espada levanta el escudo, reduce 50% del daño frontal y bloquea ataques simultáneos. El daño posterior no se reduce.
- Arquero: 100 vida, velocidad 8; arco con clic izquierdo. Consume una flecha por disparo y conserva la regeneración de flechas existente.
- Tanque: 180 vida, velocidad 4.8; comienza con mazo. Clic izquierdo golpea a varios enemigos en el área frontal, respetando paredes.
- Q activa el especial; T/Y/U mantienen la activación de pasivas recogidas. 1/2/Tab cambia armas.
- En el descanso de mitad de ronda aparece el mercader. Acércate, mira al personaje y pulsa E. Compra con botones o 1–4; Enter continúa la ronda. No cobra compras sin saldo ni artículos vendidos.
- Enemigos normales sueltan 40/50/60/80 monedas según tipo. Los jefes conservan su recompensa configurada (300). Las monedas se recogen al tocarlas o acercarse; al finalizar la ronda se acredita el oro pendiente, evitando perder la recompensa del jefe en la pantalla de victoria. El bono de rapidez sigue siendo 100.
- Los cinco cofres decorativos del Calabozo ahora cobran 60 monedas y entregan un arma aleatoria. E abre el cofre; luego E recoge el arma. Cada cofre se abre una sola vez por carga de escena. La calidad aumenta con el nivel (T1–T4).

## Implementación

- `RunNavigation.SelectedClass` transmite la elección al `Awake` del jugador. La captura/restauración existente conserva la clase durante viajes y reinicios de nivel.
- `GoldPickup` sustituye los premios inmediatos de enemigos por recogibles con protección contra cobro duplicado. Usa `Resources/GoldCoin.prefab`, generado desde el modelo Coin de Power Ups mediante `EconomySetup.Install`.
- `TreasureChest.InstallInScene` conecta únicamente las raíces chest_gold del mapa, evitando duplicar cuerpo y tapa. Reutiliza el modelo y anima su tapa; incorpora collider de interacción, precio y entrega mediante PlayerCombat.CreatePickup.
- La tienda conserva el inventario y las transacciones existentes. Se añadió indicación contextual con E y se ajustó la economía para que haya compras posibles en las primeras rondas.
- El HUD muestra precio/saldo de cofres y el control Q del especial. El menú presenta tres tarjetas con estadísticas y controles, navegables con EventSystem.

## Validación realizada en Unity 6000.5.6f1

- EconomyClassChecks: 20 comprobaciones aprobadas (selección desde botones, munición, oro, cofres, raycast E del mercader, compras y mazo contra varios objetivos).
- GameplayRepairChecks: 26 comprobaciones aprobadas (pasivas, disparos, recarga, pausa, menús y jefe).
- Escudo con entrada de ratón simulada: 100 daño frontal → 50; posterior → 100; atacar mientras bloquea devuelve false.
- Jefe derrotado: saldo 400, correspondiente a 300 de recompensa y 100 de bono.
- Pantalla de clases revisada visualmente en `Temp/class-selection.png`.

Publicación solicitada por el usuario en Anthony_Branches y dev.
