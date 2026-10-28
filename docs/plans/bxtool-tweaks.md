# Valutazione opzioni BXTool — 17/10/2026

Richiesta di Lorenzo: considerare i tweak mostrati in `C:\Users\Lorenzo\Pictures\Screenshots\bxtool` (21 immagini, versioni BXTool 4.0.10 e 4.0.6). Gli screenshot sono stati letti tutti; molte immagini ripetono le stesse schermate con interruttori On/Off.

**Decisione di Lorenzo: implementati gestione ibernazione e Power Throttling globale opzionale. Priorità per-app esclusa; preset sconosciuti saltati. Entrambi esclusi da «Applica tutti i consigliati»; collaudo delle modifiche reali in VM ancora necessario.** Le immagini mostrano etichette e descrizioni, non percorsi, tipi e valori del registro né tutte le operazioni dei preset. Non inferire una ricetta tecnica dal solo nome commerciale. Lo stato On nello screenshot non dimostra efficacia o sicurezza.

## Già coperti o parzialmente sovrapposti

| Opzioni BXTool | Riscontro in TweakHub / decisione proposta |
|---|---|
| Windows Priority | `cpu_priority_separation`: `Win32PrioritySeparation = 0x26`. Stesso intento foreground/background, non equivalenza verificata con i valori BXTool. Non duplicare. |
| CPU Tweaks | Piano energetico, stato minimo CPU e core parking già gestiti da `TweakService` e `PowerService`. Confrontare prima le singole impostazioni, non aggiungere un secondo pacchetto opaco. |
| Power plan BXTool / Light Gaming / Unpark (extreme) | Editor/importazione piani e `disable_core_parking` già presenti. I primi due piani non sono ricostruibili dalle immagini; non inventarne i valori né forzare tutti i risparmi energetici a Off. |
| Network Powersaving / Network Optimization | `optimize_network_for_gaming` e `NetworkAdapterService` coprono interrupt moderation e risparmi energetici dichiarati dai driver Intel/Realtek Ethernet e Intel Wi-Fi. Copertura parziale, non prova di identità con BXTool. |
| Network priority | Potenziale sovrapposizione con `NetworkThrottlingIndex`; senza chiavi non sappiamo se modifica anche MMCSS/QoS/priorità dei processi. Sospendere. |
| FullScreen Optimization | `disable_fullscreen_optimizations` già presente; preservare l'interazione con `optimize_flip_model`. |
| Mouse Optimization | Disattivazione accelerazione mouse già presente. Le eventuali altre modifiche di BXTool sono sconosciute. |
| Composite Bus Enumerator / Microsoft V. Drive Enumerator | Regole `CompositeBus` e `VirtualDrive` in `DeviceService`, con rischi e funzioni dipendenti. Tenere nella sezione Dispositivi, non creare interruttori registro duplicati. |
| Automatic Affinity Core | Parzialmente sovrapposto all'editor Interrupt/MSI e alla nuova richiesta Affinity nell'handoff. Non assegnare core automaticamente senza conoscere topologia, SMT e CPU group. |
| System Cleaner | Script `temp-cleanup` e `component-cleanup` già presenti. Non estendere la cancellazione a file genericamente chiamati «inutili». |
| RAM Cleanup | `empty-standby-list` già presente come azione manuale una tantum. Non trasformarlo in pulizia periodica: la standby list è cache riutilizzabile. |

## Da non importare come ottimizzazioni generiche

| Opzioni BXTool | Motivazione / azione proposta |
|---|---|
| LargeSystemCache | `TweakCheckService` segnala già il valore 1 e propone 0. Aggiungerlo come boost gaming contraddirebbe il controllo esistente. |
| Paging Executive Optimization | Se corrisponde a `DisablePagingExecutive = 1`, è già segnalato da Verifica tweak. Non importare come boost generale. |
| Page Limit Optimization | Se usa `IoPageLockLimit`, è già coperto da Verifica tweak; lo screenshot non conferma la chiave. Nessun valore arbitrario. |
| Page Optimization | La descrizione parla di pagine grandi ma non identifica la modifica. Non confondere dimensioni delle pagine, pagefile e privilegi delle applicazioni. Nessun boost universale dimostrato dalle immagini. |
| Clear Page Optimization | Pacchetto non specificato, cita `ClearPageFileAtShutdown` insieme ad altre modifiche. La pulizia del pagefile allo spegnimento è una scelta di sicurezza, non di FPS, e può rallentare lo spegnimento. Separare gli eventuali scopi prima di decidere. |
| Disable autotuning | Verifica tweak propone già il ritorno a `normal`. Non aggiungere un preset che può ridurre il throughput TCP senza un problema diagnosticato. |
| High precision event timer | La regola `Hpet` è già «lascia attivo»; Verifica tweak gestisce gli override BCD dei timer. Non proporre la disattivazione indiscriminata. |
| Disable Administrator Request | Disattivare UAC non è un'ottimizzazione delle prestazioni: riduce le protezioni. Proposta: escluderlo dal catalogo. |
| Keyboard Optimization | Lo screenshot lo definisce aggressivo ma non mostra cosa cambia. Se riduce `KeyboardDataQueueSize`, confligge con Verifica tweak e può perdere input. Non aggiungere valori presunti. |
| Cfg Optimization | La descrizione dice «Enables Control Flow Guard». Non assumere che disabiliti CFG; non toccare le mitigazioni per ricopiare un preset non documentato. Lasciare la gestione di sicurezza a Windows/Exploit Protection. |
| Legacy device | Nome insufficiente per individuare un dispositivo o un driver. Richiedere instance/hardware ID e funzione; nessuna disabilitazione per semplice corrispondenza del nome. |
| Microsoft GS Wavetable Synth | Sintetizzatore MIDI: la disattivazione può togliere funzionalità MIDI, senza beneficio gaming dimostrato dalle immagini. Non proporla come ottimizzazione standard. |

## Valutazione iniziale dei candidati (decisione finale sotto)

| Opzioni BXTool | Proposta e informazioni mancanti |
|---|---|
| Discord Optimization | Valutare una priorità per-app opzionale, separata dalla priorità degli interrupt. Abbassarla può peggiorare audio/condivisione sotto carico: nessun default automatico. Servono operazione reale e target eseguibile. |
| OBS Priority / TikTok Live Studio Priority | Preferire le opzioni native dell'app quando disponibili. Valutare un solo controllo riusabile di priorità per-app anziché un toggle copiato per ogni programma. |
| Fortnite / Valorant / COD / CS:GO 2 / Minecraft / RainbowSix / GTA V-FiveM / Rocket League / Black Myth Wukong / Elden Ring Priority | Un unico candidato «priorità per-app», non dieci promesse di aumento FPS. Identificare gli eseguibili corretti, non launcher o runtime condivisi (es. tutti i `javaw.exe`); considerare anticheat. Evitare Realtime, backup/ripristino dei valori originali, test prima del rilascio. La descrizione CS2 cita anche impostazioni personalizzate sconosciute: non includerle implicitamente. |
| CPU Power Throttling | **Non è automaticamente già coperto** da `disable_cpu_throttling`: quest'ultimo cambia lo stato minimo CPU del piano su alimentazione AC. Se BXTool modifica il criterio globale `PowerThrottlingOff`, valutarlo come scelta distinta con impatto su consumi/temperatura, non applicarlo per supposizione. |
| Disable Hibernation | Candidato come opzione di gestione spazio/funzionalità, non boost FPS. È diverso da `disable_fast_startup`: disattivare l'ibernazione tocca anche avvio rapido e sospensione ibrida. Usare l'interfaccia Windows supportata (`powercfg`) e progettare il ripristino dello stato originale, non scrivere semplicemente `HibernateEnabled`. |
| Increase System Responsiveness / Priority System Profile / Audio Priority | Potrebbero toccare MMCSS/SystemProfile, ma non sappiamo chiavi e valori. Valutare solo con ricetta esplicita e verifica della documentazione Microsoft; non promettere che un valore apparentemente «più alto/più basso» migliori audio o latenza. |
| CPU Kernel Optimization / Advanced Priority | Preset opachi. Servono tutte le modifiche; evitare priorità forzate di processi critici, già parzialmente controllate da Verifica tweak (`CsrssPriority`, `ThreadDpc`). |
| CPU NTFS Optimization | Non indica se cambia last-access, nomi 8.3 o altro. Servono comandi/chiavi e motivazione; possibili effetti su backup e compatibilità, non aggiungere un pacchetto generico. |
| CPU Alimentation | «Avviare il servizio CPU al boot» non identifica un servizio Windows. Richiedere il nome del servizio e l'operazione esatta. |
| Network Base Optimization / UDP Optimization / Network Debloat | Formulazioni insufficienti. Richiedere elenco completo: non disabilitare protocolli, QoS o componenti di rete alla cieca; distinguere bufferbloat sul router da impostazioni locali. |
| NVIDIA Driver Priority / NVIDIA performance pack | Preemption, priorità ed energia del driver sono modifiche diverse, spesso dipendenti dalla versione. Servono elenco valori/API e compatibilità. Non usare chiavi non documentate come preset generico. |
| NVIDIA Normal / Performance + Boost / Quality + Boost / Disable GPU PowerSaving | Non sono necessariamente registry tweaks. Valutare separatamente profili driver tramite interfacce supportate, conservando i profili originali. Forzare energia massima può aumentare consumi e temperature; niente deduzioni dalle sole etichette. |

## Condizioni per il prossimo passo

1. Implementati in `HibernationService` e nel catalogo `TweakService`: gestione ibernazione e disattivazione opzionale del Power Throttling globale, approvate da Lorenzo. Escludere la priorità per-app e i toggle di priorità per singolo gioco. Non richiedere installazione o servizi residenti; spiegare che le modifiche persistono anche chiudendo l'app portable e che servono i backup per ripristinarle.
2. Saltare i preset sconosciuti: niente ricostruzioni per ipotesi. Riaprire la valutazione solo con sorgenti/script ufficiali o un elenco verificabile delle modifiche. Non eseguire BXTool sul PC principale soltanto per scoprirle.
3. Verificare ogni modifica contro documentazione primaria e controlli già presenti; se inefficace, ridondante o rischiosa, documentarne l'esclusione. Nessuna ricetta inventata per completare la lista.
4. Per i soli candidati approvati: integrazione nel catalogo esistente, descrizioni oneste, backup/ripristino, UAC quando necessario, riavvio dove richiesto, risorse in tutte le sei lingue e test di applicazione/ripristino. Non cambiare il registro del PC durante l'analisi.
