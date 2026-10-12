# Piano implementativo — sezione Dispositivi e latenza

Obiettivo: nuova voce **Dispositivi e latenza** nella sidebar che integra in TweakHub, in modo nativo e reversibile, l'analisi dei dispositivi da disabilitare (spunto: script Device-Analyzer v1) e gli strumenti di latenza/input indicati da Lorenzo. Stile WinUI come la sezione Sicurezza (tab, card, grafici interattivi).

## Tab

| Tab | Contenuto | Fonte / ispirazione | Integrazione |
|---|---|---|---|
| Dispositivi | Inventario PnP (presenti, non presenti, disabilitati, problemi, bus senza figli), regole commentate per i dispositivi spesso disabilitati (cosa smette di funzionare), disattiva/riattiva con elenco di quelli disattivati da TweakHub e "riattiva tutti", grafico per categoria/classe, export CSV | Device-Analyzer v1 | Nativa |
| Interrupt e MSI | Dispositivi PCI: modalità MSI, limite messaggi, priorità, affinità su core; modifiche via RegistryService con backup e ripristino; riavvio richiesto | GoInterruptPolicy (MIT) | Nativa |
| USB e input | Catena dispositivo → hub → controller per mouse/tastiere/controller, MSI del controller, selective suspend (piano e per dispositivo) con correzione, test polling rate del mouse con Raw Input e istogramma | CPU Direct / DeepPoll (Marius Heier, MIT) | Nativa |
| Collegamenti | Sola lettura: linee e generazione PCIe di GPU, SSD NVMe e schede (attuale @ massimo come GPU-Z, link reale delle Radeon da ADLX), CPU o chipset, banda del collegamento CPU–chipset, velocità USB negoziata, tabella di 183 schede madri con slot e linee condivise; badge verde/giallo/rosso/grigio | Piano `pcie-topology.md` | Nativa |
| Verifica tweak | Controllo dei tweak dannosi diffusi (bcdedit timer, mitigazioni Spectre disattivate, code mouse/tastiera, MPO, DPC, ecc.) con spiegazione e ripristino al default; i valori gestiti da TweakHub sono segnalati come tali | RepairBadTweaks (MIT) | Nativa |
| Strumenti e risorse | Card categoria "Latency & Input": GoInterruptPolicy, strumenti Marius Heier, hidusbf, BattleBeaver overclocking, PowerPlanSettingsEditor, guida Advanced Power Options, RepairBadTweaks | — | Link |

## Vincoli
- Lettura sempre senza privilegi dove possibile; ogni modifica passa da UAC con conferma che spiega cosa smette di funzionare.
- Nessun download o esecuzione di script remoti (`irm | iex`): le funzioni utili sono reimplementate.
- Le modifiche al registro passano da `RegistryService` (backup prima della scrittura, verifica, ripristino).
- Stringhe nel dominio `Devices` nelle 6 lingue; logica testata in `TweakHub.Tests`.

## Stato
- [x] Sezione, tab Strumenti e risorse, catalogo "Latency & Input"
- [x] Dispositivi
- [x] Interrupt e MSI
- [x] USB e input
- [x] Verifica tweak
- [x] Collegamenti (PCIe, chipset, USB, schede madri) — vedi `pcie-topology.md`

## Note
- Il nome dei dispositivi USB viene da DEVPKEY_Device_BusReportedDeviceDesc; gli hub sono i nodi con driver usbhub/usbhub3 (i nodi compositi usbccgp non contano).
- Il selective suspend del piano usa i GUID del sottogruppo USB: alcuni piani personalizzati non definiscono gli alias SUB_USB/USBSELECTIVESUSPEND.
- Script di Marius Heier (CPU Direct, DeepPoll, DeepLog, USB Rig Profiles): card nella tab Strumenti, eseguiti come da documentazione dell'autore (`irm | iex`) in una finestra PowerShell amministratore, con avviso in primo piano alla prima esecuzione (consenso in `third-party-consent.json`).
- Sezione **Piano energetico**: editor di tutte le impostazioni (anche nascoste) via API powrprof, confronto con un altro piano, gestione piani (attiva, duplica, rinomina, esporta .pow con UAC, importa, elimina) e scelta del piano per il tweak Prestazioni elevate (spostata da Registry Tweaks).
- Avvio: splash che precarica tweak, Windows Update, piani, sicurezza, dispositivi, interrupt e verifica tweak (`Services/Preload.cs`); le sezioni restano in memoria; overlay di caricamento nelle viste che rileggono il sistema.
- Playbook: "Crea da questo PC" rileva piano energetico, tweak attivi, valori interrupt/USB scritti da TweakHub, dispositivi disattivati e app preferite; import/export di un singolo playbook con i tweak e script personalizzati collegati; riavvio come amministratore per una sola richiesta UAC. Esclusi: script di terze parti (interattivi), correzioni della Verifica tweak (riparano solo il PC attuale).
- Possibili sviluppi: rimozione dei dispositivi fantasma con pnputil, ripristino delle modifiche interrupt anche da "Ripristina tutto" di Registry Tweaks.
