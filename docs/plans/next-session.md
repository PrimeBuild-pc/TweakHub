# Prossima sessione — TweakHub

Stato al 12/10/2026. Questo file è solo locale (non committato): leggerlo all'inizio della prossima sessione.

## Dove siamo
- Le PR dalla #7 alla #14 sono tutte unite in `main`, un commit squash per ciascuna. L'ultimo è `89ff6c5 Fix the minimum processor state registry path in Tweak check (#14)`: corregge un percorso del registro sbagliato nella #13.
- Il merge è stato fatto come admin, perché `main` richiede 1 approvazione e non puoi approvare le tue PR. La CI `build-and-test` era verde su ogni PR.
- Build con 0 warning, 179/179 test.
- Due passate di revisione ponytail, nessun problema critico aperto.

## Da fare: test in VM
Queste funzioni cambiano il sistema e non sono mai state provate dal vivo:
- applicare impostazioni ed eseguire azioni sui piani energetici (attiva, duplica, rinomina, elimina, importa, esporta);
- eseguire un playbook con passo PowerPlan su un PC che non ha quel piano. La copia dovrebbe prendere il GUID salvato: eseguendo di nuovo il playbook deve aggiornare la stessa copia, non crearne un'altra;
- "Crea da questo PC" con il tweak "Prestazioni elevate" attivo: il playbook non deve contenere quel tweak;
- riavvio da amministratore che esegue il playbook (`--run-playbook`);
- script di Marius Heier;
- tweak di processore e sospensione USB su Windows in un'altra lingua, ad esempio in italiano: verifica la lettura tramite powrprof.
- nuovo controllo "Stato minimo del processore bloccato" in Verifica tweak (#13). In VM impostare `ValueMax = 0` in `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\893dee8e-2bef-41e0-89c6-b55d0929964c`, poi verificare:
  - che il controllo lo segnali;
  - che la correzione lo riporti a 100;
  - che dopo il riavvio l'editor del piano permetta 0–100% e il tweak "Disabilita throttling CPU" funzioni;
  - che "Annulla" lo rimetta a 0.
  Sul PC di Lorenzo il valore è davvero 0: lì si può provare la correzione anche fuori dalla VM.

## PR #15 aperta, non ancora unita (12/10/2026)
Branch `feature/tweaks-network-migration`, CI da controllare. Da fare il merge solo dopo i test in VM e con l'ok di Lorenzo. Test da fare in VM:
- applicare e ripristinare ognuno dei nuovi tweak: telemetria, contenuti suggeriti, Bing, "Termina attività", avvio rapido, BSOD dettagliata e i criteri AI estesi;
- "Ottimizza la presentazione per l'independent flip": dopo il riavvio HAGS deve essere attivo e `DirectXUserGlobalSettings` deve conservare le altre voci. Il ripristino riporta tutto com'era;
- preset di rete a bassa latenza: compare la finestra di scelta quando ci sono più schede supportate, con "Ethernet 2" preselezionata. La scheda fantasma e VirtualBox restano escluse. Su Intel/Realtek Ethernet e Wi-Fi Intel cambia solo le voci dichiarate dal driver. Con una scheda non supportata (es. Broadcom o Killer/Marvell) l'interruttore è disattivato e mostra il motivo. Il ripristino rimette i valori originali e la rete torna dopo il riavvio;
- nuovo tweak "Limita i download in background di Windows Update" (30%);
- con un utente che aveva i criteri AI applicati dalla v1.4.2: il tweak risulta parziale e il ripristino deve funzionare;
- "Importa da un'altra copia" da una cartella portabile v1.4.2 reale con modifiche applicate: dopo il riavvio le modifiche devono risultare annullabili;
- script "Pulizia dei file temporanei";
- Sicurezza: pulsante MalwareBazaar e pulsante AbuseIPDB, che deve comparire solo per gli IP pubblici.

Correzioni dell'audit ponytail, nella stessa PR. Test da fare in VM con un utente **non amministratore**:
- preset Windows Update "Sicurezza", poi ripristino: `PreventDeviceMetadataFromNetwork` deve sparire, il preset non deve risultare "Personalizzato" e il pulsante "Ripristina tutto" deve scomparire se non resta altro;
- rifiutare l'UAC su un tweak HKLM di gruppo: deve comparire una sola richiesta UAC e il tweak non deve risultare annullabile;
- "Disabilita throttling CPU" su Bilanciato, passare a Risparmio energia, applicare di nuovo, poi disattivare: entrambi i piani tornano al valore originale e il piano attivo non cambia. Nessuna richiesta UAC;
- "Applica tutti i consigliati" e "Ripristina tutto": propongono il riavvio come amministratore (una sola UAC); dopo il riavvio si preme di nuovo il pulsante. SysMain e Prefetch non devono comparire tra le modifiche ripristinate;
- DNS: applicare su Wi-Fi, poi su Ethernet, poi ripristinare: entrambe le schede tornano ai server originali;
- script di Marius Heier: la prima volta compare l'avviso; la seconda no; la finestra elevata parte e il file temporaneo viene cancellato;
- disattivazione accelerazione mouse: applicare e ripristinare;
- flip model: non tocca più `OverlayTestMode`; la riparazione "Mpo" della Verifica tweak resta annullabile anche dopo aver attivato e disattivato il flip model.

## Facoltativi
Nessuno. Rilettura dello stato dei tweak a ogni visita: lasciata com'è, di proposito. Dopo le correzioni costa un solo processo, e senza rileggere si vedrebbero interruttori sbagliati quando un'altra sezione ha cambiato qualcosa. Il branch locale `codex/pro-tools-and-repair-fixes` è stato cancellato: il suo contenuto era già in `main` con la PR #4.

## Note utili
- Screenshot di prova: gli script `shot.ps1` / `invoke.ps1` erano nello scratchpad della sessione precedente. La splash dura circa 9 s, quindi aspettare 16 s dopo l'avvio.
- Prima di ogni build chiudere `TweakHub.exe`, che altrimenti blocca il file.
- Commit sempre come `PrimeBuild <primebuild.official@gmail.com>`, senza attribuzioni a Claude.
- Le PR impilate con merge squash vanno in conflitto dopo il primo merge. Prima di ciascuna verificare che `main` abbia lo stesso contenuto della testa della PR precedente, poi fare `git merge -s ours origin/main` nel branch successivo, il push, attendere la CI e fare il merge.

## PR #16 aperta (12/10/2026): tab Collegamenti
Branch `feature/pcie-topology` da `main`, indipendente dalla #15. Piano e stato in `docs/plans/pcie-topology.md`. Da verificare dal vivo: NVIDIA in idle e sotto carico (la generazione letta da Windows si aggiorna?), Radeon in slot x8 (ADLX restituisce x8?), PC Intel 600/700/800 (classificazione CPU/chipset). Gli script per scaricare manuali e pagine (ASUS API, ASRock, MSI, Chrome headless per MSI/Gigabyte) erano nello scratchpad della sessione: per aggiungere schede basta aggiungere righe al formato `NOME | CHIPSET | MATCH | URL | M2 | SLOT | CONDIVISIONE` e rigenerare `Resources/Boards.json`.

## Stato al 12/10/2026, sera
- PR #15, #16 e #17 unite in `main` (squash, merge da admin). `main` è alla versione 1.5.0-beta.1 e coincide con la pre-release pubblicata, tranne l'avviso beta nel README. Branch delle PR e `release/1.5.0` cancellati; il tag `v1.5.0-beta.1` resta.
- Le sezioni "PR #15" e "PR #16" sopra restano valide come lista dei test in VM. Le correzioni vanno in nuove PR su `main`.
- Per la versione finale: togliere `VersionSuffix` da `TweakHub.csproj`, aggiornare nel README il pulsante e l'avviso beta (link a `v1.5.0-beta.1`), tag `v1.5.0`. Valutare prima la regola dell'updater (`UpdateService.FindUpdateAsync`: chi ha una beta riceve solo pre-release, quindi non vedrebbe la 1.5.0 finale).
- Screenshot: copia portabile in inglese in una cartella separata (`portable.flag` + `Data/appearance.json` con `"Language":"en"`), finestra 1600x1000. Non usare le schede Defender (cronologia con percorsi personali) e System (connessioni); oscurare indirizzi della scheda di rete e percorso dati.

## Stato al 14/10/2026: test in VM fatti
- PR #19 (test polling del mouse) unita in `main`. Nessuna release.
- Test in VM `NeuroTune-W11` (Hyper-V), con un amministratore e un utente standard. Tutti i test delle sezioni "Test in VM", "PR #15" e "audit" sono stati eseguiti. Dopo i test la VM è stata riportata al checkpoint precedente.
- Trovati e corretti 11 bug nella **PR #20** (`fix/vm-test-findings`), aperta e **non unita**: aspetta l'ok di Lorenzo. Dettagli nella descrizione della PR. I più gravi:
  - `New-Item -Force` nello script dei preset Windows Update cancellava le chiavi: il preset Security non veniva mai applicato per intero e le altre policy WU venivano cancellate;
  - un utente standard con "Riavvia come amministratore" scriveva i tweak HKCU nel profilo dell'amministratore;
  - Bing e Notifiche fallivano sempre, perché `HKCU\Software\Policies` richiede l'elevazione;
  - "Ripristina tutto" falliva sul preset WU.
- Restano da provare **su un PC fisico**: HAGS dopo il riavvio (flip model), il preset di rete su schede Intel e Realtek, la tab Collegamenti (NVIDIA, Radeon, chipset Intel). Windows in italiano non è stato provato: rischio basso, perché tutte le letture passano da powrprof.
- Come rifare i test in VM: vedi la memoria `vm-testing-hyperv`.

## Stato al 14/10/2026, mattina: v1.5.0 rilasciata
- Unite in `main` la PR #20 (correzioni dai test in VM) e la PR #21 (versione 1.5.0: tolti `VersionSuffix` e l'avviso beta dal README). Tag `v1.5.0` pubblicato: release stabile "Latest" con installer, portable e SHA-256. Il workflow ha inviato l'aggiornamento a WinGet.
- Chi ha la 1.5.0-beta.1 riceve la 1.5.0 dall'updater, perché il canale beta accetta anche le release stabili.
- Ancora da provare su hardware reale: HAGS, il preset di rete su Intel e Realtek, la tab Collegamenti. Eventuali correzioni andranno in una 1.5.1.

## Feedback tester del 17/10/2026: dispositivi e hub USB
- Report locale: `C:\Users\Lorenzo\Downloads\TweakHub-Hardware-20261017-145402-dd0d104c.json`; screenshot: `C:\Users\Lorenzo\Pictures\Screenshots\Screenshot 2026-10-17 145856.png`. Non aggiungere il report integrale al repository.
- PC del report: ASUS TUF GAMING B850-PLUS WIFI, Ryzen 7 7800X3D, RX 7900 XT. 360 voci PnP: 183 presenti/OK, 1 presente/Degraded senza codice problema, 9 disattivate (22), 167 non presenti (45). Le ultime sono lo storico di Windows, non 167 guasti né opportunità di ridurre la latenza.
- Nel report sono presenti gli hub ASMedia `VID_174C&PID_2074` e `VID_174C&PID_3074` (USB 2 / SuperSpeed). Lo screenshot mostra due `ASM107x` con un avviso USB 3 → USB 2: senza instance ID e descrittori non si può provare che screenshot e report mostrino gli stessi nodi.
- Correzioni locali: presenza/codice problema letti anche da DEVPKEY con fallback CIM; gli assenti non sono suggeriti e non hanno il pulsante di disabilitazione attivo; indicazioni conservative per dispositivi non valutati e hub; stato/codice PnP visibili nei dettagli.
- Collegamenti: lettura dei descrittori USB produttore/prodotto via hub IOCTL, fallback al nome PnP; gli hub non ricevono più l'avviso generico di periferica su porta lenta, perché USB 3 espone anche un ramo USB 2. Non dedurre il marchio commerciale di un controller RGB dal chip dell'hub. Le ventole/LED su header PWM/ARGB non sono necessariamente dispositivi PnP separati; un controller interno, invece, può usare USB.
- Limite attuale: senza correlare le porte companion USB 2/3, non si diagnostica automaticamente neppure un hub USB 3 realmente collegato a USB 2; restano gli avvisi per storage/video a valle.
- Verifiche locali: build Release con 0 warning/errori e 256/256 test passati. Smoke test in sola lettura sul PC di Lorenzo: 266 voci, 221 presenti (conteggio uguale a `Get-PnpDevice -PresentOnly`); velocità lette su 11/11 porte USB, produttore su 6 e prodotto su 9. Test temporaneo rimosso; restano i test di regressione deterministici.
- Da verificare sul PC del tester: nuova scansione e confronto dei nomi con CPU Direct di Marius Heier, screenshot dei controller RGB effettivi e dei loro figli. Il report attuale non contiene i descrittori USB né la velocità negoziata: non basta a confermare il nome commerciale o un cablaggio errato.

## Richiesta di Lorenzo: sezione Affinity per dispositivi
- Aggiungere una sezione dedicata per assegnare l'affinity dei dispositivi di Gestione dispositivi a CPU specifiche (interrupt affinity, non affinity dei processi).
- Esiste già un editor in `Views/InterruptView.xaml(.cs)` con `Services/InterruptService.cs`: partire da quello, senza duplicare la logica. Attualmente considera i dispositivi PCI e usa una maschera a 64 bit; non estendere ciecamente a USB/HID, i cui interrupt possono appartenere al controller host condiviso.
- Prima dell'implementazione definire core fisici/thread SMT, CPU group oltre 64 processori, valori preesistenti e ripristino del default. Conservare backup, elevazione, avviso di riavvio e limiti del driver; nessuna promessa automatica di miglioramento della latenza.
- Solo annotata in questa sessione: nessuna nuova sezione affinity implementata.

## Richiesta del 17/10/2026 — Tron in Sicurezza
- Aggiungere un tasto/card dedicato per scaricare ed eseguire **Tron**: https://github.com/bmrf/tron. Non già presente nel codice. Requisiti e cautele aggiunti a [security-section.md](security-section.md).
- Importante: il README ufficiale richiede il pacchetto completo dai riferimenti ufficiali di r/TronScript, non lo ZIP dei sorgenti GitHub. Verificare integrità, compatibilità Windows 11 e licenze prima di implementare il download.
- Esecuzione soltanto su conferma esplicita, elevata e con console/disclaimer visibili; niente autorun, consenso EULA implicito o upload dei log. Avvisare che può rimuovere file/software e alterare impostazioni non ripristinabili dai backup di TweakHub, inclusi piani energetici/pagefile. Test solo in VM sacrificabile.
- Solo pianificato: Tron non è stato scaricato come pacchetto né eseguito; è stato consultato il README ufficiale.

## Richiesta del 17/10/2026 — tweak dagli screenshot BXTool
- Esaminati tutti i 21 screenshot in `C:\Users\Lorenzo\Pictures\Screenshots\bxtool`. Inventario, confronto con il codice e motivi di esclusione/sospensione in [bxtool-tweaks.md](bxtool-tweaks.md).
- Non implementare ricette desunte dai soli nomi: le immagini non mostrano percorsi/tipi/valori del registro e vari preset non sono neppure semplici registry tweaks.
- Non duplicare foreground priority, core parking, fullscreen optimization, accelerazione mouse, funzioni già presenti del preset di rete e pulizie. Non aggiungere come boost `LargeSystemCache`, `DisablePagingExecutive`, disattivazione autotuning/HPET o code input aggressive: alcuni contraddicono proprio Verifica tweak. Proposta di esclusione anche per la disattivazione UAC.
- Decisione di Lorenzo: **aggiungere solo gestione ibernazione e disattivazione opzionale del Power Throttling globale**. Esclusa la priorità per-app, inclusi i toggle per singolo gioco. Entrambe le opzioni devono usare backup/ripristino dello stato originale ed elevazione, senza servizi residenti; chiarire che le modifiche restano dopo la chiusura della copia portable e che cancellare la cartella non ripristina Windows.
- Ibernazione: usare `powercfg`, distinguere da avvio rapido, conservare lo stato originale e considerare tipo di hiberfile e funzionalità dipendenti. Power Throttling globale: distinto dallo stato minimo CPU del piano, opzionale e non consigliato automaticamente; indicare consumi/temperature e non promettere un aumento FPS.
- Preset CPU/NTFS/MMCSS/rete/NVIDIA sconosciuti: **saltati**, non ricostruirli per ipotesi. Eventuali nuove valutazioni richiedono sorgenti o elenco verificabile delle modifiche. Nessuno dei due nuovi tweak è stato ancora implementato o applicato al PC: questa sezione registra il lavoro approvato da svolgere.

## Richiesta del 17/10/2026 — servizi Windows non necessari all'utente
- Aggiungere una gestione **selettiva e reversibile** dei servizi non utilizzati, con spiegazione di funzione, dipendenze e conseguenze. Riferimenti: `C:\Users\Lorenzo\Pictures\Screenshots\Screenshot 2026-10-17 161630.png` e https://github.com/ChrisTitusTech/winutil. Non proporre un «disabilita tutti» né dichiararli inutili per ogni PC.
- Nello screenshot compaiono questi servizi; valutazioni preliminari, da verificare sulla build Windows supportata prima dell'implementazione:

| Servizio | Indicazione per il piano |
|---|---|
| `SysMain` | Tweak già presente (`disable_sysmain`): non duplicare. Non considerarlo inutile solo perché Windows è su SSD; lasciarlo attivo salvo problema diagnosticato. |
| `MapsBroker` | Gestione mappe scaricate/offline: candidato opzionale solo se non usate. Controllare la modalità attuale e l'avvio su trigger; se è già fermo non promettere risparmi di CPU/RAM. |
| `RetailDemo` | Modalità dimostrativa dei PC da esposizione: candidato se presente e non utilizzato; nessuna azione se assente o già disattivato. |
| `DiagTrack` | Già incluso in `disable_telemetry`: condividere logica e backup, senza due controlli in conflitto. Esplicitare gli effetti sulla diagnostica. |
| `dmwappushservice` | Instradamento messaggi push WAP per gestione dispositivi: non descriverlo semplicemente come telemetria. Valutare MDM/gestione aziendale, non disattivarlo automaticamente. |
| `WbioSrvc` | Servizio biometrico: lasciarlo attivo se si usano impronta/riconoscimento facciale. Eventuale scelta esplicita per chi non usa biometria, non preset generico. |
| `WerSvc` | Segnalazione errori Windows: conservarlo per diagnosi di crash. La disattivazione non è un boost generale e contrasterebbe con l'obiettivo diagnostico di Verifica tweak. |
| `PcaSvc` | Assistente compatibilità programmi: può servire ad app e installer datati. Solo eventuale opzione avanzata con avviso, non disabilitazione consigliata a tutti. |

- Consultato in sola lettura il [config/tweaks.json ufficiale di WinUtil](https://github.com/ChrisTitusTech/winutil/blob/main/config/tweaks.json): il preset `WPFTweaksServices` («Services - Set to Manual») non mette tutto in Manuale; disabilita anche alcuni servizi e modifica `SvcHostSplitThresholdInKB` in funzione della RAM. **Non copiarlo integralmente:** il cambio di soglia contraddice il controllo `SvcHostSplit` di TweakHub. Anche `CscService` e `SharedAccess` toccati dal preset hanno funzioni reali (file offline e condivisione connessione/hotspot).
- Riutilizzare la gestione servizi di `SystemTriageService` (già imposta Manuale e ripristina Automatico/Automatico ritardato) e i tweak esistenti, senza duplicare le modifiche. Distinguere chiaramente **Manuale**, **Disabilitato** e **Arresta ora**: Manuale non impedisce l'avvio su richiesta e cambiare il tipo di avvio non arresta un servizio già in esecuzione.
- Prima di cambiare un servizio salvare lo stato originale necessario al ripristino, inclusi tipo di avvio, avvio ritardato e stato di esecuzione; non cancellare trigger o configurazioni. Gestire servizi mancanti/protetti e dipendenze, UAC e ripristino anche dopo il riavvio dell'app portable. Non interrompere forzatamente servizi dipendenti.
- Test di applicazione/ripristino in VM prima del rilascio. Nessun servizio modificato e nessuno script WinUtil eseguito durante lo sviluppo.

## Implementazione successiva al piano — stato corrente
Questa sezione aggiorna le precedenti note «solo pianificato/non implementato»: il codice delle funzioni approvate è ora presente, ma **non è stata pubblicata una release**.

- **Ibernazione:** nuovo `HibernationService` e tweak avanzato. Lettura del tipo effettivo via `GetPwrCapabilities`, dimensione da registro, modifica solo tramite `powercfg`; backup persistente prima dell'azione, ripristino di file full/reduced e dimensione originale. Nessuna configurazione indovinata se non leggibile; se già disabilitata senza backup il controllo è indisponibile. Integrazione con ripristino tweak, playbook e importazione dati di una copia precedente.
- **Power Throttling globale:** nuovo tweak CPU opzionale con `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff = 1`, backup/ripristino standard e avviso riavvio. Non cambia protezione termica o stato minimo del piano. Come ibernazione, escluso dai consigliati automatici.
- **Servizi opzionali:** scheda in Sicurezza con tutti gli 8 servizi dello screenshot, spiegazioni, Manuale/Disabilitato e ripristino del tipo originale (incluso ritardo di avvio). Nessun arresto né modifica dei trigger; stato d'esecuzione non forzato nemmeno al ripristino. SysMain/DiagTrack rinviano ai tweak esistenti, WerSvc mantenuto per diagnostica. Backup distinti e blocco contro modifiche concorrenti nella sezione Sistema/Avvio. Nuovi file undo `hibernation-backup.json` e `optional-services.json` inclusi nell'importazione da altra copia.
- **Affinity:** scheda dedicata in Dispositivi, riusando `InterruptView`/`InterruptService`; selezione delle CPU logiche con core fisico per identificare thread SMT, ritorno all'assegnazione automatica e ripristino indipendente da MSI/priorità. Nuovo `CpuTopology` tramite API Windows. Solo dispositivi PCI; USB/HID restano sotto il controller host. PC con più CPU group o topologia sconosciuta: nessuna maschera troncata, selezione manuale bloccata. Anche i playbook rifiutano maschere per CPU non valide sul PC di destinazione.
- **Tron:** card in Sicurezza → Strumenti con tre azioni confermate (download, estrazione, avvio), pacchetto completo da bmrf.org, controllo dimensione/SHA-256 e file estratti, console elevata/disclaimer e opzioni conservative `-p -spr -m -sor -se -scs`. Niente autorun, consenso EULA implicito, invio log o override OS. Blocco operazioni sovrapposte e controllo del processo già avviato anche riaprendo TweakHub. Il codice non certifica la sicurezza del publisher e TweakHub non annulla la pulizia Tron.
- **Verifiche:** build Release con 0 warning/errori, 283/283 test passati; incluse sei lingue, costruzione delle nuove viste WPF senza caricarle a schermo, topologia CPU reale in sola lettura, script PowerShell analizzati senza eseguirli, backup/ripristino simulati, migrazione dati e download/verifica Tron con HTTP fittizio e byte inerti.
- **Da fare prima del rilascio:** VM per ibernazione full/reduced con dimensione personalizzata, avvio rapido/sospensione ibrida e ripristino dopo riavvio; Power Throttling con valore originale assente/0/1 e UAC rifiutata; servizi mancanti/protetti/già fermi e ripristino automatico ritardato; affinity applicata dopo riavvio, MSI invariato, CPU group multipli e driver che ignorano la policy. Per Tron provare il pacchetto reale, autoestraente e formato manifesto su VM sacrificabile (anche utente standard e percorsi con spazi). Il comportamento end-to-end del pacchetto esterno non è ancora collaudato.
- Nessuna modifica reale a ibernazione, servizi, Power Throttling o interrupt di Windows durante lo sviluppo. Nessun pacchetto Tron vero scaricato/eseguito; consultati soltanto sorgenti/documentazione/manifesto del publisher. Nessuna priorità per-app o preset sconosciuto aggiunto.

## 13/11/2026 — ricerca GPU App Prioritization
- Issue di ricerca: https://github.com/PrimeBuild-pc/TweakHub/issues/34. Possibile futura voce nella sezione GPU; nessuna implementazione approvata o realizzata per ora.
- NVIDIA documenta il bilanciamento GPU e il requisito HAGS per RTX Spark; la disponibilità su GeForce è segnalata solo su alcuni sistemi. Verificare capacità effettiva, API supportata, lettura/scrittura e ripristino prima di proporre il tweak. Il solo riconoscimento NVIDIA non basta; eventuali equivalenti AMD/Intel vanno valutati separatamente.
- Il tweak esistente `optimize_flip_model` imposta `HwSchMode`, ma non la ripartizione di priorità. Non applicare chiavi ipotetiche né attivare HAGS automaticamente. La issue contiene fonti, criteri e test, inclusa verifica su hardware fisico compatibile e benchmark foreground/background.

## 15/11/2026 — rimozione dispositivi fantasma
- Issue promemoria: https://github.com/PrimeBuild-pc/TweakHub/issues/35. Nessuna implementazione approvata o realizzata per ora.
- Già presente: `DeviceService` legge `DEVPKEY_Device_IsPresent` e la pagina Dispositivi mostra i non presenti; in Strumenti c'è solo il link esterno a GhostBuster (MajorGeeks).
- Origine: richiesta upstream https://github.com/lostindark/driverstoreExplorer/issues/345 (il maintainer ha rimandato a un tool esterno).
- Riferimenti: Device Cleanup Tool di Uwe Sieber https://www.uwe-sieber.de/misc_tools_e.html#devicecleanup ; Windows Maintenance Tool https://github.com/ios12checker/Windows-Maintenance-Tool (script PowerShell: `pnputil /remove-device` sui dispositivi con Status = Unknown).
