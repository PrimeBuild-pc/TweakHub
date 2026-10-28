# Piano implementativo — sezione Sicurezza

Obiettivo: la voce **Sicurezza** della sidebar non è un elenco di link, ma integra in TweakHub le funzioni utili degli strumenti OSINT/cybersecurity del catalogo, con funzionalità native di Windows (Defender, PowerShell, Authenticode). Nessun codice o binario di terze parti incluso. Gli strumenti non integrabili restano card che aprono la pagina ufficiale (o installano via winget).

Stile: fedele a WinUI/Windows 11 (ModernWpf, `CardStyle`, Segoe Fluent Icons, brush tematici), grafici di stato dove utili. L'app gira `asInvoker`: le azioni privilegiate passano da `PowerShellService.ExecuteScriptAsync(..., requireAdministrator: true)` con prompt UAC e icona scudo.

## Struttura (`Views/SecurityPage`, tab)

| Tab | Contenuto | Strumenti sostituiti/integrati | Fase |
|---|---|---|---|
| Panoramica | Punteggio (anello segmentato interattivo) + checklist con esito, "Correggi" (elevato, con conferma) e "Apri impostazioni" | Personal Security Checklist, WPD (parz.) | 1 |
| Defender | Stato protezione, firme, ultime scansioni; scansione rapida/completa/offline, aggiorna firme; storico minacce; Safety Scanner scaricato dal sito Microsoft e avviato solo rilevamento (`msert /N`) | Microsoft Safety Scanner | 1 |
| Analisi file | Drag&drop: SHA-256/SHA-1/MD5, firma Authenticode, editore, versione; ricerca hash (non upload) su VirusTotal/Triage | Sigcheck, Triage | 1 |
| Sistema | Connessioni attive per processo (firma exe), avvio automatico/persistenza con evidenza non firmati, report esportabile | TapMap, SecurityToolkit | 2 |
| Privacy e account | Password compromessa (Pwned Passwords k-anonymity, solo su richiesta), ricerca eliminazione account (JSON JustDeleteMe MIT, scaricato su richiesta), DNS filtrante/Pi-hole con ripristino, metadati via ExifTool | EmailOSINT, JustDeleteMe, Pi-hole, ExifTool | 3 |
| Strumenti e risorse | Card esistenti `ExternalToolsPage(securityOnly)` (link/winget) | ffuf, Xalgorix, hackingtool, raccolte link, ungoogled-chromium, privacy.sexy | 1 |

## Vincoli
- Funzioni di rete solo su azione esplicita dell'utente; mai upload di file.
- Ogni "Correggi" mostra cosa cambia e se serve riavvio; BitLocker/Secure Boot non si modificano automaticamente (solo impostazioni/istruzioni).
- Stringhe nel dominio di risorse `Security` in tutte e 6 le lingue (test `LocalizationTests`).
- Logica di parsing/punteggio in `Services/SecurityService.cs`, coperta da `TweakHub.Tests/SecurityServiceTests.cs`.

## Stato
- [x] Catalogo link OSINT & Security (21 riferimenti) e voce sidebar
- [x] Fase 1: Panoramica, Defender, Analisi file, Strumenti e risorse (2026-10-11)
- [x] Fase 2: Sistema (2026-10-11)
- [x] Fase 3: Privacy e account (2026-10-11)
- [x] Revisione "Follow the White Rabbit" (394 link, 2026-10-12): integrati la ricerca hash su MalwareBazaar (Analisi file) e il controllo dell'IP remoto su AbuseIPDB (Sistema, solo per gli indirizzi pubblici); aggiunte le schede Have I Been Pwned, Cover Your Tracks, Privacy Guides, urlscan.io e Nuclei. Esclusi di proposito: strumenti di sorveglianza e intrusione (webcam esposte, IP logger, phishing per la posizione), database di credenziali rubate, data broker di persone, materiale formativo e link locali polacchi.

## Richiesta del 17/10/2026 — Tron (codice implementato, collaudo VM pendente)

- Aggiungere in Sicurezza una card dedicata per **scaricare e avviare Tron**, progetto ufficiale: https://github.com/bmrf/tron. Non presente al momento della richiesta; ora integrata in `Views/SecurityPage` e `Services/TronService.cs`, con download, estrazione e avvio separati e confermati.
- Il [README ufficiale](https://github.com/bmrf/tron/blob/master/README.md), consultato durante l'analisi, avverte che il download del solo repository GitHub non funziona: serve il pacchetto completo (`tron.bat` e `resources`), distribuito tramite i riferimenti ufficiali di r/TronScript. Prima dell'integrazione verificare fonte di distribuzione, versione, integrità e supporto della build Windows 11 corrente; non aggirare il controllo OS con `-dev`.
- Distinguere download/preparazione da esecuzione: avvio elevato solo dopo conferma esplicita, console e disclaimer visibili. Mai eseguirlo durante la scansione Sicurezza, all'apertura della pagina o dentro «Correggi tutto»; niente modalità automatica `-a`/`-asm` o accettazione EULA implicita.
- Tron non è uno scanner in sola lettura: può eliminare file/software, modificare impostazioni, resettare piani energetici/pagefile ed eseguire operazioni di riparazione. Mostrare questi effetti, durata potenzialmente di ore, necessità di backup e indicazioni ufficiali sul riavvio. I backup dei tweak TweakHub non permettono di annullare tutte le azioni di Tron.
- Prima di fissare la riga di comando, valutare e spiegare le opzioni di conservazione di piani energetici e pagefile, coerentemente con quanto TweakHub gestisce già. Non scegliere silenziosamente una pulizia aggressiva.
- Nessun invio di log o report: non abilitare `-udl`/`-er`. Non distribuire binari di terzi nel progetto senza verificarne licenze e provenienza. Preferire i servizi di download/elevazione già esistenti; testare l'integrazione in VM sacrificabile, non sul PC principale.

### Stato dell'integrazione e collaudi rimanenti
- Implementati download del pacchetto completo da `https://bmrf.org/repos/tron/`, scelta versione dal manifesto SHA-256 del publisher, controllo dimensione/hash prima dell'estrazione e verifica dei file estratti prima dell'avvio. Nessun binario incluso nel repository e nessuna esecuzione su scansione/punteggio.
- Download in `Data/Tools/Tron` per la copia portable (directory dati utente nell'installata). Estrazione tramite l'autoestraente verificato, separata dall'avvio; console elevata con disclaimer e argomenti `-p -spr -m -sor -se -scs`. Controllati i default che abiliterebbero autorun/EULA automatica/upload/email/riavvio/override OS; nessun bypass.
- Il manifesto viene verificato via HTTPS, non tramite una verifica PGP indipendente: non presentare SHA-256 come certificazione di sicurezza. Formato del pacchetto sconosciuto, file modificati/extra eseguibili, link o manifesti incompleti fanno fallire l'avvio senza fallback insicuri.
- Implementata anche la scheda Servizi opzionali (`OptionalServicesView`, `OptionalServicesService`): configurazione selettiva dell'avvio, senza fermare i servizi né cambiare trigger, con backup persistente e ripristino. SysMain/DiagTrack restano nei tweak esistenti; WerSvc è solo informativo. Blocchi contro backup in conflitto con Sistema/Avvio.
- Test automatici con HTTP simulato e payload inerti, senza scaricare/eseguire il vero pacchetto. **Da fare in VM sacrificabile:** download reale, sintassi/struttura dell'autoestraente e del manifesto interno, percorso con spazi, utente standard/UAC rifiutata, console/disclaimer, processi già attivi, eventuale ripresa di Tron e ripristino servizi dopo riavvio. Compatibilità della versione Tron con la build Windows del tester da verificare; non forzare l'OS.

## Note emerse durante l'implementazione
- `PowerShellService`: gli script ora sono scritti in UTF-8 con BOM (PowerShell 5.1 leggeva i file senza BOM come ANSI e rovinava i percorsi non ASCII) e il processo non eredita più `PSModulePath` (se TweakHub è avviato da PowerShell 7, i moduli di sistema non si caricavano).
- Gli script che restituiscono JSON usano `PowerShellService.AsciiJson` e i valori interpolati passano da `PowerShellService.Literal` (che raddoppia anche gli apici tipografici).
- Il test DNS rileva le reti che intercettano la porta 53 (risposta da 192.0.2.1): in quel caso il test non è attendibile e il cambio DNS potrebbe non avere effetto.
- Sviluppi completati: attiva/disattiva degli elementi di avvio (StartupApproved come Gestione attività, attività pianificate, servizi impostati su Manuale con ripristino della modalità originale), ripristino anche dei DNS IPv6 statici. Safety Scanner esiste solo x86/x64: su ARM64 Windows 11 usa l'emulazione x64.
