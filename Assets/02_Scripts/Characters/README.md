# Punto 2: jugador y mercader

Integración en el proyecto existente, rama `Anthony_Branches`, sobre los mapas de `dev`.
Abrir `Assets/00_Scenes/Maps/Calabozo.unity` o `Coliseo.unity` y pulsar Play.
Las escenas tienen el prefab Player en `Player_Entry`; Calabozo también contiene Merchant en `Merchant_Center`.
La cámara panorámica queda desactivada. No se regeneró la arquitectura.
Calabozo es la primera escena de compilación, seguida de Coliseo; Game se conserva en la lista.

## Jugador

Antes de Play, seleccionar `Characters (Phase 2)/Player` y elegir `Initial Class` en PlayerCharacter:
Warrior (120 HP, 6.5 m/s, espada), Tank (180 HP, 4.8 m/s, hacha) o Archer (100 HP, 6.2 m/s, arco).
Los cuerpos usan Knight, Barbarian y Ranger de Characters y solo proyectan sombras para no tapar la cámara.
El mercader usa Mage de la misma carpeta. Hay materiales URP propios sin modificar los originales.

- WASD y ratón: movimiento y vista. Clic en Game captura el cursor; Escape lo libera.
- Espacio o Shift izquierdo: dash de 0.3 s invulnerable, enfriamiento 1.5 s.
- Clic izquierdo: ataque básico. Espada con tercer corte de mayor alcance; hacha con arco de 180 grados y contrato de ruptura de guardia/empuje; arco con proyectil y colisión barrida.
- 1, 2, Tab o rueda: cambiar entre dos ranuras. Arco: ocho flechas, regenera una cada 1.5 s.
- E mirando a un objeto a menos de 3 m: recoger/reemplazar arma o abrir el mercader.
- La muerte bloquea las acciones. Reiniciar Play inicia otra sesión; no se añadió menú de reinicio.

`IDamageReceiver.ReceiveDamage(DamageInfo)` es el contrato para los futuros enemigos.
El ataque nunca añade enemigos ni IA. `CharacterState.AddGold` permite conectar el botín futuro.
`CharacterState.AddAmmunition` repone la munición del arma activa hasta su capacidad.
Las armas de la tienda tienen ataques básicos genéricos y modelos existentes; sus ultimates y efectos específicos pertenecen al punto 5.

## Mercader sin desarrollar el punto 7 (UI)

Empieza oculto. El futuro controlador de oleadas debe llamar a
`ReportRoundProgress(round, defeated, total)` o `BeginIntermission(round)`.
Con cinco enemigos aparece a la tercera baja (primer entero que alcanza el 50%).
Los eventos `onIntermissionStarted` y `onRoundContinued` permiten pausar/reanudar el spawn externo.
Este componente no implementa oleadas ni pausa global del combate.

Para probarlo ahora, en Play:

1. Menú contextual del componente Merchant: `Prueba / Abrir interludio (solo Play)`.
2. Menú contextual de PlayerCharacter: `Prueba / Dar 500 oro (solo Play)`.
3. Acercarse al centro, mirar al mercader y pulsar E. La consola muestra el catálogo y precios.
4. Con la tienda abierta, 1/2 compran armas, 3 compra pasiva y 4 compra curación.
5. E/Escape cierran la conversación y permiten volver a abrirla; Enter continúa la ronda y oculta al mercader.

Hay dos armas distintas T2–T4, una pasiva (150 oro) y una curación del 50% de vida máxima (80 oro).
Precios de armas provisionales y editables: T2=120, T3=220, T4=320; el GDD no los fija.
Cada oferta se compra una sola vez por interludio. No se cobra por curación con vida llena,
sin fondos, fuera de rango o por una cuarta pasiva sin elección de reemplazo.
`TryPurchase(player, offerIndex, replacePassiveSlot)` permite a la futura UI elegir ranura 0–2.
Las pasivas se almacenan, pero sus cinco efectos y la habilidad Q/clic derecho se dejan para el punto 5.
`SpecialRequested`, `ShopOpened`, `StockChanged`, `CharacterState.Changed` y `Died` son puntos de conexión.
No hay oro de prueba ni apertura automática del mercader en builds.

## Validación y mantenimiento

`Codice > Characters > Validate character transactions` ejecuta comprobaciones de datos.
En Play, `CharacterPlayChecks.Run()` ejecuta comprobaciones de compra, interludio, colisión,
dash y muerte con fixtures temporales (invocable mediante Unity CLI eval).

`Codice > Characters > Integrate player and merchant into existing maps` reconstruye los prefabs
y añade el grupo de personajes solo si falta. No modifica la geometría. Guardar primero las
ediciones manuales de las escenas. Reconstruir reemplaza las personalizaciones de los prefabs generados.
Si se regeneran mapas con CodiceMapBuilder, volver a ejecutar la integración de personajes.

No se implementaron enemigos, jefes, oleadas, efectos de pasivas/ultimates, HUD, música ni VFX.
El paquete Pipeline se usa para validar en el editor y no es una dependencia de los scripts de juego.
