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

## Note emerse durante l'implementazione
- `PowerShellService`: gli script ora sono scritti in UTF-8 con BOM (PowerShell 5.1 leggeva i file senza BOM come ANSI e rovinava i percorsi non ASCII) e il processo non eredita più `PSModulePath` (se TweakHub è avviato da PowerShell 7, i moduli di sistema non si caricavano).
- Gli script che restituiscono JSON usano `PowerShellService.AsciiJson` e i valori interpolati passano da `PowerShellService.Literal` (che raddoppia anche gli apici tipografici).
- Il test DNS rileva le reti che intercettano la porta 53 (risposta da 192.0.2.1): in quel caso il test non è attendibile e il cambio DNS potrebbe non avere effetto.
- Sviluppi completati: attiva/disattiva degli elementi di avvio (StartupApproved come Gestione attività, attività pianificate, servizi impostati su Manuale con ripristino della modalità originale), ripristino anche dei DNS IPv6 statici. Safety Scanner esiste solo x86/x64: su ARM64 Windows 11 usa l'emulazione x64.
