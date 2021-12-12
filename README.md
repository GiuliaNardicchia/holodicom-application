# README #

## Used technologies ##
### Client ###
- Unity 2020.3.14f1 LTS with OpenXR
- MRTK 2.7
- Visual Studio 2019
### ServerWeb and UploadFiles ###
- Node.js
- Visual Studio Code

## How it works ##
Una volta avviata l'applicazione, verrà creata una connessione al server-web tramite WebSocket, il quale richiederà una lista degli ultimi sei modelli caricati in una cartella nel file system. Una volta che tale lista viene restituita, all'interno dell'ambiente sarà visualizzato un menu comprendente appunto sei bottoni, che rappresentano ciascuno un modello con estensione .obj. Cliccandoci sopra verrà effettuata un'altra richiesta in cui si vuole ottenere il file corrispondente. Una volta ottenuto verranno caricati i file DICOM che si trovano all'interno dello StreamingAsset e il modello caricato a runtime mediante OBJImport.

## Description ##
All'interno della Scene di Unity sono presenti i seguenti GameObject:

- `Plugins/` Contiene il codice sorgente del progetto FO-DICOM.Core con le sue dipendenze compilate per la versione 2.0 del .NET Standard che è la versione supportata dalla versione 2020 di Unity. Gli assembly delle dipendenze sono stati presi dai corrispondenti pacchetti NuGet. Anche  WebSocketSharp è stato aggiunto alle dipendenze tramite NuGet per poter comunicare con il ServerWeb.

- `Streaming Assets/`
    - `DICOM/` 
        - `kidneys/` Dataset di immagini in piano frontale dell'addome di un paziente.
        - `kidneys2/` Dataset di immagini in piano frontale dell'addome di un altro paziente aventi dei reni policistici.
        - `skull/` CT scan in piano traverso di una testa.
        - `ACT/` CT scan dell'atrio di un cuore.
    - `Models/` 
        - **kidney.glb** Versione in formato `glTF` del modello dei reni originale. I file con estensione .glb non servono più se si utilizza il caricamento a runtime mediante OBJImport. 

- `OBJImport/`
    Contiene il codice sorgente dei file che permettono il caricamento a runtime dei modelli importabili in formato **.obj**

- `Shaders/`
    Contiene una versione modificata dello `StandardShader` del MRTK per consentire il clipping in contemporanea da tre piani diversi.

- `QuickOutline/`
    Importato allo scopo di visualizzare i bordi della mesh del modello davanti al file DICOM.

- `Materials/`
    - **ClippingPlane.mat** Materiale per la visualizzazione del piano di clipping.
    - **DoubleFacedRenderer** Utilizza lo `StandardShader` fornito in MRTK configurato per disabilitare il culling dei vertici e la definizione a compile-time dell'albedo, in modo da potere essere utilizzato su un elemento `Quad` di Unity per visualizzare texture caricate a runtime.
    - **ModelMaterial.mat** Usa lo shader customizzato per abilitare il clipping del modello.
- `Scripts/`
    - `MeshLoader/` Componente necessaria al caricamento a runtime dei modelli, include una classe template con tre implementazioni per il caricamento tramite File, HTTP, o, in caso di modelli supportati dalla pipeline di Unity, normale caricamento di asset serializzati. 
    - `UI/`
        * **SliceSlider.cs** Implementazione di [IMixedRealityPointerHandler](https://docs.microsoft.com/en-us/dotnet/api/microsoft.mixedreality.toolkit.input.imixedrealitypointerhandler) per l'interazione con i piani di interazione delle immagini.
        Consente lo spostamento del piano lungo la sua direzione normale rispettando le posizioni indicate nei file DICOM. 
        Inoltre usa gli eventi di Unity per permettere ad altri componenti di osservare il cambiamento di posizione e l'inizio e la fine di ogni interazione.
    - **DicomFileUtils.cs** Contiene le funzioni per il caricamento dei file DICOM da directory. 
    - **GeometryExtensions.cs** Estensioni per la classe `FrameGeometry` di fo-dicom in particolare include funzioni per:
        * Ricavare un vettore di scala per il piano di visualizzazione dall'attributo [Pixel Spacing](https://dicom.innolitics.com/ciods/rt-dose/image-plane/00280030) dei file DICOM.
        * Convertire la rotazione dell'immagine rispetto al sistema di riferimento del paziente, dai [vettori unitari](https://dicom.innolitics.com/ciods/rt-dose/image-plane/00200037) presenti nei file DICOM, alla rappresentazione secondo gli angoli di Eulero per l'ordinamento ZXY usato da Unity.
        * Proiezione del vettore fornito dall'attributo [Image Position](https://dicom.innolitics.com/ciods/rt-dose/image-plane/00200032) sul sistema di riferimento del paziente, per il posizionamento corretto della superficie di visualizzazione.
    - **DicomViewer.cs** Lo script per il componente principale della scena, dopo avere caricato i file DICOM e la mesh del modello procede ad inizializzare le proprietà dei piani di visualizzazione corrispondenti ai punti di vista usati nel dataset caricato, e l'orientamento del modello.
    - **DicomViewerOBJImporter.cs** Svolge lo stesso compito del DicomViewer con la differenza che utilizza OBJImport per caricare il modello con estensione .obj.
    - **ServerGateway.cs** Crea una connessione WebSocket, permette l'invio e la ricezione dei messaggi.
    - **NearMenu3x2.cs** Menu che permette la visualizzazione di sei bottoni, comunica tramite il ServerGateway e il DicomViewerOBJImporter.
- `Scenes/`
    - **Main.unity** La scena principale del progetto contiene sette esempi di DicomViewer, uno per il dataset del cranio, uno per quello dei reni, uno per visualizzare un atrio del cuore, uno che utilizza una versione modificata dello script per abilitare tutti i piani di visualizzazione per testare il funzionamento dello shader modificato, uno che permette la visualizzazione del bordo della mesh dei reni e dell'atrio del cuore e infine uno che permette il caricamento runtime dei modelli.

## Problems ##
### Ottimizzazione dei modelli ###
I modelli ottenuti da file DICOM hanno un numero molto elevato di vertici, il che limita fortemente le performance su Hololens. La latenza del caricamento è molto sentita.
### Caricamento di File Dicom runtime ###
Nella versione corrente i modelli possono essere caricati tramite una richiesta al WebSocket, in formato `.obj` (formato Wavefront). Mentre il caricamento dei file DICOM rimane limitato alla lettura di `Streaming Assets/`, questo perché dalle prove svolte, il caricamento di file molto lunghi e numerosi comporta il blocco dell'applicazione per molto tempo.
### Visualizzazione del bordo dei modelli ###
Non è possibile visualizzare l'outline dei modelli tramite il caricamento a runtime perchè OBJImport non carica la mesh se non è di tipo `Standard (Specular setup)` che rende visibile tutto il modello.
### Perfezionamento di corrispondenza tra immagini e Modelli ###
Nella versione attuale del progetto l'allineamento dell'immagine viene effetuato centrando la bounding box del modello sull'asse di scorrimento del piano dell'immagine. Il metodo ottiene risultati abbastanza buoni ma per avere una corrispondenza perfetta andrebbe usati metodi più sofisticati.