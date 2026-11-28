# Piano di implementazione dell’audit TweakHub

Per ogni modifica eseguire prima il test di regressione mirato, poi le suite EXE e Store prima del merge. Usare la VM Windows 11 condivisa per le prove su servizi, dispositivi, rete e UAC. Al termine ripristinare il checkpoint pulito.

| Ordine | Rilievi | Intervento e verifica |
|---|---|---|
| 1 | #1 Ripristino di Windows Update | Implementato. I test coprono i backup precedenti, la validazione dei target e il nuovo tentativo dopo un errore. Resta la prova completa con UAC in VM. |
| 2 | #2 e #4 Isolamento dei backup | Implementato. I backup locali usano PC e account Windows. La migrazione dei backup precedenti richiede conferma e una sola istanza può scrivere nella stessa cartella dati. |
| 3 | #3 Aggiornamenti EXE e portatili | Implementato. La scrittura del download termina prima della verifica. Il file verificato resta aperto in sola lettura fino al passaggio all’installer o all’aggiornamento portatile. |
| 4 | #5 e #6 Ripristino del registro | Assegnare una sola responsabilità per ogni policy condivisa. Salvare i valori prima di ogni batch per annullare un tentativo fallito e conservare i backup originali per il ripristino esplicito. Provare il conflitto tra preset e toggle e un errore dopo una modifica precedente riuscita. |
| 5 | #7 Identità delle schede DNS | Salvare l’identificatore stabile della scheda e risolvere l’indice corrente al ripristino. Conservare i record senza corrispondenza. Provare un cambio di indice e un ripristino parziale. |
| 6 | #8 e #9 Piani energetici e dispositivi | Segnalare i fallimenti di `powercfg` e di attivazione del piano. Registrare la disattivazione prevista prima di toccare i dispositivi e confrontarla con lo stato effettivo dopo un’interruzione. |
| 7 | #10 Test di distribuzione | Implementato in anticipo. La suite predefinita compila i test EXE e la CI esegue anche i test Store nella loro configurazione. La validazione degli artefatti rimane separata. |
| 8 | #11 Latenza di avvio | Mostrare la finestra quando la prima pagina è pronta. Verificare con `agent-desktop` che Accesso rapido sia utilizzabile mentre una scansione secondaria è ancora in corso. |
| 9 | #12 e #13 Pulizia build MSIX | Rimuovere le directory di staging dopo le build riuscite e semplificare i controlli specifici di TweakHub. Verificare due build consecutive e conservare le evidenze in caso di errore. |

## Backup di Windows Update esistenti

- I nuovi backup dei servizi e delle attività risiedono nel registro a 64 bit del PC di origine. Un utente standard può leggerne la presenza. I permessi del registro di sistema proteggono le scritture.
- TweakHub conserva il vecchio file `windows-update-backup.json` per la revisione manuale. Il file può provenire da un altro PC o da una cartella scrivibile da un utente standard.
- Il ripristino dei singoli valori del registro usa il percorso esistente. Il punto 4 affronta il coordinamento del ripristino tra quei valori, servizi e attività pianificate.

## Verifica del primo intervento

- I test riproducono il rifiuto di un backup precedente e il ripristino da un archivio di sistema simulato. Verificano che tutti i target siano controllati prima delle modifiche e che il backup sopravviva a una scrittura fallita.
- Su una copia portatile di prova, `agent-desktop` ha selezionato un preset e premuto **Apply** con un vecchio backup di prova. L’avviso è apparso e il file è rimasto intatto.
- In VM, partire dal checkpoint pulito ed eseguire TweakHub come utente standard con credenziali amministrative separate per UAC. Applicare Security e verificare che il backup sia leggibile ma non scrivibile dall’utente standard. Cambiare preset, ripristinare e confrontare i servizi, le attività e i valori del registro con lo stato iniziale.

## Verifica dei punti 2 e 3

- Sul PC host una copia portatile isolata ha mostrato lo stato corrente di Windows Update, respinto un secondo avvio e aperto la finestra del processo di riavvio alla chiusura del primo. Le prove con UAC, cambiamenti di sistema o installazione di un aggiornamento richiedono un ambiente separato.
- I test di importazione usano cartelle temporanee con backup di identità diverse. I test dell’updater usano risposte HTTP simulate per EXE e ZIP validi, checksum errato, download troncato e annullamento.
