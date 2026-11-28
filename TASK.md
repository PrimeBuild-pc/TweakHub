# TASK — TweakHub

> Documento di specifica approvato il 23/10/2026.
> Fonte delle decisioni: `HANDOFF.md` e matrice di roadmap approvata.
> **Stato:** task documentati e approvati come direzione; **nessuna implementazione autorizzata**.
> Le modifiche a codice, configurazioni, script o test richiedono una nuova autorizzazione esplicita.

## Regole comuni

- Non creare commit, branch o PR senza autorizzazione di Lorenzo.
- Non toccare il WIP esistente del branch `main`.
- Prima di ogni build chiudere `TweakHub.exe`.
- Test: `dotnet test TweakHub.Tests/TweakHub.Tests.csproj`.
- **Prerequisito aperto:** `HANDOFF.md` riporta 321/321 alla riga 10 e 335/335 nel log alla riga 58. Riconciliare il conteggio prima di usarlo come baseline di accettazione.
- Stringhe in 6 lingue (`Resources/*.resx`); `LocalizationTests` controlla le chiavi.
- Script elevati solo tramite il flusso con hash approvato; `ElevationHardeningTests` è vincolante.
- Non dichiarare un'operazione reversibile se il ripristino completo non è garantito.
- Distinguere sempre: esito dell'operazione, stato configurato, stato effettivo, vincoli esterni/policy, possibilità di rollback. Non comprimere le dimensioni in un unico stato ambiguo.
- Le task possono usare l'infrastruttura esistente (`RegistryService`, backup, servizi, viste); l'implementazione interna resta libera.
- Ogni task deve essere incrementale e verificabile da solo.
- Etichette di analisi: `VERIFICATO`, `INFERITO`, `DA VALIDARE`, `NON ACCESSIBILE`.

## Tracciabilità ID

- `TH-USB-03` è e resta **refresh live** della topologia USB.
- `TH-USB-05` è **diagnostica del link**, con confidenza e senza speculazione.
- `TH-USB-06` è **export del bundle di troubleshooting**.
- `TH-USB-04` è **test porta/banda**.
- `TH-PRV-03` è e resta **drift detection**.
- `TH-PRV-04` è **configured/effective/policy/unknown**.
- `TH-BEN-01` e `X-PRIV-01` hanno una **baseline P1** obbligatoria e un completamento P2.
- `TH-VBS-02` resta condizionato a `TH-VBS-01` e a nuova approvazione.

## P0

Nessun task P0 in TweakHub. Il primo lavoro è la baseline P1 e il rollback.

## P1 — fondamenta e domini principali

### TH-BEN-01 — Baseline build Windows e applicabilità

**Priorità:** P1 per la baseline; P2 per la copertura completa
**Dipendenze:** nessuna per la baseline
**Obiettivo:** rendere il supporto alle build Windows un requisito di progettazione fin dall'inizio, senza aspettare la matrice completa.

**In scope — baseline P1**
- Modello minimo per dichiarare build minima/massima, build note e comportamento su build sconosciuta.
- Gate di applicabilità usabile da GameDVR, privacy e VBS.
- Metadati per le voci nuove e per quelle che vengono toccate dai task P1.
- Fallback esplicito: build sconosciuta ≠ supportata.

**In scope — completamento P2**
- Copertura completa del catalogo.
- Matrice delle build supportate e delle eccezioni note.
- Integrazione con la UI e con i test di disponibilità.

**Out of scope**
- Nuovo framework di metadati.
- Refactoring globale del catalogo.
- Modifiche a ZapTweaks.

**Requisiti**
- Le nuove funzionalità devono consultare la baseline fin dal primo rilascio, non solo alla fine.
- Le voci non supportate devono essere nascoste o marcate, non applicate silenziosamente.
- La build sconosciuta non deve essere assunta come supportata.
- I metadati devono essere testabili e leggibili dalla UI.
- La copertura completa può arrivare dopo, ma non può essere un prerequisito per usare il gate.

**Criteri di accettazione**
- Almeno GameDVR, privacy e VBS usano il gate di build nella baseline.
- Test per build minima, build superiore, build sconosciuta e voce non supportata.
- Una voce non supportata non viene applicata per default.
- La baseline è usabile senza attendere la matrice completa.
- `dotnet test` e `LocalizationTests` verdi.

### X-PRIV-01 — Ownership privacy e registro canonico delle chiavi

**Priorità:** P1 per la baseline; P2 per l'automazione completa
**Dipendenze:** nessuna per la baseline; per il completamento: `TH-PRV-01`, `TH-PRV-02`, `ZT-PRV-01`, `ZT-GDV-01`
**Obiettivo:** definire chi possiede ogni chiave privacy/GameDVR e quali sovrapposizioni sono intenzionali, prima di aggiungere nuove funzionalità.

**In scope — baseline P1**
- Matrice compatta: dominio, chiave, owner primario, eventuale secondo owner, tipo di sovrapposizione, motivazione.
- Registro canonico delle chiavi toccate da privacy, GameDVR e VBS.
- Regola di scrittura: una chiave ha un owner primario; gli altri devono passare da un'API/contratto condiviso o non scriverla.
- Ownership esplicita per GameBar/GameDVR: ZapTweaks e TweakHub non devono duplicare la stessa responsabilità.

**In scope — completamento P2**
- Automazione del controllo delle sovrapposizioni.
- Report delle chiavi non registrate o scritte da più componenti.
- Aggiornamento continuo della matrice con il catalogo.

**Out of scope**
- Separazione rigida tra i due prodotti.
- Eliminazione delle sovrapposizioni utili.
- Refactoring globale della UI privacy.

**Requisiti**
- Le sovrapposizioni sono ammesse solo se intenzionali e documentate.
- Le nuove chiavi non possono essere aggiunte senza una voce nel registro.
- La matrice deve essere disponibile prima di `TH-PRV-01` e `TH-GDV-01`, non dopo.
- Deve essere chiaro cosa è owner TweakHub e cosa è owner ZapTweaks.
- Il registro deve essere abbastanza piccolo da essere revisionabile.

**Criteri di accettazione**
- Esiste una matrice con owner primario per le chiavi di privacy, GameDVR e VBS.
- GameBar/GameDVR ha un solo owner funzionale.
- Le sovrapposizioni intenzionali hanno una motivazione scritta.
- `TH-PRV-01` e `TH-GDV-01` possono partire usando la baseline.
- Nessuna nuova chiave viene introdotta senza registrazione.

### TH-RB-01 — Rollback e conflitti TweakHub

**Priorità:** P1
**Dipendenze:** riconciliazione del conteggio test
**Obiettivo:** rendere il rollback e la verifica TweakHub affidabili anche in caso di errore, eccezione o conflitto tra tweak.

**In scope**
- `ApplyValueWithBackup` e verifica del valore.
- Percorso composite `ApplyValuesWithBackupAsync`/`RestoreValuesAsync`.
- Percorso eccezione e rollback parziale.
- Conflitti tra `disable_fullscreen_optimizations` e `optimize_flip_model` su `GameDVR_FSEBehaviorMode`.
- Separazione di `AutoGameModeEnabled` dal dominio GameDVR.
- Stato UI: riuscito, parziale, fallito, rollback incompleto.

**Out of scope**
- Nuovo engine generale di transazioni.
- Refactoring di tutte le operazioni non coinvolte.
- Modifiche a ZapTweaks.

**Requisiti**
- Se la verifica di un singolo valore fallisce, il valore precedente deve essere ripristinato o l'operazione deve dichiarare `rollback_incomplete`.
- Il percorso eccezione deve essere testato, non solo quello con `!success`.
- I conflitti di chiave devono essere rilevati prima dell'apply e dichiarati nella UI.
- `AutoGameModeEnabled` non deve essere modificato come effetto collaterale di un tweak GameDVR.
- Gli stati configured/effective/policy/rollback devono essere distinti.
- Il rollback non deve sovrascrivere una modifica esterna intervenuta nel frattempo.

**Criteri di accettazione**
- Test: verifica fallita → ripristino o stato incompleto esplicito.
- Test: eccezione durante il composite → rollback o stato incompleto.
- Test: conflitto `GameDVR_FSEBehaviorMode` rilevato con valori 2 vs 0.
- Test: `AutoGameModeEnabled` non viene toccato dal task GameDVR.
- La UI non mostra “ripristinato” se il rollback non è completo.
- `dotnet test` e `LocalizationTests` verdi.

### TH-GDV-01 — GameDVR corretto e onesto

**Priorità:** P1
**Dipendenze:** `TH-BEN-01` baseline, `X-PRIV-01` baseline, `TH-RB-01`, riconciliazione test
**Obiettivo:** correggere il comportamento GameDVR e la promessa UI, senza rimuovere pacchetti nel MVP e senza effetti collaterali su Game Mode.

**Fase 1 — MVP**
- Correggere `disable_game_bar`: oggi scrive solo `AppCaptureEnabled=0` ma promette la disattivazione Game Bar.
- Rilevare lo stato reale di Game Bar/GameDVR e mostrare etichette oneste.
- Distinguere cattura, overlay, hotkey e avvio.
- Non toccare `AutoGameModeEnabled`.

**Fase 2 — completamento P1**
- Coprire overlay, hotkey, avvio e prompt/protocolli residui.
- Verificare la compatibilità con Xbox app, Game Pass e Gaming Services.
- Salvare e ripristinare i valori originali.
- Documentare cosa resta attivo dopo l'applicazione.

**Fuori dal MVP e non autorizzato**
- Rimozione del pacchetto Xbox Game Bar: opzionale, fuori dal MVP, richiede nuovo ID e nuova approvazione.
- Promesse di FPS, latenza o benefici non misurati.
- `AutoGameModeEnabled`.

**Requisiti**
- Usare la baseline build di `TH-BEN-01` per nascondere o marcare le funzioni non supportate.
- Rispettare l'ownership di `X-PRIV-01`: GameBar/GameDVR non deve essere duplicato in privacy.
- Stato configured/effective/policy/unknown.
- Rollback esatto o dichiarazione esplicita.
- La UI deve spiegare la differenza tra “cattura disattivata”, “overlay disattivato” e “Game Bar non disponibile”.

**Criteri di accettazione**
- `disable_game_bar` non promette più della funzione che esegue.
- Test per stato GameDVR attivo/disattivato, policy, valori assenti e rollback.
- Test che `AutoGameModeEnabled` resti invariato.
- Nessuna rimozione pacchetto nel MVP.
- Compatibilità Xbox documentata.
- `dotnet test` e `LocalizationTests` verdi.

### TH-USB-01 — Modello dati e topologia USB affidabile

**Priorità:** P1
**Dipendenze:** nessuna
**Obiettivo:** costruire una base dati completa e onesta per controller, hub, porte e dispositivi USB.

**In scope**
- Topologia completa: controller, hub, porte, dispositivi, relazioni padre/figlio.
- Distinzione tra dati enumerati da Windows e dati inferiti.
- Confidenza CPU/chipset con evidenze.
- Gestione di root hub mancanti, parent mancanti, dispositivi composti e porte logiche/fisiche.
- Velocità negoziata distinta dalla banda misurata.
- Modello pronto per refresh, diagnostica ed export.

**Out of scope**
- UI completa, che è `TH-USB-02`.
- Refresh live, che è `TH-USB-03`.
- Test di banda, che è `TH-USB-04`.
- Diagnostica, che è `TH-USB-05`.
- Export, che è `TH-USB-06`.

**Requisiti**
- Non inventare un controller o una porta se il dato non è disponibile.
- Ogni inferenza deve avere confidenza ed evidenza.
- I dati mancanti devono produrre uno stato esplicito, non un valore fittizio.
- Il modello deve essere serializzabile per l'export.
- Deve gestire la disconnessione senza corrompere la topologia.

**Criteri di accettazione**
- Fixture per: root hub mancante, parent mancante, dispositivo composto, controller inferito, velocità sconosciuta.
- La topologia ricostruisce controller → hub → porta → dispositivo quando i dati lo consentono.
- I dati identificati e inferiti sono separati nel modello.
- La velocità negoziata non viene presentata come banda misurata.
- Test unitari del modello e dei casi limite.

### TH-USB-02 — Sezione Device orientata al gaming

**Priorità:** P1
**Dipendenze:** `TH-USB-01`
**Obiettivo:** rendere la sezione Device di TweakHub significativamente più utile dell'attuale, non solo un refactoring interno.

**In scope**
- Vista chiara per controller, hub, porte e dispositivi.
- Raggruppamento per controller e percorso fisico.
- Etichette comprensibili per velocità, porta, hub condiviso e confidenza.
- Riepilogo utile per il gaming: controller/hub critici, dispositivi lenti, condivisione di risorse.
- Fallback quando la topologia è incompleta.
- Inserimento nella sezione Device esistente o in una vista dedicata; non è obbligatorio usare `TopologyTab`.

**Out of scope**
- Diagnostica speculativa.
- Test di banda.
- Export.
- Modifiche al modello dati oltre a quelle necessarie per la UI.

**Requisiti**
- La UI deve essere comprensibile a un utente gaming non esperto.
- I dati inferiti devono essere marcati come tali.
- Nessun avviso automatico basato su un'ipotesi non dimostrata.
- La vista deve restare usabile con dati parziali.
- I controlli e i testi devono essere localizzati nelle 6 lingue.

**Criteri di accettazione**
- Un utente può capire quale controller/hub/porta sta usando e con quale velocità.
- I dati mancanti sono mostrati come “non disponibili”, non come zero.
- La vista non si rompe con root hub o parent mancanti.
- Test UI per stato completo, parziale e vuoto.
- `dotnet test` e `LocalizationTests` verdi.

### TH-USB-03 — Refresh live della topologia

**Priorità:** P1
**Dipendenze:** `TH-USB-01`
**Obiettivo:** aggiornare la topologia dopo connessione/disconnessione senza perdere il contesto dell'utente.

**In scope**
- Rilevamento di connessione/disconnessione.
- Refresh con debounce.
- Conservazione di selezione, scroll e filtri quando possibile.
- Gestione di sospensione/ripresa e rapide sequenze di eventi.
- Indicatore di aggiornamento non invasivo.

**Out of scope**
- Diagnostica del link.
- Export.
- Test di banda.

**Requisiti**
- Non bloccare la UI durante il refresh.
- Non perdere la selezione se il dispositivo è ancora presente.
- Se il dispositivo selezionato scompare, mostrare uno stato coerente.
- Evitare refresh multipli ravvicinati.
- Il refresh deve usare il modello di `TH-USB-01`.

**Criteri di accettazione**
- Connessione/disconnessione aggiorna la vista entro un tempo ragionevole.
- Selezione e scroll vengono preservati quando possibile.
- Una raffica di eventi non produce refresh multipli o freeze.
- Test con eventi simulati e dispositivo rimosso.
- `dotnet test` verde.

### TH-PRV-01 — Catalogo privacy ampliato

**Priorità:** P1
**Dipendenze:** `TH-RB-01`, `TH-BEN-01` baseline, `X-PRIV-01` baseline
**Obiettivo:** ampliare il catalogo privacy con voci comprensibili, applicabili e reversibili.

**In scope**
- Nuove voci privacy ispirate a ShutUp10++/WinToolify, senza copiare codice non compatibile.
- Per ogni voce: cosa cambia, build supportate, conseguenze applicative, rollback, rischio.
- Separazione tra consigliato e aggressivo.
- Rispetto dell'ownership e del registro chiavi.

**Out of scope**
- Profili completi, che sono `TH-PRV-02`.
- Drift detection, che è `TH-PRV-03`.
- Configured/effective, che è `TH-PRV-04`.
- Modifiche a ZapTweaks.

**Requisiti**
- Ogni voce deve avere una spiegazione verificabile.
- Le build non supportate devono essere nascoste o marcate.
- Le voci aggressive non devono essere preselezionate.
- Le chiavi duplicate o con owner diverso non vanno aggiunte senza `X-PRIV-01`.
- Il rollback deve essere esatto o dichiarato non disponibile.

**Criteri di accettazione**
- Catalogo con metadati completi per ogni voce.
- Test per build supportata/non supportata, rollback, ownership.
- Nessuna voce nuova senza registrazione in `X-PRIV-01`.
- `dotnet test` e `LocalizationTests` verdi.

### TH-PRV-02 — Profili privacy e conseguenze

**Priorità:** P1
**Dipendenze:** `TH-PRV-01`, `TH-BEN-01` baseline, `X-PRIV-01` baseline
**Obiettivo:** offrire profili privacy con rollback, applicabilità e conseguenze chiare.

**In scope**
- Profili safe e aggressive.
- Anteprima delle modifiche e delle app che possono smettere di funzionare.
- Rollback e stato per profilo.
- Separazione netta degli aggressivi.

**Out of scope**
- Nuove chiavi non presenti in `TH-PRV-01`.
- Drift detection.
- Configured/effective.

**Requisiti**
- Nessun profilo aggressivo silenzioso o preselezionato.
- Ogni profilo deve dichiarare cosa è reversibile e cosa no.
- Le build non supportate devono essere gestite dal gate di `TH-BEN-01`.
- Le sovrapposizioni devono rispettare `X-PRIV-01`.

**Criteri di accettazione**
- Un profilo mostra modifiche, conseguenze, rollback e build.
- Il profilo safe non include voci aggressive.
- Il rollback è verificato o dichiarato.
- Test per applicazione, rollback e voce non supportata.

### TH-VBS-01 — Decisione e prototipo VBS/HVCI

**Priorità:** P1
**Dipendenze:** `TH-BEN-01` baseline per le build
**Obiettivo:** decidere e prototipare il comportamento TweakHub VBS/HVCI, partendo da read-only.

**In scope**
- Rilevamento read-only di VBS/HVCI.
- Confronto tra read-only e futura modalità esperta protetta.
- Rischi, benefici, requisiti di sicurezza e limiti.
- Prototipo di sola lettura, senza modifiche.

**Out of scope**
- Implementazione della modalità esperta.
- Bypass di policy o `Locked`.
- Disattivazione nei preset.
- Modifiche a ZapTweaks.

**Requisiti**
- Il MVP è read-only.
- La modalità esperta è solo una proposta successiva, soggetta a nuova approvazione.
- Lo stato deve distinguere configured/effective/policy/unknown.
- La UI non deve suggerire che il rilevamento sia un fix.

**Criteri di accettazione**
- Decisione documentata con opzioni e raccomandazione.
- Prototipo read-only senza scritture.
- Test per VBS attivo, disattivato, policy, unknown.
- Nessuna implementazione esperta senza nuova approvazione.

## P2 — completamento, stato ed evoluzioni approvate

### TH-PRV-03 — Drift detection privacy

**Priorità:** P2
**Dipendenze:** `TH-PRV-01`, `TH-PRV-02`, `TH-RB-01`
**Obiettivo:** rilevare quando un'impostazione privacy è cambiata dopo l'applicazione.

**In scope**
- Confronto tra stato atteso e stato corrente.
- Rilevamento di modifiche esterne, policy e ripristini.
- Notifica non invasiva e proposta di riallineamento manuale.
- Uso del modello di stato di `TH-RB-01`.

**Out of scope**
- Riapplicazione automatica.
- Nuove voci privacy.
- Modifica di policy.

**Criteri di accettazione**
- Test: modifica esterna rilevata.
- Test: policy rilevata senza tentativo di override.
- Test: valore tornato al default.
- Nessun riallineamento automatico senza consenso.

### TH-PRV-04 — Configured/effective/policy/unknown

**Priorità:** P2
**Dipendenze:** `TH-PRV-01`, `TH-PRV-02`, `TH-PRV-03`
**Obiettivo:** mostrare la differenza tra configurazione scritta, stato effettivo e vincoli esterni.

**In scope**
- Stato configured, effective, policy, unknown.
- Spiegazioni nella UI.
- Test per valori illeggibili o gestiti da policy.

**Out of scope**
- Nuovi tweak.
- Modifica automatica delle policy.
- Drift detection, già in `TH-PRV-03`.

**Criteri di accettazione**
- La UI non mostra “attivo” quando lo stato effettivo è sconosciuto.
- Policy e unknown sono distinti.
- Test per tutti gli stati.
- `LocalizationTests` verde.

### TH-USB-05 — Diagnostica link non speculativa

**Priorità:** P2
**Dipendenze:** `TH-USB-01`, `TH-USB-02`, `TH-USB-03`
**Obiettivo:** offrire diagnostica USB basata su evidenze, senza trasformare ipotesi in avvisi.

**In scope**
- Velocità negoziata, tipo di link, condivisione di hub/controller, confidenza.
- Evidenze per ogni conclusione.
- Distinzione tra problema dimostrato, sospetto e dato mancante.

**Out of scope**
- Test di banda.
- Avvisi automatici basati su inferenze.
- Modifiche al modello dati oltre a quelle necessarie.

**Criteri di accettazione**
- Ogni diagnosi ha un'evidenza o è marcata come sospetta.
- Nessun avviso “lento” basato solo su un'ipotesi.
- Test per link veloce, lento, sconosciuto e hub condiviso.
- `dotnet test` e `LocalizationTests` verdi.

### TH-USB-06 — Export troubleshooting

**Priorità:** P2
**Dipendenze:** `TH-USB-01`, `TH-USB-02`, `TH-USB-03`
**Obiettivo:** esportare un bundle utile per il troubleshooting della topologia USB.

**In scope**
- Topologia, controller, hub, porte, dispositivi.
- Velocità negoziate, confidenza, evidenze e timestamp.
- Stato dati mancanti/inferiti.
- Formato leggibile e allegabile.

**Out of scope**
- Dati personali non necessari.
- Diagnostica speculativa.
- Test di banda.

**Criteri di accettazione**
- L'export contiene tutti i dati essenziali e le confidenze.
- I dati mancanti sono espliciti, non assenti senza spiegazione.
- Nessun segreto o dato personale non necessario.
- Test del contenuto e della serializzazione.

## Evoluzioni rinviate — non autorizzate all'implementazione

Queste attività non sono autorizzate e richiedono un nuovo ID e una nuova approvazione:

- `TH-USB-04` — test porta/banda misurata. Dipende da `TH-USB-01/02/03`; deve distinguere velocità negoziata e banda misurata; opt-in.
- `TH-VBS-02` — modalità esperta VBS/HVCI protetta. Dipende da `TH-VBS-01` e da una nuova approvazione; nessun preset, nessun bypass, backup esatto.
- `TH-OSINT-01` — FreeOSINTUI. Rimandato; nessuna integrazione senza pertinenza dimostrata.
- Rimozione del pacchetto Xbox Game Bar: fuori dal MVP; richiede nuovo ID e nuova approvazione.
- Automazione completa di `X-PRIV-01` oltre la baseline P1: solo dopo i task P1.
- Copertura completa di `TH-BEN-01` oltre la baseline P1: prevista P2, non prerequisito.

## Scartati

- `ST-HT-01`: nessun task per documentare il rigetto di Hypertune.
- `X-DOC-01` come task separato: la matrice evidenze è un criterio di accettazione.
- Task per memory cleaner, FPS booster, tweak pseudoscientifici o progetti esterni non compatibili.
- Qualsiasi integrazione che richieda codice GPL/Commons Clause nei repo MIT.

## Riferimenti operativi

- `HANDOFF.md`: stato corrente, WIP, regole di progetto.
- `Services/TweakService.cs`: GameDVR, privacy, composite.
- `Services/RegistryService.cs`: backup, apply e restore.
- `Services/UsbService.cs`, `UsbSpeedService.cs`, `LinkService.cs`, `DeviceService.cs`: modello USB.
- `Views/DevicesPage.xaml`, `UsbInputView.xaml`: UI esistente.
- `Models/Playbook.cs`, `Models/TweakCategory.cs`: catalogo e metadati.
- `TweakHub.Tests/`: test di rollback, localizzazione, elevazione.
