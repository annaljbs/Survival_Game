# Spielkonzept: Ohne Empfang

## Kurzbeschreibung

Ein kurzes, lehrreiches 2D-Pixel-Survival-Spiel in Top-down-Perspektive. Nach einem Flugzeugabsturz strandet die Hauptfigur in einer abgelegenen Wildnis. Ohne Netz, Internet oder brauchbares Handy muss sie Entscheidungen treffen, Ressourcen einteilen und einen sicheren Ort erreichen.

Das Spiel soll in etwa **15–20 Minuten** durchspielbar sein. Es soll spannend und anspruchsvoll wirken, dabei aber mit wenigen, verständlichen Spielsystemen auskommen und für eine einzelne Person in Unity realistisch umsetzbar bleiben.

## Rahmen (Kurs „Games with a Purpose“)

| Punkt | Festlegung |
|---|---|
| Genre | 2D-Top-down-Survival mit Entscheidungs- und Ressourcenmanagement |
| Plattform | PC (Windows-Build; Unity 6000.3, URP 2D) |
| Zielgruppe | Studierende und junge Erwachsene (ca. 16–25), die Smartphone und Internet gewohnt sind und wenig Outdoor-Erfahrung haben |
| Spielzeit | 15–20 Minuten pro Durchlauf, schneller Neustart |
| Sprache im Spiel | Englisch (Konzept und Dokumentation bleiben Deutsch/Englisch gemischt) |
| Termine | Playtest 18.12. (60–70 % fertig), Abgabe 28.01.2027 |

## Lernziele (messbar)

Nach einem Durchlauf sollen Spielende erkennen können:

1. **Handy ist keine Lösung:** Ohne Netz zählen Beobachtung und Planung, nicht wiederholtes „Neu laden“.
2. **Wasser zuerst und aufbereiten:** Eine nahe Wasserstelle ist nicht automatisch sicher.
3. **Schutz vor Nacht und Nässe:** Schlafplatz und Wärme vor Einbruch der Nacht, bei Nässe zuerst den Schutz verbessern.
4. **Kräfte einteilen:** Jede Handlung kostet Zeit und Energie; Umwege und Abkürzungen haben Preise.
5. **Auffindbar bleiben:** Sichtbares Signal an einem offenen Ort statt ziellosem Weitergehen. Unbekannte Pflanzen nicht essen.

Gemessen wird das mit einem kurzen **Vorher/Nachher-Quiz** (4–5 Fragen) und einem Entscheidungs-Log (siehe „Evaluation“).

## Triadisches Design (Reality – Meaning – Play)

| Komponente | Kriterien | Umsetzung im Spiel |
|---|---|---|
| **Reality** | Realismus, Fidelity, Validität | Vereinfachtes, aber konsistentes Modell: Durst steigt schneller als Hunger, Nässe und Nacht senken Wärme, Wasser aus der Natur braucht Aufbereitung. Inhalte stützen sich auf veröffentlichte Quellen (siehe „Quellen“). |
| **Meaning** | Reflexion, Transfer, Relevanz | Game-Over-Rückblick erklärt, *warum* etwas schiefging; Lernziele sind auf Alltagssituationen übertragbar (Planung, Prioritäten, Ruhe bewahren). |
| **Play** | Engagement, Spaß, Herausforderung | Zeit/Energie als begrenztes Budget, sichtbare Kostenvorschau, humorvolle Gedankenblasen, klare Warnungen, kurze Durchläufe. |

Zusätzlich: **Control / Constraints / Consequences** – Kontrolle = eine Interaktionstaste, Einschränkung = Zeit und Energie, Konsequenz = Kostenvorschau und Rückblick.

## Spielerlebnis

Die Spielenden lernen durch Entscheidungen und Folgen, worauf es in einer Notsituation ankommt: Ruhe bewahren, die Lage einschätzen, Wasser und Schutz priorisieren, Kräfte einteilen und rechtzeitig Hilfe oder einen sicheren Weg suchen. Das Spiel vermittelt Grundlagen, ersetzt aber keinen echten Erste-Hilfe- oder Outdoor-Kurs.

Der Humor entsteht vor allem aus den Reaktionen der Figur auf den fehlenden Handyempfang. Die Survival-Situation selbst bleibt ernst genug, damit Entscheidungen Gewicht haben.

## Einstieg

Das Spiel beginnt unmittelbar nach einem Flugzeugabsturz. Die Figur kommt zu sich, sieht Trümmer und hört ein paar kurze Gedankenblasen:

- „Krass … wo bin ich? Was ist passiert?“
- „Wo ist mein Handy?“
- „Akku fast leer. Kein Netz. Kein Internet.“
- „Okay. Nicht panisch werden. …Ich werde sterben.“

Die Figur sucht kurz nach dem Handy. Es lässt sich noch einschalten, hilft aber kaum: kein Empfang und nur wenige Prozent Akku. Danach beginnt das eigentliche Spiel. Das Handy wird nicht zur Lösung; die Spielenden müssen die Umgebung untersuchen und selbst planen.

## Kern-Spielschleife

1. **Umgebung prüfen:** Hinweise, Material, mögliche Wasserstellen, Wege und Gefahren entdecken.
2. **Aufgabe wählen:** Entscheiden, was gerade am dringendsten ist.
3. **Kosten abwägen:** Jede Handlung verbraucht Zeit und Energie; manche Handlungen beeinflussen Hunger, Durst oder Wärme.
4. **Handeln und Ergebnis beobachten:** Eine gute Vorbereitung spart später Ressourcen. Fehlversuche kosten Zeit und Kraft, liefern aber verständliche Hinweise.
5. **Plan anpassen:** Vor Einbruch der Nacht Schutz und Wärme organisieren und am nächsten Tag den Weg zum Ziel fortsetzen.

Die zentrale Frage lautet: **„Was ist jetzt die klügste Verwendung meiner begrenzten Kraft?“** Vor wichtigen Aktionen sieht man ihre geschätzten Kosten und möglichen Auswirkungen.

## Ressourcen und Zustände

Für den ersten Prototyp genügen vier einfache Anzeigen:

| Anzeige | Funktion im Spiel |
|---|---|
| **Energie** | Sinkt durch Gehen, Sammeln, Bauen und andere anstrengende Aktionen. Bei sehr niedrigem Stand werden Aktionen schwieriger oder langsamer. |
| **Durst** | Steigt mit der Zeit und stärker bei anstrengenden Aktionen. Sicheres Wasser ist eine hohe Priorität. |
| **Hunger** | Steigt langsamer als Durst. Essen hilft, Energie wieder aufzubauen oder den Energieverlust zu bremsen. |
| **Wärme** | Sinkt nachts, bei Regen oder mit nasser Kleidung. Ein geeigneter Schlafplatz und trockene Schichten schützen davor. |

Zusätzlich gibt es **Tageszeit** und einen begrenzten Vorrat an Gegenständen. So entsteht Druck, ohne dass eine komplexe Simulation nötig wird.

### Verständliche Kosten

Vor einer Aktion zeigt eine kleine Vorschau zum Beispiel:

> Wasserstelle untersuchen — ca. 20 Minuten, −8 Energie

Die Werte sind Spielwerte und sollen leicht verständlich sein. Eine kurze Einblendung erklärt, warum eine Aktion mehr oder weniger kostet, etwa „weiter Weg“ oder „schweres Material tragen“.

### Hunger, Durst und Energie

- Durst soll eine dringende, gut erkennbare Gefahr darstellen.
- Hunger nimmt langsamer zu. Essen ist nützlich, aber Suchen kostet Zeit und Energie.
- Nahrung kann Energie auffüllen oder deren Verlust bremsen.
- Anstrengende Tätigkeiten lassen Hunger und Durst schneller steigen.
- Die Anzeigen sinken nicht überraschend: Warnstufen und kurze Figurengedanken kündigen kritische Zustände an.

## Spielstruktur für 15–20 Minuten

Eine kompakte Kampagne mit drei Abschnitten hält den Umfang überschaubar:

### Abschnitt 1: Absturzstelle und erste Entscheidungen — etwa 4 Minuten

- Orientierung gewinnen und nützliche Ausrüstung aus den Trümmern bergen.
- Eine sichere Wasserquelle finden und Wasser aufbereiten.
- Entscheiden, was sofort mitgenommen wird und was zu schwer oder unnötig ist.

### Abschnitt 2: Der erste Abend — etwa 6 Minuten

- Vor dem Wetterumschwung einen geeigneten Lagerplatz wählen.
- Mit gefundenem Material einen einfachen Schlafschutz herstellen.
- Feuer als mögliche, aber nicht immer notwendige Option abwägen.
- Wasser, Wärme und Tageslicht gegeneinander priorisieren.

### Abschnitt 3: Weg zum Rettungspunkt — etwa 6–10 Minuten

- Eine Papierkarte und Gelände-Hinweise zur Orientierung verwenden.
- Einen sicheren Weg wählen; eine Abkürzung kann zusätzliche Risiken und Kosten mit sich bringen.
- Eine kleine Verletzung versorgen oder die Folgen bei Vernachlässigung tragen.
- Am vereinbarten Punkt ein gut sichtbares Signal vorbereiten und Hilfe erreichen.

## Missionen und Ereignisse

Diese Ereignisse lassen sich in der Hauptkampagne verwenden oder als optionale Varianten einbauen:

1. **Kein Empfang:** Das Handy zeigt Akku, aber keinen Empfang. Die Figur muss aufhören, wiederholt nach Netz zu suchen.
2. **Wo sind wir?:** Die Spielenden richten eine Papierkarte anhand weniger auffälliger Geländepunkte aus.
3. **Die Abkürzung:** Ein Pfad spart scheinbar Zeit, führt aber durch dichteres Gelände und kostet mehr Energie.
4. **Wasser für die Nacht:** Eine nahe Wasserstelle ist nicht automatisch sicher. Die Gruppe muss Quelle und Aufbereitung berücksichtigen.
5. **Die verlorene Flasche:** Die einzige Flasche ist im unübersichtlichen Gepäck verschwunden. Ordnung und Inventar werden wichtig.
6. **Regen in zwanzig Minuten:** Die Spielenden müssen zwischen einem nahen, ungünstigen Platz und einem weiter entfernten, geschützteren Lagerplatz wählen.
7. **Alles ist nass:** Nasse Kleidung und Schlafmaterial verschärfen den Wärmeverlust.
8. **Feuer oder Schutz?:** Zunder ist feucht und Wind kommt auf. Die beste Entscheidung kann sein, zuerst den Schlafplatz zu verbessern.
9. **Der Knöchel:** Eine Figur verletzt sich leicht. Die Gruppe muss die Belastung reduzieren und den Plan anpassen.
10. **Der Fluss:** Die direkte Route ist riskant. Eine sichere Umleitung kostet Zeit und Kraft.
11. **Zu viele Ratschläge:** Figuren erinnern sich an widersprüchliche Online-Tipps. Die Spielenden müssen Beobachtungen und sichere Hinweise höher gewichten.
12. **Das Signal:** An einem sicheren offenen Ort muss die Gruppe auffindbar bleiben, statt ziellos weiterzugehen.
13. **Die letzte Weggabelung:** Zwischen einem längeren bekannten Weg und einer unbekannten Abkürzung entscheiden.
14. **Der zweite Tag:** Der Zustand der Gruppe ist anders als am Vorabend. Der alte Plan muss neu bewertet werden.

Nicht alle Ereignisse müssen in einer Runde vorkommen. Für den Umfang eines Semesterprojekts gelten **sechs Kernaufgaben** (angelehnt an die Ereignisse Nr. 1, 4, 5, 6, 8, 10, 12 und 13 oben):

| # | Abschnitt | Kernaufgabe | Lernziel |
|---|---|---|---|
| 1 | Absturzstelle | **Kein Empfang** – Intro, Handy hilft nicht (Tutorial als Geschichte) | Nicht aufs Handy verlassen |
| 2 | Absturzstelle | **Gepäck bergen** – nützliche Dinge wählen (Flasche, Jacke, Karte, Feuerzeug …) | Prioritäten setzen |
| 3 | Absturzstelle | **Wasser** – nahe Quelle ist unsicher, aufbereiten | Sicheres Wasser zuerst |
| 4 | Abend | **Regen in 20 Minuten** – naher schlechter vs. weiter geschützter Lagerplatz | Schutz und Zeitplanung |
| 5 | Abend | **Feuer oder Schutz?** – feuchter Zunder, Wind: erst Schlafplatz verbessern | Wärme richtig priorisieren |
| 6 | Weg zum Ziel | **Route und Signal** – sichere Route oder Abkürzung, dann sichtbar am offenen Ort | Risiko vs. Kosten, auffindbar bleiben |

Optional, nur wenn Zeit bleibt: Karte ausrichten, Knöchel verletzt, „Zu viele Ratschläge“.

## Schwierigkeit und Game Over

Das Spiel soll fordern, aber die Spielenden sollen verstehen können, warum etwas schiefgeht.

- Eine einzelne kleine Fehlentscheidung sollte nicht sofort zum Game Over führen.
- Mehrere schlechte Entscheidungen können sich summieren: zu spät Wasser suchen, zu lange Umwege gehen, den Schlafplatz vernachlässigen und erschöpft weiterziehen.
- Vor kritischen Zuständen gibt es klare Warnungen, zum Beispiel zittrige Bewegung, langsamere Aktionen oder eine besorgte Gedankenblase.
- Wenn eine Figur wegen Erschöpfung, Durst oder Kälte nicht mehr weitermachen kann, erscheint ein **Game-Over-Bildschirm**.
- Der Bildschirm nennt knapp die Hauptursache und zeigt einen hilfreichen Rückblick: „Du hast viel Kraft für den Umweg verbraucht. Wasser und ein früher Lagerplatz hätten den zweiten Tag erleichtert.“
- Ein schneller Neustart oder ein Neustart ab Abschnittsbeginn hält die kurze Spielzeit angenehm.

Das Ziel ist eine faire Lernschleife: **Fehler erkennen, neu planen, beim nächsten Versuch besser entscheiden.**

### Wiederspielbarkeit und Bewertung

- Am Ende jedes Durchlaufs (Sieg oder Game Over) gibt es eine **Bewertung mit 1–3 Sternen** nach erreichten Lernzielen (z. B. Wasser aufbereitet, Schutz vor Nacht, Signal gesetzt).
- Ein **Rückblick** nennt die wichtigsten Entscheidungen und was besser gewesen wäre.
- **Zwei bis drei zufällige Varianten** pro Durchlauf (z. B. Zeitpunkt des Regens, welche Wasserstelle unsicher ist, welche Route riskant ist) sorgen dafür, dass ein zweiter Durchlauf nicht identisch ist.
- Es gibt Neustart ab Abschnittsbeginn und Neustart des ganzen Spiels.

### Evaluation und Playtest

- **Vorher/Nachher-Quiz** (4–5 Fragen zu den Lernzielen) auf Papier oder im Spiel.
- **Entscheidungs-Log (offline):** speichert Zeitstempel, Aktionen, Werte der vier Anzeigen, Todesursache und Spielzeit in einer lokalen CSV-Datei.
- **GEQ** (Game Experience Questionnaire) und offene Fragen zu Verständlichkeit, Fairness und Spaß.
- Ablauf pro Sitzung: Einführung 2–3 Min., Aufwärmgespräch 5 Min., Spielen 15–20 Min. (laut denkend, ohne Hilfe), Nachgespräch 15–20 Min.

### Quellen für die Lerninhalte

Für das Game Design Document werden belegbare Quellen gesammelt, z. B. Rotes Kreuz / Erste-Hilfe-Leitfäden, Bergrettung (Notfall- und Signalregeln, z. B. internationales Notsignal), Wildnismedizin-Leitfäden (Wasser, Unterkühlung). Das Spiel ersetzt keinen Kurs; beim Start wird ein kurzer Hinweis eingeblendet.

## Humorvolle Sprechblasen

Sprechblasen kommentieren passende Ereignisse und sollten nicht ständig erscheinen. So bleiben sie lustig und stören den Spielfluss nicht.

Beispiele:

- Bei fehlendem Empfang: „Drei Balken Akku und null Balken Netz. Das ist irgendwie persönlich.“
- Beim Kartenlesen: „Kann jemand die Karte drehen, bis sie aussieht wie Maps?“
- Beim Fehlversuch: „Ich hab dazu bestimmt mal ein Video gesehen. Angeschaut hab ich’s nicht.“
- Bei niedrigem Akku: „Mein Handy ist bei zwei Prozent. Ich auch.“
- Beim sinnlosen Aktualisieren: „Ich aktualisiere die Seite noch einmal. Das wird’s richten.“
- Beim ersten gelungenen Plan: „Wir haben das gerade selbst rausgefunden. Komisches Gefühl.“
- Bei Hunger: „Ich würde jetzt gern eine Pizza bestellen. An einen Baum.“
- Bei der Entscheidung umzukehren: „Umkehren ist kein Scheitern. Sagt zumindest die Person, die gerade umkehrt.“

### Regeln für Dialoge

- Pro Auslöser nur wenige Varianten verwenden und Wiederholungen vermeiden.
- Ein ernster Hinweis, etwa zu Durst oder Kälte, hat Vorrang vor einem Witz.
- Bei ernsten Rückschlägen darf der Humor zurücktreten.
- Die Figur kann sich entwickeln: Am Anfang fragt sie nach einer App, später beschreibt sie selbst, was sie gelernt hat.

## Visueller Stil und Bedienung

- 2D-Pixelgrafik aus der Top-down-Perspektive.
- Kleine, überschaubare Gebiete mit klar erkennbaren Orientierungspunkten.
- Wenige gut unterscheidbare Ressourcen- und Gegenstandssymbole.
- Gut lesbare Anzeigen für Energie, Durst, Hunger, Wärme und Tageszeit.
- Interaktionen über einen einzigen klaren Knopf oder eine einfache Tasteneingabe.
- Kurze Animationen für Sammeln, Trinken, Ausruhen, Bauen und Verletzungen.
- Gedankenblasen mit kurzer Textanimation und einem kleinen Porträt der Figur.

## Überschaubare Unity-Umsetzung

### Erstes spielbares Minimum

Für den ersten Prototyp reichen:

1. Eine spielbare Figur mit Top-down-Bewegung.
2. Eine kleine Karte mit Absturzstelle, Wasserpunkt, Lagerplatz und Ausgang.
3. Vier Ressourcenanzeigen und ein einfacher Tageszeit-Timer.
4. Interaktive Objekte mit festen Zeit- und Energiekosten.
5. Ein kleines Inventar mit wenigen Gegenständen.
6. Ein System für Missionsziele und Abschlussbedingungen.
7. Sprechblasen, Warnungen und Game-Over-Bildschirm.

### Einfache technische Aufteilung

- **PlayerController:** Bewegung und Interaktion.
- **SurvivalStats:** Energie, Durst, Hunger, Wärme und deren Veränderung.
- **ActionSystem:** Prüft Kosten, führt Aktionen aus und lässt Zeit verstreichen.
- **Inventory:** Verwaltet die wenigen Gegenstände.
- **MissionManager:** Aktiviert Aufgaben, verfolgt Fortschritt und startet das Ende.
- **DialogueBubbleUI:** Zeigt Figurengedanken und Hinweise an.
- **GameStateManager:** Wechselt zwischen Spiel, Pause, Sieg und Game Over.

Missionen, Aktionen und Dialoge können als `ScriptableObject`-Daten angelegt werden. Damit lassen sich Texte und Werte im Unity-Editor ändern, ohne für jede neue Aufgabe ein eigenes Skript zu bauen. Für einen ersten Prototyp genügt auch eine einfache, direkt im Code gepflegte Missionsliste.

### Aktionsdaten

Jede Aktion sollte wenige einstellbare Werte haben:

- Name und kurze Beschreibung
- Zeitkosten
- Energiekosten
- optionale Änderung von Durst, Hunger oder Wärme
- Voraussetzungen, etwa benötigter Gegenstand oder geeigneter Ort
- Ergebnis bei Erfolg
- verständlicher Hinweis, wenn etwas nicht funktioniert

### Sprechblasen-Auslöser

Ein Dialog kann durch einfache Spielereignisse ausgelöst werden:

- Handy ohne Empfang benutzt
- Aktion zum ersten Mal fehlgeschlagen
- Energie oder Wasser unter einen Warnwert gefallen
- Lagerplatz ausgewählt
- sichere Route gefunden
- Mission abgeschlossen

Eine Priorität und eine kurze Abklingzeit verhindern, dass Dialoge einander überlagern.

## Umfang bewusst begrenzen

Für ein erstes fertiges Spiel sollte der Umfang auf **eine Hauptfigur, eine kleine handgebaute Karte, einen Spieltag mit Nacht (bis zum Mittag des Folgetags), wenige Gegenstände und sechs Kernaufgaben** beschränkt bleiben. Wetter, Verletzungen und Nahrungssuche werden als einfache Ereignisse umgesetzt, nicht als Simulation.

**Bewusst gestrichen oder vereinfacht:**

- Kein Gewichtssystem im Inventar; stattdessen 3–4 feste Plätze.
- Feuer ist eine **Entscheidung mit Folge**, kein Bastel-Minispiel.
- Verletzung („Knöchel“) ist optional.
- Hunger ist ein langsamer, einfacher Wert; Essen gibt es nur als eindeutig sichere Gegenstände.
- Keine zufällig erzeugte Welt; Orientierungspunkte bleiben immer an derselben Stelle.
- Gedankenblasen sind **kurz und schnell** (max. zwei Zeilen) und lassen sich überspringen.
- Keine Dark Patterns: kein künstlicher Zeitdruck durch Serien oder Belohnungsfallen, nur der erklärbare Druck der Spielwelt.

Lebensmittel und Wasser sollten im Spiel klar als sicher, unsicher oder aufbereitungsbedürftig markiert sein. Das Spiel sollte keine riskante Anleitung zum Verzehr unbekannter Pflanzen vermitteln. Seine Lernbotschaft kann sein: **Unbekanntes nicht essen, Risiken erkennen und sichere Hilfe suchen.**

## Arbeitstitel

- **Ohne Empfang**
- **Kein Netz. Kein Plan.**
- **Akku leer**
- **Offline: Überleben**

## Pitch in einem Satz

Nach einem Flugzeugabsturz muss eine handygewöhnte Figur ohne Internet Wasser, Wärme und einen sicheren Weg organisieren – und lernen, dass jede Entscheidung Zeit und Kraft kostet.
