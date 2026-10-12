# Piano implementativo — tab Collegamenti (PCIe e USB)

Obiettivo: nuova tab **Collegamenti** nella sezione Dispositivi e latenza. Mostra, in sola lettura, se GPU, SSD NVMe, schede PCIe e periferiche USB stanno lavorando al massimo o se sono limitate da linee PCIe mancanti, slot sbagliati, chipset condiviso o porte USB lente. Nessuna correzione con un clic: l'utente sposta fisicamente i dispositivi e preme **Scan** per vedere il nuovo risultato.

## Cosa si può leggere e da dove (verificato sul PC di Lorenzo, MSI MPG X570 Gaming Edge WiFi)

| Dato | Fonte | Privilegi | Note |
|---|---|---|---|
| Link massimo e attuale (generazione e linee) dei dispositivi finali | `DEVPKEY_PciDevice_MaxLinkSpeed/Width`, `CurrentLinkSpeed/Width` | utente | Vuoti su root port e switch: Windows non espone le capacità dello slot. |
| Percorso fino al root port | `DEVPKEY_Device_Parent` (già nell'inventario) + `DEVPKEY_Device_LocationInfo` | utente | Serve per CPU/chipset e per contare i dispositivi dietro al chipset. |
| Link reale delle GPU AMD | ADLX (`amdadlx64.dll`, installata col driver): `IADLXGPU1::PCIBusType` e `PCIBusLaneWidth` | utente | Le Radeon RDNA hanno uno switch interno: il link letto da Windows è quello interno (sempre x16). Provato: 6950 XT → PCIe 4.0 x16. ADL classico (`Overdrive5_CurrentActivity`) non funziona su RDNA. |
| Velocità USB negoziata | `IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX(_V2)` sull'hub padre | utente | Dice se un dispositivo USB 3 sta andando a USB 2. |
| Scheda madre | `Win32_BaseBoard` (Manufacturer, Product) | utente | Chiave per la tabella delle schede. |

**PawnIO non serve e non basta:** i moduli ufficiali (PawnIO.Modules) non includono un accesso generico alla configurazione PCI e la versione firmata carica solo moduli firmati. Senza modulo non si leggono comunque le capacità dei root port. Possibile sviluppo futuro: contribuire un modulo `PciConfig` upstream; con quello si leggerebbero lo slot reale e lo stato del collegamento al chipset.

## CPU o chipset

- **AMD:** il dispositivo è sul chipset se nel percorso c'è uno switch del chipset: `1022:43B0/43B4` (300), `43C6/43C7` (400: B450, X470), `43E9/43EA` (500: B550, A520), `57AD/57A3/57A4` (X570), `43F4/43F5` (Promontory 21: B650, X670, B850, X870 e varianti E). Gli switch interni delle GPU (`1002:1478/1479`) non contano.
- **Intel:** il dispositivo è sul chipset se il root port ha un ID del PCH: `8086:06AC–06C3` (400), `43B0–43C7` (500), `7AB0–7ACB` (600), `7A30–7A4B` (700), `7F30–7F47` (800). Tutti gli altri root port sono della CPU (PEG `0:1.x`, M.2 CPU `0:6.x`, linee SoC di Arrow Lake).
- Fonte degli ID: pci.ids (pci-ids.ucw.cz). Un percorso che non arriva a un root port noto → "non determinato", mai un'ipotesi.

## Collegamento CPU–chipset (tabella fissa nel codice)

| Chipset | Collegamento | Note |
|---|---|---|
| B450 | PCIe 3.0 x4 | linee del chipset Gen2 |
| B550 | PCIe 3.0 x4 | |
| X570 | PCIe 4.0 x4 | Gen3 con Ryzen 2000 e APU |
| B650, B650E, X670, X670E, B850, B850E, X870, X870E | PCIe 4.0 x4 | X670/X670E/X870E: due chip in cascata sullo stesso collegamento |
| Z490, B460 | DMI 3.0 x4 | |
| Z590 | DMI 3.0 x8 con Core 11ª gen, x4 con 10ª | |
| B560 | DMI 3.0 x4 | |
| Z690, Z790, Z890 | DMI 4.0 x8 | |
| B660, B760, B860 | DMI 4.0 x4 | |

Il chipset si riconosce dal nome della scheda (regex sul nome del chipset) o, se manca, dalla famiglia degli ID qui sopra e dall'ID del controller eSPI/LPC Intel (`8086:0685` Z490, `A3C8` B460, `4385` Z590, `4387` B560, `7A84` Z690, `7A86` B660, `7A04` Z790, `7A06` B760, `7F04` Z890, `7F06` B860). Da verificare per ogni voce prima di scriverla.

## Controlli e badge

Verde = ok, giallo = da controllare o migliorabile, rosso = il dispositivo sta perdendo prestazioni per come è collegato, grigio = non verificabile.

| Controllo | Verde | Giallo | Rosso |
|---|---|---|---|
| GPU, linee | attuali = massime | slot elettrico più stretto della scheda (dalla tabella schede: "usa lo slot principale") | meno linee del massimo; oppure GPU collegata al chipset |
| GPU, generazione | attuale = massima, o uguale al limite dello slot noto | inferiore: "normale a riposo, apri un gioco e rifai lo scan; se resta, lo slot o la CPU limitano a GenX" | — |
| NVMe e schede PCIe | generazione e linee al massimo | generazione inferiore (es. SSD Gen4 in slot Gen3); sul chipset con un M.2 della CPU libero secondo la tabella | meno linee del massimo (x2 invece di x4) |
| Chipset | banda teorica dei dispositivi veloci ≤ collegamento | 2 o più dispositivi veloci (NVMe, rete ≥ 10 Gb, schede di acquisizione) che insieme superano il collegamento: "si dividono la banda se usati insieme" | — |
| USB | velocità giusta | mouse/tastiera dietro un hub; 2 o più dispositivi ad alta banda (archiviazione, videocamere) dietro lo stesso hub esterno. Non lo stesso controller: con xHCI ogni porta ha la sua banda | dispositivo USB 3 collegato a USB 2 (giallo se non è un dispositivo di archiviazione o video) |
| GPU dietro switch interno senza API del produttore (Intel Arc senza IGCL) | — | — | grigio "link reale non leggibile" |

Testo della scheda: per ogni dispositivo "attuale @ massimo" come GPU-Z (es. `PCIe 4.0 x16 @ 4.0 x8`), dove è collegato (CPU, chipset, controller USB), il badge e una frase su cosa fare. Il "chipset sovraccarico" è banda teorica, non traffico misurato: l'interfaccia lo dice.

Massimo delle GPU con switch interno: il link interno riporta sempre x16, quindi le schede più strette hanno il massimo da tabella (es. Navi 23 RX 6600/6650 x8, Navi 24 RX 6400/6500 XT x4, Navi 33 RX 7600 x8, Arc A380/B570/B580 x8). Da verificare voce per voce.

## Tabella delle schede madri

File `Resources/Boards.json` (risorsa incorporata). Per ogni scheda:

```json
{ "match": "MPG X570 GAMING EDGE WIFI", "chipset": "X570", "source": "https://…",
  "m2":    [ { "label": "M2_1", "source": "cpu", "gen": 4, "lanes": 4 } ],
  "slots": [ { "label": "PCI_E1", "source": "cpu", "gen": 4, "lanes": 16 } ],
  "sharing": [ { "when": "M2_2", "affects": "SATA5, SATA6", "lanes": 0 } ] }
```

- `match`: confrontato con `Win32_BaseBoard.Product` senza maiuscole/minuscole, spazi e trattini.
- `sharing.lanes`: 0 = disattivato, altrimenti le linee che restano. Il testo è costruito con modelli tradotti, quindi la tabella non contiene frasi.
- Usi: suggerimenti precisi ("M2_1 è collegato alla CPU: sposta lì il disco più usato", "PCI_E1 passa a x8 quando usi M2_3"), M.2 della CPU libero (slot della CPU nella tabella > NVMe trovati sulla CPU), slot della GPU più stretto della scheda.
- Senza scheda in tabella tutto funziona lo stesso, con frasi generiche ("controlla nel manuale quali slot condividono le linee").
- Copertura richiesta da Lorenzo: B450, B550, X570, B650, B650E, X670, X670E, B850, B850E, X870, X870E, Z890, B860, Z790, B760, Z690, B660, B560, Z590, Z490, B460. Per ogni chipset le schede gaming più diffuse di ASUS, MSI, Gigabyte e ASRock (6–10 per chipset), dalle pagine delle specifiche ufficiali; ogni voce porta l'URL della fonte.

## Fasi

1. **Motore PCIe e tab.** Chiavi del link in `DeviceService` (stessa pipeline dell'inventario), `LinkService` con classificazione CPU/chipset, tabella dei chipset e valutazione dei badge come funzione pura, vista `LinkCheckView` con Scan, nuova tab tra USB e Verifica tweak. Test con il percorso reale del PC di Lorenzo e con percorsi Intel finti.
2. **GPU.** ADLX per le GPU AMD dietro switch interno, tabella delle schede strette. NVIDIA: valori di Windows; se il test su NVIDIA mostra che restano quelli dell'avvio, aggiungere NVML (`nvml.dll`). Intel Arc: grigio finché non si aggiunge IGCL.
3. **USB.** Velocità negoziata dall'hub, regole USB della tabella, riuso delle catene di `UsbService`.
4. **Schede madri.** `Boards.json` a lotti per chipset, nell'ordine: AM4 (B450, B550, X570), AM5 (600 e 800), Intel (400 → 800). Test che valida il file (chipset noto, label univoche, generazioni e linee plausibili, URL presente).
5. **Chiusura.** Stringhe nel dominio `Devices` nelle 6 lingue, aggiornamento di `devices-latency.md`, PR.

## Da verificare dal vivo
- Una NVIDIA in idle e sotto carico: `CurrentLinkSpeed` di Windows si aggiorna a ogni lettura o resta quello dell'avvio?
- Una AMD RDNA in slot x8 (o con M.2 che condivide): ADLX restituisce x8?
- Un PC Intel 600/700/800: classificazione CPU/chipset degli NVMe.

## Stato
- [x] Fase 1 — Motore PCIe e tab (provata sul PC di Lorenzo: GPU e Crucial sulla CPU, Samsung 980 e Wi-Fi sul chipset X570)
- [x] Fase 2 — GPU: ADLX per le Radeon. NVML e IGCL rimandati ai test su NVIDIA e Intel Arc
- [x] Fase 3 — USB (provata: la Logitech StreamCam di Lorenzo supporta USB 3 ma va a USB 2; lettura degli hub verificata anche da utente non amministratore)
- [ ] Fase 4 — Schede madri
- [ ] Fase 5 — Lingue, documentazione, PR
