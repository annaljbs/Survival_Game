# Level Design: Anleitung zum Selbermachen

Für: Boden, Pfade, Wasser, Büsche, Bäume (Größen und Holzarten) und das Felsplateau (Klettern, Höhle).
Alles hier läuft in Unity 6 mit `Assets/Scenes/Level.unity`. Ergänzt `docs/LEVEL_EDITING.md` (Grundlagen) und `docs/PROJECT_GUIDE.md`.

**Was ich (Claude) nicht prüfen konnte:** Ich kann Unity nicht bedienen. Die Befehle unter *Survival > Ground* sind kompiliert und die Tile-Zuordnung ist mit einer Testzeichnung geprüft, aber noch nicht in deinem Unity gelaufen. Wenn etwas komisch aussieht: Cmd+Z, und schick mir einen Screenshot.

---

## 0. Die wichtigsten Grundlagen

### Die Ebenen des Levels (von hinten nach vorn)
| Sorting Order | Was | Objekt in der Hierarchy |
|---|---|---|
| -50 | Wiese (Grundfarbe) | `Ground piece` (36 Stück, unter `Ground`) |
| -49 | Grasbüschel | Tilemap `Meadow` (erst nach dem Ground-Befehl) |
| -45 / -44 | Plateau-Boden / Pfade | `Plateau ground` / Tilemap `Paths` |
| -43 | Deko (Blumen, Büsche, Steine), läuft man drüber | Tilemap `Details` |
| -20 | Wasser | Tilemap `Water` (neu) oder die alten blauen Rechtecke |
| 1 | Bäume (blockieren den Weg) | Tilemap `Trees` |
| 5 | Objekte zum Einsammeln, Lager | Prefabs aus `Assets/Prefabs/Objects` |
| 10 | Spieler | `Player` |

Etwas, das **nur schön aussieht**, gehört auf `Details` oder `Paths`. Etwas, das **den Weg versperrt**, gehört auf `Trees` (nur dieses Tilemap hat einen Collider).

### Tile Palette einrichten (einmalig)
1. **Window > 2D > Tile Palette**.
2. Oben **Create New Palette**, Name zum Beispiel `Gelaende`, speichern in `Assets/Palettes`.
3. Ziehe Tiles aus dem Project-Fenster in das Palette-Fenster:
   - fertige Tiles aus `Assets/Tiles` und `Assets/Tiles/Ground`,
   - oder ein ganzes Sheet aus `Assets/Art/NinjaAdventure/Backgrounds/Tilesets/` (zum Beispiel `TilesetRelief`). Unity fragt nach einem Speicherort für die neuen Tile-Dateien: nimm einen eigenen Ordner wie `Assets/Tiles/Relief`.
4. **Tipp:** Mach pro Thema eine Palette (`Wasser`, `Baeume`, `Felsen`), sonst wird eine Palette riesig.
5. Rechts oben in der Palette steht **Active Tilemap**. Hier wählst du, auf welcher Ebene du malst (`Paths`, `Details`, `Trees`, `Water`, `Meadow`). **Der häufigste Fehler ist, auf der falschen Ebene zu malen.**

### Werkzeuge
| Taste | Werkzeug |
|---|---|
| **B** | Pinsel |
| **U** | Rechteck füllen |
| **I** | Aufnehmen (Tile unter der Maus wählen) |
| **D** | Radierer (oder Shift gedrückt halten mit dem Pinsel) |
| **S** / Auswahl, dann **Entf** | ganzen Bereich löschen |

Shift+Space über dem Scene-Fenster macht es groß. Mehr dazu in `LEVEL_EDITING.md`, Kapitel 7.

### Sicherheit
- Nach jeder gelungenen Änderung: **Cmd+S** und committen.
- Vor einem großen Befehl: committen. Zurück geht es mit **Cmd+Z** oder `git checkout Assets/Scenes/Level.unity`.
- **Niemals** *Rebuild Level From Generator* ausführen. Das überschreibt dein Level.

### Normales Tile oder Rule Tile?
- Ein **normales Tile** zeigt immer dasselbe Bild.
- Ein **Rule Tile** wählt selbst das passende Bild je nach Nachbarn. Du malst nur mit einem Tile, und Kanten, Ecken und Enden passen sich an. `DirtPath_*`, `Water_Pond` und `Meadow_*` in `Assets/Tiles/Ground` sind Rule Tiles.
- Ein Rule Tile kannst du anklicken und im Inspector ansehen und ändern: Liste **Tiling Rules**. Jede Regel hat ein 3x3-Feld. Grüner Pfeil = hier muss ein Tile desselben Typs liegen, rotes X = hier darf keines liegen, leer = egal. Rechts steht das Bild, das dafür gezeigt wird. **Output: Random** heißt, es werden mehrere Bilder zufällig gemischt.

---

## 1. Wiese und Pfad

**Einmal ausführen:** `Survival > Ground > Soft Paths + Olive Meadow` oder `... + Green Meadow`. Die Befehle:
- wandeln alle alten Dirt-Quadrate auf `Paths` in weiche Pfade um,
- setzen die Grundfarbe (`Ground piece`) passend zur Farbwahl,
- erzeugen das Tilemap `Meadow` und streuen zufällige Grasbüschel darüber.

Du kannst sie beliebig oft ausführen, um die Farbe zu wechseln. Die Büschel werden dabei neu gestreut.

### Feinjustieren
- **Neue Pfade malen:** `DirtPath_Olive` / `DirtPath_Green` in die Palette ziehen, **Active Tilemap = Paths**, malen. Radieren mit **D**. Die Ränder passen sich von selbst an.
  - Ein Pfad ist nur Optik. Damit man dort wirklich laufen kann, musst du auf **Trees** die Bäume an der Stelle entfernen.
- **Mehr oder weniger Grasbüschel:** Klicke `Assets/Tiles/Ground/Meadow_Green.asset` an. Im Inspector bei **Tiling Rules** die Regel öffnen. In der Liste der Sprites stehen 14-mal das einfache Gras und 4 Büschel. Mehr einfaches Gras (Eintrag kopieren) heißt weniger Büschel.
- **Büschel von Hand ergänzen oder entfernen:** `Meadow` als Active Tilemap wählen und mit dem Radierer (D) oder dem `Meadow_*`-Tile malen.
- **Boden an einer Stelle anders:** `Details` benutzen. Dort kannst du zum Beispiel ein anderes Bodenstück (Laub, Sand, Steine aus `TilesetFloorDetail`) hinmalen.
- **Andere Grundfarbe:** Alle `Ground piece` markieren (Hierarchy-Suche: `Ground piece`) und beim Sprite Renderer **Color** ändern.
- **Matschige Stelle / Lichtung:** Mit dem `DirtPath`-Tile einen Kreis aus Feldern malen. Er wird automatisch rund.

---

## 2. Wasser

Das Wasser im Spiel besteht aus drei Dingen:

| Objekt | Was es tut |
|---|---|
| `Stream (water)` | Bach, man kann hindurchlaufen (kein Collider) |
| `River (cold section)` + `Terrain: River` | kalter Abschnitt, macht Kleidung nass und kalt (das macht die Zone, nicht das Bild) |
| `Landmark: lake` | der See im Westen, **solid** (Collider) |

Bisher sind alle drei ein blaues Rechteck mit einem Bild. Das Aussehen und die Logik sind getrennt, deshalb kannst du das Aussehen frei ändern, ohne das Spiel zu verändern.

### Variante A: automatisch umstellen
`Survival > Ground > Soft Water From Old Water Rectangles`
- Malt Bach, Fluss und See als weiches Wasser mit Ufer (braun und weißer Schaum) auf das neue Tilemap `Water`.
- Blendet die alten Rechtecke aus (`Sprite Renderer` ausgeschaltet). Collider und Kältezone bleiben, das Spiel verhält sich wie vorher.
- Danach kannst du die Form von Hand ändern (siehe unten).

### Variante B: nur Werkzeug anlegen und alles selbst malen
`Survival > Ground > Create Water Tile + Water Tilemap`
- Legt `Assets/Tiles/Ground/Water_Pond.asset` an und ein leeres Tilemap `Water`.
- Ziehe `Water_Pond` in deine Palette, **Active Tilemap = Water**, und male. Runde Teiche, Bäche, Buchten: Ufer, Ecken und Enden passen sich an.

### Variante C: Wasser aus den Einzelteilen selbst zusammensetzen
Wenn du lieber mit dem ganzen Sheet arbeitest statt mit dem Rule Tile: Der **Bauplan** `docs/img/wasser_bauplan.png` zeigt alle Teile mit ihrer Position (Spalte, Zeile), einen fertig zusammengesetzten Teich, die Innenecken für L-Formen und die Schritte in Unity.
Kurz: Neue Palette `WasserTeile` anlegen, Stift (Edit) einschalten, `Assets/Resources/Pixel/Tilesets/TilesetWater` in die Palette ziehen (Tiles in einen neuen Ordner `Assets/Tiles/WaterSheet` speichern lassen), Stift ausschalten, **Active Tilemap = Water**. In der Palette ein Rechteck von Teilen aufziehen = Stempel, mit dem Pinsel (B) setzen.

### Weg und Fluss bündig machen (ohne grüne Kante)
Das Wasser-Sheet `TilesetWaterBank` ist `TilesetWater`, bei dem der **Rasenrand um das Ufer durchsichtig** ist. Es hat dieselben Positionen wie der Bauplan, nur dass jetzt das Teil darunter sichtbar bleibt: der Weg oder die Wiese.
So wird der Übergang sauber:
1. **Survival > Ground > Prepare Water Sheet for Hand Painting** (schneidet das Sheet).
2. Palette `WasserTeile`: `Assets/Resources/Pixel/Tilesets/TilesetWaterBank` hineinziehen (nicht `TilesetWater`), Active Tilemap = **Water**, Fluss malen.
3. **Survival > Ground > Make Paths Flush With Water**: wo ein Wasserfeld einen Wegfeld berührt, wird der Weg unter dem Wasserfeld weitergeführt. Dadurch verschwindet der grasige Wegrand am Ufer, und der Weg geht direkt in die Uferlinie über. Nach jeder Änderung am Fluss noch einmal ausführen.
4. Zurücksetzen: **Survival > Ground > Reset Water (clear painted water)** löscht das gemalte Wasser (und zeigt auf Wunsch die alten blauen Rechtecke wieder).

### Wichtig beim Wasser
- **Gras-Farbe:** Das Ufer ist für das **olive** Gras gezeichnet. Mit `Olive Meadow` passt es nahtlos. Mit `Green Meadow` sieht man einen olivefarbenen Rand ums Wasser. Entweder Olive wählen oder die Wasserfarbe in einem anderen Satz des `TilesetWater` suchen (der Sand-Satz in der oberen Hälfte ist farblich unabhängig, hat aber einen harten Sandrand).
- **Form:** Runde, weiche Formen sehen am besten aus. Schmale Bäche (1 Feld breit) gehen auch. Spitze Einbuchtungen von nur einem Feld sehen komisch aus.
- **Der Collider des Sees** ist noch das alte Rechteck (`Landmark: lake`, Komponente **Box Collider 2D**). Wenn du den See anders formst, passe die Größe des Colliders an oder lege mehrere kleine Collider an. Alternativ: ein zweites Tilemap `Water solid` mit **Tilemap Collider 2D** (wie `Trees`) für Wasser, durch das man nicht gehen kann.
- **Der Bach muss begehbar bleiben** (hat keinen Collider). Das neue Tilemap `Water` hat auch keinen.
- **Die Kältezone** (`Terrain: River`) ist ein Rechteck mit der Komponente **Terrain Zone**. Wenn du den Fluss verschiebst, verschiebe die Zone mit.
- **Animiertes Wasser:** `Assets/Art/NinjaAdventure/Backgrounds/Animated/Water Ripples` und `Waterfall` enthalten Animationen. Sie sind als Sprite-Sheets da. Dafür müsste ich dir Animation-Clips einrichten. Sag Bescheid, wenn du das willst.

---

## 3. Büsche zufällig verteilen

Es gibt zwei verschiedene Arten von Büschen. Das ist wichtig:

| Art | Was es ist | Gibt Holz? |
|---|---|---|
| **Deko-Büsche** `Deco_Bush_*`, `Deco_Fern_*`, `Deco_Tuft_*`, `Deco_Flower_*` | Tiles auf `Details`. Nur Optik, man läuft darüber | Nein |
| **Green bush** (Prefab in `Assets/Prefabs/Objects`) | ein Objekt mit Interaktion, 7 Stück, gibt 3 Grünholz | Ja |

### Zufällig malen mit Random-Tiles (empfohlen, am einfachsten)
1. Führe einmal **Survival > Ground > Create Random Deco Tiles (bushes, ferns, flowers)** aus.
2. In `Assets/Tiles/Ground` liegen jetzt vier Tiles: `Random_Bushes`, `Random_Ferns`, `Random_Flowers`, `Random_Forest_Floor`.
3. Ziehe sie in deine Palette, **Active Tilemap = Details**, und male mit **B**. Jeder Klick setzt einen anderen Busch (oder ein anderes Farn oder eine andere Blume) aus der Auswahl. Das Bild ändert sich auch nicht, wenn du dasselbe Feld überschreibst, sondern ist an die Position gebunden.
4. **Weniger oft dasselbe:** Klicke das Tile an. In **Tiling Rules** steht die Liste der Sprites. Einträge hinzufügen oder löschen. Ein Bild, das mehrfach in der Liste steht, kommt öfter vor.
5. **Weniger dicht:** Nicht mit gedrückter Maustaste ziehen, sondern einzeln klicken, und Abstand lassen.

Alternativ gibt es den **Random Brush** aus *2D Tilemap Extras*: im Tile-Palette-Fenster unten bei **Brush** ein neues Brush-Asset anlegen und den Typ *Random Brush* wählen. Unter **Random Tile Sets** legst du eine Liste von Tiles an, und beim Malen wird zufällig eines davon gesetzt. Die Random-Tiles oben sind einfacher und gehen auch mit Rechteck füllen (**U**).

### Automatisch: `Survival > Place Bushes at the Forest Edge`
Setzt Büsche in kleinen Gruppen am Waldrand. Jeder neue Lauf gibt eine neue Verteilung. Dichte und Abstand stehen oben in `Assets/Editor/ForestDecorator.cs` (`SeedChance`, `MinSpacing`). Von Hand gemalte Tiles werden nicht angefasst.

### Büsche wieder löschen
Radierer (D) auf `Details`, oder **Survival > Remove Automatic Bushes** (entfernt nur die `Deco_*`-Tiles).

### Neue Deko-Tiles aus dem Sheet
`TilesetNature` im Palette-Fenster öffnen: Zeilen 10 und 11 sind Büsche, Farne, Blumen, Pilze. Die Zeile 8 und 9 enthalten Baumstümpfe und Äste. Ziehe, was dir gefällt, in die Palette.

---

## 4. Bäume: Größen und Holzarten

### Wie das Level Bäume benutzt (zwei getrennte Dinge)
1. **Wald-Bäume** auf dem Tilemap `Trees` (Zellen 2x2 Einheiten = 32x32 Pixel). Sie sind **nur Kulisse und Wand**, geben nichts.
2. **Holz-Objekte** (Prefabs), die der Spieler benutzt:

| Holzart im Spiel | Item | Objekt | Aktuelles Bild |
|---|---|---|---|
| Fichtenzweige (Dach, Bett, Rauchsignal) | Boughs | `Spruce tree` (10 Stück) | Nadelbaum `TilesetNature_r32_0_32x32` |
| Birkenrinde (Zunder) | Tinder | `Birch tree` (2) | weißer Baum `TilesetNature_r192_0_32x32` |
| Grünholz (Gerüststangen, brennt nicht) | Green Wood | `Green bush` (7) | Busch `TilesetNature_0_10` |
| Trockene Äste (Anzündholz) | Kindling | `Dead twigs` (3) | Ast aus `Items/Resource/Branch` |
| Baumstämme (Brennholz) | Logs | `Fallen log` (5) | `tileset_camp_r0_0_32x16` |

### Dein Plan: pro Holzart ein passender Baum
Mein Vorschlag, damit der Spieler **am Aussehen erkennt, wo er was findet** und das ist genau dein Lerneffekt:

| Holzart | Baumtyp zum Wiedererkennen | Wo im Level |
|---|---|---|
| Fichtenzweige | dunkler Nadelbaum (`Tree_Spruce`, groß: `TilesetNature_r0_32_64x48`) | Fichtenwald, zum Beispiel um Lager B |
| Birkenrinde | helle, weiße Laubbäume (`TilesetNature_r192_0_32x32`) | eine helle Birkengruppe |
| Brennholz / Stämme | große Eichen (`Tree_BigOak`) mit Baumstümpfen und Stämmen (`TilesetNature_0_8`, `2_8`) | rund um die Fallen-log-Stellen |
| Anzündholz | kahle braune Bäume (`Tree_Dead`) | im toten Waldstück, wo `Dead twigs` liegen |
| Grünholz | grüne Büsche und kleine runde Bäume | an Bächen und Rändern |

So hat jede Holzart ein eigenes Waldstück. **Maximal 3 bis 4 Baumarten, jeweils in 2 Größen** (nicht zu viele), das wirkt ruhig und lesbar.

### Wald ausdünnen: Tannen entlang der Wege, Rest unsichtbare Wand
**Survival > Forest > Thin Out Forest (invisible wall + fir trees along the paths)**
1. Entfernt Bäume, die auf Wasser stehen.
2. Macht dort, wo jetzt ein Baum steht, eine **unsichtbare Wand** (Tilemap `Forest Wall`). In der Scene-Ansicht siehst du sie als rote Quadrate, im Spiel ist sie unsichtbar. Dort kann man nicht laufen.
3. Ersetzt die vielen verschiedenen Bäume durch **wenige Tannen in vier Größen** (`Fir_S`, `Fir_M`, `Fir_L`, `Fir_XL`: einzelne, immer höhere und breitere Tannen) am Rand der offenen Flächen.
4. Mehr oder weniger Tannen: oben in `Assets/Editor/ForestTools.cs` `KeepChance` (Anzahl) und `SizeWeights` (Anteil der Größen). Der Befehl kann mehrfach laufen und würfelt neu.
Die Wand bleibt, bis du die roten Felder auf `Forest Wall` radierst. Du kannst Bäume (reine Optik, auch auf `Trees` mit Collider) selbst frei setzen.
**Nur Wasser:** **Survival > Forest > Remove Trees On Water** löscht Bäume, die Wasser berühren (gemaltes Wasser und die alten Wasserrechtecke).
Der Spieler (`Player`) muss beim Ausführen auf offenem Boden stehen, weil von dort aus der erreichbare Boden gesucht wird. Rote Felder im Scene-Fenster ausblenden: in der Hierarchy das Augen-Symbol bei `Grid: Forest Wall (cell 2)`.

### Tannen selbst malen: vier Größen
**Survival > Forest > Create Fir Tiles (4 sizes) for Hand Painting** legt die Tiles an: `Fir_S` (die Tanne aus dem Tileset), `Fir_M`, `Fir_L`, `Fir_XL` (jeweils auch `_Mirror`) und `Random_Firs` (ein Tile, das zufällig eine Größe und Spiegelung wählt). Sie liegen in `Assets/Tiles` und `Assets/Tiles/Ground`.
- `Fir_M/L/XL` sind aus der Tanne des Tilesets zusammengebaut: mehr Stufen und breiter, im gleichen Pixelstil.
- Der Fuß jeder Tanne sitzt am unteren Rand der Zelle. Eine große Tanne wächst von dort nach oben.
- Male auf `Trees`. Der Collider ist immer nur die eine Zelle, egal wie groß die Tanne aussieht.
- Der Befehl stellt auch bei `Trees` die Zeichenreihenfolge auf **Top Left**, damit eine weiter unten stehende Tanne vor der dahinter steht.
- Die Quellbilder liegen in `Assets/Resources/Pixel/Custom/Fir_M.png` usw. Du kannst sie in einem Pixelprogramm nachbearbeiten.

### Tannen automatisch über den Wald verteilen
**Survival > Forest > Scatter Fir Trees (sparse / medium / dense)** pflanzt Tannen aller vier Größen über den ganzen Wald (überall dort, wo die unsichtbare Wand `Forest Wall` ist):
- **Am Weg stehen kleine und mittlere Tannen, weiter drinnen die hohen** (bis 9 Zellen tief, mehr sieht die Kamera nie).
- **Wäldchen und Lichtungen** durch einen langsamen Zufallsverlauf, nicht gleichmäßig verteilt.
- **Abstand:** kleine Tannen mit einer freien Zelle dazwischen, große mit mehr.
- **Nicht auf Wasser**, und schon gesetzte Bäume bleiben stehen. Noch einmal ausführen = es kommen mehr dazu. **Cmd+Z** nimmt es zurück.
- Dichte: `sparse` (30 %), `medium` (50 %), `dense` (80 %). Beginne mit sparse.
Voraussetzung: `Forest Wall` existiert (vorher *Thin Out Forest*) und der `Player` steht auf offenem Boden.

### Verschiedene Größen
Die Bäume im Pack gibt es in:
- **klein 32x32:** Zeile 0 und 1 des `TilesetNature`,
- **mittel 48x48:** zum Beispiel `Tree_BigOak` und `Tree_Autumn` (schon in `Assets/Tiles`),
- **groß 64x48:** Zeile 2 bis 4 (die dreiteiligen Bäume, Nadel, rund, weiß) und Zeile 18 bis 20.

**Ein neues Baum-Tile anlegen:**
1. Im Project-Fenster `TilesetNature` (aus `Assets/Resources/Pixel/Tilesets`) aufklappen. Die freien Rechtecke heißen `TilesetNature_r<x>_<y>_<breite>x<höhe>`.
2. Rechtsklick in `Assets/Tiles` > **Create > 2D > Tiles > Tile**, Name zum Beispiel `Tree_BigSpruce`.
3. Beim Tile: **Sprite** auf das gewünschte Bild setzen, **Collider Type = Grid** (sonst blockiert er nicht).
4. Das Tile in die Palette ziehen, **Active Tilemap = Trees**, malen.
5. Wenn du ein Sprite nicht findest: `Sprite Editor` des Sheets öffnen und das Rechteck als eigenes Sprite hinzufügen (Name im Schema oben).

Große Bäume ragen optisch über ihre Nachbarzellen. Das ist in Ordnung (siehe `LEVEL_EDITING.md`, Kapitel 8: der Collider bleibt in der einen Zelle).

### Vorgehen
1. **Survival > Trim Hidden Forest** hat schon viel entfernt. Entscheide, wo welches Waldstück sein soll.
2. Ein Waldstück **leeren** (Auswahl-Werkzeug, Rechteck, Entf, Active Tilemap = Trees) und mit der gewünschten Baumart neu bemalen (Rechteck füllen **U**). Das Mischen mit einem zweiten Tile der gleichen Art (gespiegelt, `_Mirror`) bringt Abwechslung.
3. **Survival > More Tree Variety** nicht benutzen, wenn du die Arten selbst festlegst. Der Befehl mischt die Arten zufällig.
4. Das Holz-Objekt (`Spruce tree` usw.) so stellen, dass es **in** dem passenden Waldstück liegt.

### Bild eines Holz-Objekts ändern
Prefab doppelt anklicken (`Assets/Prefabs/Objects/Spruce tree.prefab`) > **Sprite Renderer > Sprite**. Das ändert alle Fichten auf einmal. Die Größe steht beim Sprite Renderer bei **Size** (Draw Mode **Tiled**) oder bei der Scale.

---

## 5. Felsplateau, Klettern und Höhle

### So ist es gebaut
- `Landmark: hill` bei (70, -20): ein solider Hügel 18x12, bisher mit einem Boden-Tile gefüllt.
- `Climb up` bei (70, -12) am Fuß des Hügels: Aktion mit Kosten (25 Min, -20 Energie). Sie **teleportiert** den Spieler zum Anker `Plateau top`.
- Das Plateau liegt **außerhalb der Karte** bei (170, 0), Größe 30x20: Objekt `Plateau ground` (graue Fläche), vier unsichtbare `Wall`-Objekte als Rand, `Camp C` (ROCK LEDGE), `Climb down` bei (158, -5) mit Ziel `Plateau bottom`.
- Das Klettern ist also nur eine Aktion mit Kosten und Teleport. Es gibt keine eigene Szene.

### Plateau neu gestalten (Optik)
1. `Plateau ground` auswählen und beim **Sprite Renderer** das Häkchen oben bei der Komponente ausschalten (verstecken).
2. Neues Tilemap anlegen: Rechtsklick in der Hierarchy > **2D Object > Tilemap > Rectangular**, Name `Plateau`. Beim **Tilemap Renderer**: **Order in Layer = -45**.
3. Die Position des neuen Grid auf `0, 0, 0` lassen. Gemalt wird bei x = 155 bis 185, y = -10 bis 10.
4. Sheets für die Palette `Felsen`:
   - `TilesetRelief`: Felsplatten mit Klippenrand (graugrün und sandorange, Zeilen 0 bis 2 und 5 bis 7). Oben die helle Fläche, unten die Felswand. Damit baust du eine Felskante.
   - `TilesetFloor` (Satz mit Sand), `TilesetFloorDetail` (Steine, Kiesel), `TilesetNature` Zeile 12 bis 17 (Felsbrocken, Findlinge).
5. Ränder (`Wall`-Objekte) und `Camp C`, `Plateau top`, `Climb down` **nicht verschieben**, oder gemeinsam mit allem. Wenn du das Plateau größer machst, die Walls mit verschieben.
6. Felsen als Hindernisse: auf `Trees` malen oder ein zweites Tilemap mit Collider.

### Hügel / Aufstieg gestalten
- Den Hügel (`Landmark: hill`) mit `TilesetRelief` aussehen lassen: Sprite Renderer ausschalten, mit Relief-Tiles auf einem eigenen Tilemap (Order -10, mit **Tilemap Collider 2D**) nachbauen. Der alte Collider darf bleiben.
- Einen Aufstieg darstellen: Felsstufen aus `TilesetRelief` am Hügelfuß direkt vor `Climb up`.

### Eine Höhle
- Der Pack hat `TilesetHole` (Löcher und Höhleneingänge in Dunkelgrau mit Rand) und `TilesetDungeon` / `TilesetHouse` für Innenräume.
- **Als Optik:** Einen Höhleneingang (dunkle Fläche aus `TilesetHole`) an den Hügelfuß malen. Dort liegt `Climb up`. Den Text der Aktion in der Komponente **Interactable** ändern (zum Beispiel "Climb up to the cave ledge"). Das **Ziel** bleibt das Plateau.
- **Als echte Höhle** (eigener Raum, anderer Zustand, zum Beispiel windgeschützt): Das Plateau selbst ist schon so ein Nebenlevel. Du kannst es als "Höhle" gestalten: dunkler Boden (`TilesetHole`-Fläche), Felswände rundherum (`TilesetRelief`), ein Lagerplatz in der Mitte. Der Wärmefaktor des Orts ist an der Komponente **Shelter Zone** von `Zone: Camp C` (Site Factor 0.7 = guter Schutz).
- Wenn du eine **echte** Höhle mit eigenem Gameplay willst (zum Beispiel dunkel ohne Licht), sag mir, was darin passieren soll, dann baue ich es dir.

### Test
Es gibt keine Debug-Taste fürs Plateau. Stelle im Editor den Spieler im Scene-Fenster auf das Plateau (Player auswählen, im Inspector **Position** auf 160, -5 setzen) und drücke Play.

---

## 6. Dein Ablauf, wenn du loslegst
1. Committen.
2. `Survival > Ground > Soft Paths + Olive Meadow` (oder Green).
3. `Survival > Ground > Soft Water From Old Water Rectangles`.
4. `Survival > Ground > Create Random Deco Tiles`, Palette `Gelaende` anlegen, die Details-Ebene mit den `Random_*`-Tiles bestreuen (Büsche, Blumen).
5. Waldstücke nach Holzart bemalen.
6. Plateau und Hügel gestalten.
7. Alles durchspielen (Debug-Tasten 1, 6, 4 und die Route) und committen.
