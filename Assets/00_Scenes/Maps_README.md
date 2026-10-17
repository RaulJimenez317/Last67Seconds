# Mapas de Códice de Ceniza

Abrir `Assets/00_Scenes/Maps/Calabozo.unity` o `Coliseo.unity` en Unity 6000.5.6f1. Las escenas están guardadas y no necesitan ejecutar un generador. La cámara de vista general permite inspeccionarlas al pulsar Play; el recorrido en primera persona se integra posteriormente.

## Calabozo

Rotonda central con cuatro accesos, cuatro alas con criptas de botín, tramos de suelo de madera y balcones a 2 m accesibles por rampas. La ampliación ocupa 92 × 92 m: añade un circuito exterior de 12 m de ancho, conexiones laterales de 8 m y cuatro salas (Archivo, Almacén, Cripta Mayor y Galería), con accesos abiertos y elementos de orientación por color. Permite circular entre las alas sin cruzar la rotonda. Hay 445 módulos de suelo de 4 × 4 m. Suelo a Y=0 y límites físicos de piedra. La cubierta queda abierta para facilitar la edición y la inspección del nivel. Los modelos y la textura proceden de `StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE`.

## Coliseo

Arena circular de 40 m de diámetro, cuatro niveles de gradas, arcada exterior, cuatro pilares físicos de cobertura y tres pedestales de suministros en los bordes. La entrada es un punto de llegada dentro de la arena cerrada, coherente con el traslado por portal previsto en el GDD.

## Integración posterior

Los objetos vacíos del grupo `Integration markers (no behaviour)` indican entrada del jugador, mercader, portales, enemigos, botín, balcones, jefe y suministros según la escena. Son referencias espaciales sin scripts, spawns ni interacciones. Los pilares son geometría estática: su destrucción y la regeneración de suministros corresponden a la fase de mecánicas. No se añadieron jugador, mercader, enemigos, jefes, UI, música ni efectos.

Cada escena incluye iluminación arquitectónica y cámara de inspección. Al integrar al jugador, sustituir o desactivar esa cámara. Las escenas no modifican `Game.unity` ni la lista de escenas de compilación del proyecto.

## Regeneración y comprobación

El menú `Codice > Maps > Rebuild both maps (overwrites map scenes)` regenera ambas escenas y comprueba materiales, apoyos y rutas de navegación. Sobrescribe las ediciones manuales de estas dos escenas; guardar esos cambios antes en control de versiones. Para regenerar únicamente el calabozo, usar `Codice > Maps > Rebuild dungeon only (overwrites Calabozo)` o `-executeMethod CodiceMapBuilder.BuildDungeon`. La herramienta solo se compila en el editor.

Comando para regenerar y validar con el editor cerrado (adaptar la ruta del ejecutable):

```powershell
& 'D:/MRD/Editor/Unity.exe' -batchmode -projectPath 'D:/67/Last67Seconds-Maps' -executeMethod CodiceMapBuilder.Build -quit -logFile 'D:/67/maps-build.log'
```

Las capturas de revisión se generan en `MapReview/`, fuera de los recursos del juego. La validación de rutas crea una malla temporal y la retira: no introduce comportamiento ni datos de IA en las escenas.

Validación realizada en Unity 6000.5.6f1: 26 recorridos completos en el calabozo ampliado y otros 20 con la rotonda temporalmente bloqueada durante la prueba (el bloqueo no se guarda). El coliseo conserva su validación anterior de 2 recorridos para un agente de 1,80 m de altura y 0,80 m de ancho; 31 marcadores con superficie de apoyo en el calabozo ampliado. Vistas generales y de entrada revisadas. Esta prueba geométrica no sustituye las pruebas posteriores con el controlador definitivo del jugador.
