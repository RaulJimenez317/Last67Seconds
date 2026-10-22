# Expansión de mazmorras y efectos de combate

Proyecto: `Last67Seconds-Maps`, rama `Leandro_Branches`.

## Recorrido

| Rondas | Escenario |
|---|---|
| 1–5 | Calabozo |
| 6–10 | Coliseo |
| 11–15 | Criptas del Eco (`Criptas`) |
| 16–20 | Bastión de Ceniza (`Bastion`) |
| 21–24 | Regreso al Calabozo |
| 25 | FinalArena, Ork Berserker |

Los jefes normales siguen siendo aleatorios en 5, 10, 15 y 20. Los portales aparecen al terminar 5, 10, 15, 20 y 24. El HUD muestra el nombre de cada zona y el total de 25 rondas. Las pantallas de victoria, reinicio y regreso al menú usan el sistema existente.

## Mapas

Escenas guardadas en `Assets/00_Scenes/Maps/`, incluidas en Build Settings. Ambas reutilizan los modelos de StructuresItemsInMap y las referencias de jugador, mercader, encuentros, jefes y portal del Calabozo. Cada mapa dispone de ocho entradas de enemigos y doce posiciones de botín. El NavMesh está guardado como un asset independiente por escena.

Criptas combina cuatro cámaras, un cruce central y un circuito exterior, con luces azules. Bastión combina un salón central con pilares, galerías laterales de madera y enlaces transversales, con luces cálidas. Los suelos y paredes tienen colliders.

## Efectos

- Sangre del paquete existente `Assets/06_Effects/Vefects/Free Blood VFX` al recibir daño real el jugador, los enemigos y los jefes. Un bloqueo completo, una esquiva o un golpe fallido no generan sangre. El daño parcial bloqueado puede generar sangre por la vida realmente perdida.
- Humo breve de pólvora en la boca de la fusca/mosquete, junto al destello existente.
- Proyectil visible y explosión al impactar con el báculo. Son cambios visuales; no añaden daño de área ni modifican el balance del arma.
- Se conservan los impactos, pasos, sonidos y explosiones existentes. La música de mazmorra se aplica también a las nuevas escenas.
- Los prefabs derivados están en `Assets/06_Effects/Combat`. No se descargaron recursos externos.
- Los efectos de impacto tienen vida limitada y comparten el límite de 40 instancias activas.

## Validación

`DungeonExpansionChecks` ejecuta una partida de 25 rondas con eliminaciones automáticas para verificar progresión, mercader, portales, jefes, victoria, reinicio y vuelta al menú. No sustituye una partida manual para evaluar dificultad o rendimiento durante combate prolongado. El informe local se escribe en `Library/ExpansionChecks.txt`.

Las comprobaciones de construcción verifican suelo y rutas de navegación completas hacia las entradas, el botín, el mercader, el jefe y el portal. Las capturas de revisión quedan en `Library/CriptasOverview.png`, `Library/BastionOverview.png` y `Library/CombatEffectsReview.png`.

Resultado: las 25 rondas, transiciones, victoria, reinicio, vuelta al menú y limpieza de VFX pasaron la prueba automatizada.

## Mago, maná y trucos

- Nueva clase Mago: empieza con báculo, 100 de vida, velocidad 6.5 y 100 de maná; usa el modelo Mage del paquete de personajes existente.
- Cualquier otra clase dispone de 60 de maná al conseguir un báculo. La barra se muestra mientras lleve un báculo en el inventario.
- Disparo: 20 maná. Especial del báculo: 40 maná, además de la carga habitual.
- Regeneración: 8 por segundo después de 1.5 segundos sin gastar maná. Se detiene durante pausa, muerte, resultados, carga de escena o controles bloqueados por una tienda.
- La reserva es del personaje: cambiar de arma o recoger otro báculo no la rellena. Los portales conservan maná y demora de regeneración.
- Chispas amarillas en cada ataque aceptado de espada; destello eléctrico al lanzar magia y explosión al impactar. Se reutiliza Magic Effects FREE y sus materiales convertidos a URP.
- Menú principal → TRUCOS → introducir `Jam2026` → ACTIVAR. Elegir ronda con ANTERIOR/SIGUIENTE; VOLVER → INICIAR JUEGO → elegir clase. El desbloqueo dura la sesión; al abrir de nuevo el juego se vuelve a empezar bloqueado y en ronda 1.
- Comprobaciones de datos y Play Mode: `Assets/Editor/Characters/MageChecks.cs`; informe local `Library/MageChecks.txt`.

## Especiales, armas duales y Shinobi

- Q requiere la carga completa del especial. Espada: gran tajo frontal. Báculo: rayo sostenido que consume 40 de maná. Martillo y hacha: golpe al suelo en un radio de 7 metros. Daga: doce ataques rápidos. Guadaña: barrido circular de 7 metros. Mosquete: ocho disparos. Arco: tres ráfagas de cinco flechas.
- Los ataques frontales respetan los obstáculos. El especial impide atacar normalmente mientras se ejecuta; cambiar de arma o sufrir un derribo lo interrumpe.
- Duales: llevar dos espadas o dos dagas, una en cada espacio. Aparece el arma de la mano izquierda, con 30% más velocidad y 25% más daño por golpe. Las katanas no activan este modo.
- Shinobi: clase inicial adicional con 120 de vida, katana y shuriken. Cambiar al arma secundaria y usar ataque normal lanza shuriken. Q con katana ejecuta cinco tajos rojos hacia delante.
- Clic derecho del Shinobi: parry frontal perfecto durante los primeros 0.2 segundos; evita todo el daño y no consume postura. Después reduce el daño un 60% y consume postura. Los ataques por la espalda no quedan bloqueados. La postura rota deja al personaje vulnerable; se recupera al soltar la guardia tras una demora.
- La barra del HUD muestra postura, parry perfecto y guardia rota. El caballero da prioridad a la animación de escudo al activarlo, también mientras camina.
- Validación en Play Mode: `AdvancedCombatChecks` verifica las cinco clases, equipamiento inicial, parry, daño reducido, rotura de postura, proyectil de shuriken, daño real de los siete especiales, duales e interrupción al cambiar de arma. Informe: `Library/AdvancedCombatChecks.txt`. El ajuste de dificultad y la sensación de combate requieren además jugar manualmente.

## Modelos por tier y animaciones de armas

El catálogo `Assets/Resources/WeaponVisualCatalog.asset` contiene 40 variantes (10 tipos × 4 tiers), además de katana y shuriken. Se aplica al equipar, mejorar, cambiar de espacio y soltar un arma. Las duales usan el tier real de cada mano. Los prefabs derivados están en `Assets/Resources/WeaponTiers`; los packs originales conservan sus materiales.

| Arma | Tier 1 | Tier 2 | Tier 3 | Tier 4 |
|---|---|---|---|---|
| Espada | Nappin ShortSword | Blink Sword1 | Blink Sword3 | Blink Sword5 |
| Hacha | KayKit axe_A | Blink Basic1 | Blink Basic2 | Blink Evolving3 |
| Arco | KayKit bow_A | Nappin Bow | PolyRonin Yumi | KayKit bow_B |
| Daga | Nappin Dagger | PolyRonin Kunai | PolyRonin Tanto | Blink Dagger4 |
| Martillo | KayKit hammer_A | KayKit hammer_B | KayKit hammer_C | Blink Hammer1 |
| Lanza | KayKit spear_A | PolyRonin Yari | PolyRonin Naginata | Blink Polearm2 |
| Guadaña | Blink Scythe1 | Variante con runa cian | Variante con runas violetas | Variante con runas doradas |
| Escudo | Blink Shield3 | KayKit shield_A | Blink Shield2 | KayKit shield_C |
| Báculo | KayKit staff_A | Blink Staff2 | Blink Staff4 | Blink Staff5 |
| Fusca | Nappin HuntingRifle | Nappin Rifle | Blink Musket1 | Musket1 con adornos dorados |

La guadaña y las dos últimas fuscas reutilizan su malla base con adornos propios porque no hay cuatro modelos adecuados de esos tipos en los packs revisados. La katana usa PolyRonin y el shuriken usa Nappin. Los materiales derivados de armas son URP/Lit, con referencias a las texturas originales.

El lanzamiento usa el clip Throw de KayKit Medium, libera el proyectil a los 0.12 segundos y oculta temporalmente el shuriken de la mano. Cambiar de arma, morir o perder el control antes de liberar cancela el proyectil pendiente. El báculo usa Spellcasting y Spellcasting_Long; la katana alterna Slice_Diagonal y Melee_2H_Attack_Slice, con preparación antes de cada contacto de la ultimate.

El láser usa VolumetricLineBehavior y su shader SingleLine-LightSaber del paquete VolumetricLines. Hay una sola instancia durante la canalización, que sigue la punta del báculo y termina en la geometría del escenario. Se elimina al finalizar, morir o cancelar el especial. Su daño y coste de maná permanecen en el sistema de combate existente.

Reconstrucción: menú `Codice/Presentation/Build weapon tiers`. Revisión de modelos: `Library/WeaponTierReview.png`. Las comprobaciones ampliadas de `AdvancedCombatChecks` incluyen catálogo, pickups, estados del animador y láser.
`AdvancedCombatChecks` finalizó con ADVANCED_COMBAT_PASSED después de verificar los 40 modelos equipados y sus pickups, los seis estados nuevos y una única instancia del láser con shader compatible. Se revisaron las capturas del catálogo y del láser en partida.

## Ajustes de postura y munición

- Postura/parry debajo de la mira, centrado para seguir el combate.
- Los cuatro tiers de fusca usan exclusivamente el mosquete original Blink Musket1_2_1. HuntingRifle y Rifle dejaron de estar asignados; no se borraron assets originales del usuario.
- Shuriken: capacidad 30 y 30 al empezar. Arco: 24–36 flechas según tier. Cada ataque normal consume una unidad; ninguna de estas municiones se regenera esperando. Los especiales mantienen su coste de carga de Q.
- Paquetes de 8 flechas y 8 shuriken distribuidos en los puntos de botín de cada ronda, además de drops de enemigos. Se recogen por contacto o E; reponen el arma compatible incluso en el espacio secundario y no desaparecen si está llena. El HUD indica cantidad y dirige hacia suministros compatibles.
- Las asignaciones anteriores de fuscas en la tabla quedan reemplazadas por este ajuste.

## Iconos, desvío y enemigos KayKit

- Catálogo de 42 iconos renderizados desde los modelos reales: 40 tiers, katana y shuriken. La barra rápida usa el arma concreta; shuriken ya no comparte el icono del arco. La tienda usa el icono del tipo ofrecido.
- Shinobi: la postura se oculta en reposo, incluso si mantiene la guardia sin recibir impactos. Un bloqueo/desvío la muestra durante 2.5 segundos. Dos mitades doradas crecen desde el centro según la postura perdida; flash blanco y texto de desvío perfecto, rojo al romperse. La ventana mecánica del parry no cambia.
- Clips dedicados KayKit: Melee_Block para guardia, Melee_Block_Attack para desvío perfecto y Melee_Block_Hit para bloqueo tardío.
- Desde ronda 2, uno de cada tres puestos del encuentro puede usar una variante del mismo rol: Saqueador de la cripta (Rogue_Hooded, rápido y frágil), Cazador renegado (Ranger, proyectiles), Hechicero del abismo (Mage, resistente pero lento) y Guardián bárbaro (Barbarian, maza y escudo). Conservan navegación, recompensas, muerte y conteo de rondas existentes; no cambian el número de enemigos.
- Assets derivados en Resources/EnemyVariants y Resources/WeaponIcons. Constructor: Assets/Editor/Presentation/DeflectEnemySetup.cs.
Validación final: ADVANCED_COMBAT_PASSED, incluidos 42 iconos válidos, icono shuriken distinto del arco, visibilidad de postura por contacto y las cuatro variantes inicializadas y eliminables. El balance de dificultad requiere probar las rondas manualmente.

## Corrección de presentación del arco y mosquete

- Mosquete: su eje de cañón (+Y del catálogo) se orienta hacia delante de la cámara, evitando que la rotación importada de la mano lo atraviese horizontalmente por la pantalla. Posición de primera persona y retroceso breve al disparar.
- Arco: movimiento de suelta/recuperación y leve balanceo; la flecha visible desaparece al disparar, reaparece en la segunda mitad del ciclo y retrocede al preparar el siguiente disparo. Su posición sigue el arco. No se cambian daño, munición ni cadencia.
- Las comprobaciones cubren los cuatro tiers: dirección del mosquete, movimiento del arco, desaparición de flecha y colocación de la siguiente.

## Duales y progresión de enemigos por ronda

- Reposo dual separado del estado de arma a dos manos. Mangos vinculados a ambas manos y hojas orientadas hacia arriba y hacia afuera; ataques DualSlice y DualStab del paquete KayKit. Se elimina la oscilación artificial de la mano secundaria.
- Enemigos normales y las cuatro variantes: para r = ronda − 1 (rondas 1–25), vida × (1 + 0.095r + 0.0025r²), daño × (1 + 0.025r) y velocidad × (1 + 0.005r). Se conservan los multiplicadores de cada variante. La velocidad crece como máximo un 12%; se conservan las ventanas de ataque. Los jefes mantienen sus reglas existentes.
- Ejemplo espadachín: ronda 1 = 65 HP / 12 daño; ronda 11 = 143 HP / 15 daño; ronda 24 = 293 HP / 18.9 daño. Los tiers altos reducen el número de golpes necesarios frente a mantener un arma inicial.
- No se encontraron modelos Golem/Necromancer en los assets locales; no se añadieron modelos supuestos a partir de las imágenes de promoción.
- Validación específica: Library/DualScalingChecks.txt → DUAL_SCALING_PASSED. Comprueba reposo, separación, orientación y ataques de espadas/dagas, y crecimiento de vida/daño de los ocho perfiles en las 25 rondas. Capturas: Library/DualSwordReview.png y Library/DualDaggerReview.png. La prueba general previa se detuvo en Bow damages target; no se considera una aprobación completa de todo el combate.

## Hechizos y criaturas añadidas — 22/10/2026

- Con el bastón equipado: 1 Arcano (20 maná), 2 Hielo (25, impacto en radio de 1,8 m y aturdimiento de 1,5 s; jefes 0,3 s), 3 Rayo (30, daño directo x1,8), 4 Cura (35, restaura 30% de vida máxima y espera 8 s). Clic izquierdo lanza; Tab/rueda cambia arma; Q conserva el especial. La elección se conserva al pasar de mapa. También pueden acceder otras clases al equipar bastón, conservando la ventaja de maná del mago.
- La cura no consume maná a vida completa. Los tiempos individuales evitan saltarse la espera cambiando de hechizo. La geometría sólida bloquea los impactos de hielo/rayo. El mercader conserva sus números de compra.
- Efectos derivados en Resources/SpellEffects: Snow hit, Electro hit y Healing circle del pack mágico; VFX_Classic_01 del nuevo WALLCOEUR para impacto del rayo. Materiales URP derivados, sin modificar los originales. Círculo de cura reducido para no tapar la vista.
- Monster Duo: Slime desde ronda 3; TurtleShell desde ronda 6. Usan modelos Polyart, clips originales, colisiones y escalado de vida/daño por ronda. TurtleShell usa la protección frontal del arquetipo pesado.
- Dungeon Skeleton: Guardián de la cripta como minijefe en rondas 4/9/14/19/24, reemplazando el último enemigo contado de la oleada. Mayor vida, alcance de 3,1 m, anticipación de 1,2 s y triple recompensa de oro. No sustituye los jefes de cada bloque. Anuncio y rótulo propios. El demo solo contiene idle/walk/attack; su cadáver usa caída visual porque no incluye muerte.
- El paquete DoubleL es Humanoid y Ork Berserker es Generic. Se conservaron sus clips nativos, verificando que Melee/Shoot/Slam están asignados y tienen curvas de animación. No se asignaron clips incompatibles ni se afirma un retarget del nuevo paquete.
- Generación reproducible: SpellMonsterSetup (petición Library/SpellMonsterSetup.request). Pruebas en Play: SpellMonsterChecks (petición Library/SpellMonsterChecks.request). Reportes locales Library/SpellMonsterSetup.txt y Library/SpellMonsterChecks.txt. Cubren maná, cura/cooldown, selección serializada, hielo/aturdimiento, rayo, paredes, proyectil arcano, controladores/bones de las tres criaturas, materiales, calendario de 25 rondas y finalización de oleada con minijefe. Capturas de revisión Library/Review*.png.
Resultado: SETUP_PASSED y SPELL_MONSTER_CHECKS_PASSED. Capturas de Slime, TurtleShell, DungeonSkeleton y HUD revisadas; ajustes finales de contraste y marcador ASCII del selector. Cambios locales en Leandro_Branches.

## Corrección de activación de Q — 22/10/2026
- Q pulsada durante un ataque cuerpo a cuerpo queda pendiente hasta terminar el golpe; mantener clic izquierdo ya no puede adelantarse y perder el especial. Se cancela la petición al cambiar de arma o perder el control de combate.
- El porcentaje se redondea hacia abajo: 99,8% muestra 99%, no un 100% prematuro.
- Los intentos rechazados muestran un aviso; el bastón exige 40 de maná y conserva la carga si no alcanza.
- La detección de especiales lineales usa el volumen de los colliders, incluyendo objetivos que solapan el extremo del alcance, y sigue respetando los muros.
- Verificado en Play con UltimateReliabilityChecks: activación pendiente, consumo único, daño de bastón/espada/martillo/daga/guadaña/mosquete, maná insuficiente, indicador, alcance y obstáculos. Resultado: ULTIMATE_CHECKS_PASSED (Library/UltimateReliabilityChecks.txt).
- Guardado localmente en Leandro_Branches; no publicado al remoto en esta tarea.
