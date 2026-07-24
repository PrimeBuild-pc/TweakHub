# Piano implementativo — punti 6 e 7: viste WPF native

## Valutazione del vantaggio

Sì, entrambi i punti hanno un vantaggio concreto, soprattutto sulla manutenibilità:

| Punto | Beneficio atteso | Rischio | Priorità |
|---|---|---|---|
| 6. `QuickAccessPage` dichiarativa | Medio: elimina quasi tutta la costruzione manuale dei controlli | Medio-basso | Prima |
| 7. `ExternalToolsPage` dichiarativa | Alto: elimina il maggiore blocco di UI imperativa rimasto | Medio-alto | Dopo il punto 6 |

Stima prudente complessiva: **300–400 righe C# rimosse**, compensate da circa **150–220 righe XAML**, quindi **120–220 righe nette in meno**. Il vantaggio principale non è il conteggio: template, binding, accessibilità e layout diventano centralizzati invece di essere ricostruiti elemento per elemento.

## Branch e base

- Branch del piano: `plan/native-wpf-list-views`
- Base verificata: `cleanup/ponytail-safe`, commit `90522da`
- Per l'implementazione creare un nuovo branch dal branch del piano, oppure da `cleanup/ponytail-safe` dopo aver copiato questo documento.

## Ambito

### Incluso

1. Rendere `QuickAccessPage` basata su `ItemsControl` e `DataTemplate`.
2. Rendere `ExternalToolsPage` basata su gruppi dati, `ItemsControl` e `DataTemplate`.
3. Conservare integralmente azioni, filtri, preferiti, dialoghi, progressi, localizzazione e accessibilità.
4. Aggiungere un solo controllo automatico mirato per la logica di filtro più delicata.

### Escluso

- Nessun framework MVVM.
- Nessuna nuova dipendenza NuGet.
- Nessun nuovo service, repository, command framework o view model generale.
- Nessuna modifica al catalogo di shortcut o strumenti.
- Nessuna modifica a installazione WinGet, esecuzione PowerShell o persistenza.
- Nessun refactoring delle altre pagine o dei dialoghi.
- Nessuna virtualizzazione personalizzata: il catalogo corrente non la richiede.

## Vincoli funzionali da preservare

### Quick Access

- Categorie nello stesso ordine corrente.
- Shortcut ordinate per nome dentro ogni categoria.
- Icone e nomi categoria localizzati.
- Stesso comando eseguito per ogni shortcut.
- Stessi dialoghi in caso di errore.
- Attivazione nativa da mouse, `Enter` e `Space`.
- Layout leggibile alle larghezze attuali di riferimento: circa 700 e 1100 px.

### External Tools

- Preferiti mostrati anche nella sezione iniziale quando il filtro “solo preferiti” è disattivato.
- Con “solo preferiti” attivo, mostrare esclusivamente gli strumenti preferiti raggruppati nelle categorie normali.
- Ricerca invariata su nome, descrizione, categoria interna e categoria localizzata.
- Ordinamento categorie invariato; preferiti prima degli altri dentro la categoria.
- Apertura siti, esecuzione PowerShell e installazione WinGet invariate.
- Modifica ed eliminazione degli strumenti personalizzati invariate.
- Disinstallazione WinGet e relativa conferma invariate.
- Pannello di avanzamento e temporizzazione di chiusura invariati.
- Card attivabili da tastiera e con gli stessi nomi per l'automazione.

---

# Fase 0 — Baseline

Prima di modificare codice:

1. Eseguire:
   ```powershell
   dotnet test TweakHub.Tests/TweakHub.Tests.csproj -c Release --no-restore
   ```
2. Annotare il totale baseline: **64 test** sul commit `90522da`.
3. Acquisire screenshot di entrambe le pagine a larghezze approssimative di 600, 800 e 1200 px.
4. Verificare manualmente almeno uno shortcut, un link, un preferito e la ricerca strumenti.

Questa fase serve solo come riferimento visivo e funzionale; non introdurre snapshot test o framework UI.

---

# Punto 6 — `QuickAccessPage`

## File principali

- `Views/QuickAccessPage.xaml`
- `Views/QuickAccessPage.xaml.cs`
- Eventualmente `TweakHub.Tests/ShortcutServiceTests.cs` solo se emerge logica non già coperta.

## Struttura dati minima

Aggiungere in `QuickAccessPage.xaml.cs` un piccolo record privato, senza creare nuovi file:

```csharp
private sealed record ShortcutGroup(
    string Name,
    string Icon,
    IReadOnlyList<SystemShortcut> Shortcuts);
```

`Name` deve essere già localizzato e `Icon` deve provenire da `ShortcutService.CategoryIcon`.

## Passi

1. **Sostituire il contenitore imperativo**
   - Rimpiazzare `ShortcutsContainer` con un `ItemsControl` nominato, associato a una lista di `ShortcutGroup`.
   - Il template del gruppo contiene intestazione e secondo `ItemsControl` per le shortcut.

2. **Definire i template in XAML**
   - Portare nel `DataTemplate` quanto oggi viene creato da `CreateShortcutButton`.
   - Usare un vero `Button`: mouse, `Enter`, `Space` e focus restano nativi.
   - Conservare risorse, colori, font, wrapping e automation name.

3. **Usare un pannello responsivo nativo**
   - Usare `WrapPanel` come `ItemsPanel` interno.
   - Impostare una larghezza/min-width delle card che permetta il wrapping naturale.
   - Evitare converter o dependency property finché il `WrapPanel` mantiene un risultato visivo accettabile.
   - Solo se il confronto screenshot mostra una regressione evidente, mantenere il calcolo 1/2/3 colonne tramite un singolo binding/converter condivisibile; non ripristinare la scansione manuale dei figli visuali.

4. **Ridurre il code-behind**
   - `LoadShortcuts` deve soltanto raggruppare, ordinare e assegnare `ItemsSource`.
   - Sostituire la lambda per ogni pulsante con un solo handler `Shortcut_Click`, leggendo `DataContext`.
   - Mantenere `ExecuteShortcut` e i dialoghi esistenti.

5. **Predefinire lo stato di errore in XAML**
   - Aggiungere un `TextBlock` di errore inizialmente nascosto.
   - `ShowLoadError` deve solo cambiare `Visibility`, senza creare controlli.

6. **Eliminare codice diventato inutile**
   - `GetColumnCount`
   - `UpdateGridColumns`
   - `CreateShortcutButton`
   - handler `SizeChanged`, se il `WrapPanel` è sufficiente
   - creazione dinamica di intestazioni, griglie e testo di errore

## Criteri di completamento del punto 6

- Nessun controllo shortcut creato con `new Button`, `new TextBlock` o `Children.Add` nel code-behind.
- Nessuna scansione di `ShortcutsContainer.Children`.
- Ordine, icone, descrizioni e comandi invariati.
- Navigazione da tastiera verificata.
- Nessuna nuova dipendenza o classe pubblica.

Commit consigliato separato:

```text
Refactor Quick Access cards to WPF templates
```

---

# Punto 7 — `ExternalToolsPage`

## File principali

- `Views/ExternalToolsPage.xaml`
- `Views/ExternalToolsPage.xaml.cs`
- `TweakHub.Tests/ShortcutServiceTests.cs` oppure un singolo nuovo test nello stesso progetto

Non modificare `ShortcutService`, `ToolDownloadService`, `UserDataService` o `ExternalTool` salvo necessità dimostrata. La prima soluzione deve riusare i modelli esistenti.

## Strutture dati minime

Definire due record privati dentro `ExternalToolsPage.xaml.cs`:

```csharp
private sealed record ToolItem(
    ExternalTool Tool,
    string ActionText,
    string ActionIcon);

private sealed record ToolGroup(
    string Name,
    string Icon,
    string CountText,
    IReadOnlyList<ToolItem> Tools);
```

I wrapper servono solo a proiettare testo/icona già calcolati. Non devono contenere logica di persistenza o esecuzione.

## Passi

1. **Separare proiezione dati e rendering**
   - Trasformare `LoadExternalTools` in una funzione che filtra, ordina e produce `ToolGroup`.
   - Assegnare il risultato a un `ItemsControl` esterno.
   - Non creare `Expander`, `UniformGrid`, card o testi dal C#.

2. **Preservare esattamente la semantica dei preferiti**
   - Se `_favoritesOnly == false` e ci sono preferiti, aggiungere per primo un gruppo “Preferiti”.
   - Continuare poi con tutti i gruppi normali, inclusi gli stessi strumenti preferiti.
   - Se `_favoritesOnly == true`, non creare il gruppo speciale; filtrare i gruppi normali.

3. **Spostare intestazioni e card in XAML**
   - Template gruppo: `Expander`, icona, nome e conteggio.
   - Template card: nome, icona, descrizione, azione e pulsanti secondari.
   - Usare `DataTrigger` per mostrare/nascondere modifica, eliminazione e disinstallazione.
   - Usare i valori `ActionText` e `ActionIcon` del wrapper per evitare converter.

4. **Conservare l'attivazione accessibile della card**
   - La card resta focusable.
   - Collegare in XAML un solo `MouseLeftButtonUp` e un solo `KeyDown` condivisi.
   - Entrambi chiamano un metodo comune `RunToolAsync`.
   - `Enter` e `Space` devono continuare a funzionare.
   - I pulsanti annidati devono impostare `e.Handled = true`, come oggi.

5. **Convertire le azioni locali in handler condivisi**
   - Preferito
   - Modifica
   - Eliminazione
   - Disinstallazione
   - Apertura/installazione/esecuzione della card

   Ogni handler recupera `ToolItem` o `ExternalTool` dal `DataContext`; nessuna closure per card.

6. **Gestire il refresh senza cambiare il modello**
   - `ExternalTool` non deve diventare `INotifyPropertyChanged` solo per questa pagina.
   - Dopo cambio preferito, salvataggio o eliminazione, ricostruire la lista dei gruppi dati come già avviene oggi.
   - Il costo è trascurabile per il catalogo corrente e evita un nuovo livello di stato.

7. **Layout responsivo**
   - Provare prima un `WrapPanel` con dimensioni card definite nello style.
   - Confrontare gli screenshot alle tre larghezze baseline.
   - Se il risultato non conserva almeno 1/2/3 card leggibili, introdurre al massimo un piccolo converter larghezza→colonne usato dal binding XAML.
   - Non iterare il visual tree e non aggiungere un layout panel custom.

8. **Predefinire lo stato di errore in XAML**
   - Come per Quick Access, mostrare un elemento già dichiarato invece di costruire un `TextBlock` a runtime.

9. **Eliminare codice diventato inutile**
   - `CreateCategoryHeader`
   - `CreateFavoritesHeader`
   - `CreateToolCard`
   - `UpdateGridColumns`
   - `GetToolColumnCount`, se sostituito dal layout nativo
   - creazione dinamica dello stato di errore
   - lambda e funzioni locali istanziate per ogni card

## Controllo automatico minimo richiesto

La logica ricerca/preferiti contiene più rami e deve lasciare un controllo eseguibile.

Estrarre solo la parte realmente riusata dal rendering, ad esempio:

```csharp
internal static IEnumerable<ExternalTool> FilterTools(
    IEnumerable<ExternalTool> tools,
    string searchQuery,
    bool favoritesOnly)
```

Aggiungere **un solo test parametrico o un solo test con `Assert.Multiple`** che verifichi:

- filtro “solo preferiti”;
- ricerca case-insensitive per nome;
- ricerca per descrizione/categoria;
- esclusione degli elementi non corrispondenti.

Non testare WPF visual tree, pixel, template o binding con reflection.

## Criteri di completamento del punto 7

- Nessuna card o intestazione creata imperativamente.
- Ricerca e preferiti conservano la semantica corrente.
- Tutte le azioni secondarie restano disponibili.
- Progress panel invariato.
- Nessuna nuova dipendenza.
- Nessun framework MVVM.
- Un solo test nuovo per la logica di filtro.

Commit consigliato separato:

```text
Refactor External Tools cards to WPF templates
```

---

# Verifica finale

## Automatica

```powershell
dotnet build TweakHub.sln -c Release --no-restore
dotnet test TweakHub.Tests/TweakHub.Tests.csproj -c Release --no-build --no-restore
git diff --check
```

Atteso:

- build senza warning/errori;
- tutti i 64 test baseline più il singolo test del filtro;
- nessun errore whitespace.

## Manuale — Quick Access

- Aprire tutte le categorie.
- Avviare almeno uno `.msc`, un comando semplice e un URI `ms-settings:`.
- Verificare errore controllato con uno shortcut temporaneamente non valido solo in debug, poi ripristinarlo.
- Navigare con `Tab`, `Enter` e `Space`.
- Ridimensionare la finestra alle tre larghezze baseline.

## Manuale — External Tools

- Cercare per nome, descrizione e nome categoria localizzato.
- Attivare/disattivare “solo preferiti”.
- Aggiungere e rimuovere un preferito.
- Aprire un sito.
- Verificare una card WinGet senza completare necessariamente l'installazione, almeno fino alla conferma/progresso.
- Creare, modificare ed eliminare uno strumento personalizzato di prova.
- Verificare che i pulsanti secondari non attivino anche la card.
- Navigare con tastiera e controllare i nomi accessibili.

# Strategia di rollback

I due punti devono restare in commit separati. Se il punto 7 produce regressioni, è possibile conservarne il piano e fare revert del solo secondo commit senza perdere il refactoring più semplice di Quick Access.

# Condizione di stop

Non introdurre altre astrazioni per ottenere una riduzione nominale di righe. Se template e binding richiedono view model generici, command framework o converter multipli, fermarsi alla soluzione code-behind minima con gruppi dati e handler condivisi.
