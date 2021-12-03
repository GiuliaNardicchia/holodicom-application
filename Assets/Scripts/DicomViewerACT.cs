using FellowOakDicom.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class DicomViewerACT : MonoBehaviour
{

    public string dicomFolderName;
    public SliceSlider sliderAxial;
    public SliceSlider sliderCoronal;
    public SliceSlider sliderSagittal;
    public MeshRenderer attachedModel;
    public MeshLoader meshLoader;

    private SliceSlider GetSlider(FrameOrientation orientation) => orientation switch
    {
        FrameOrientation.Axial => sliderAxial,
        FrameOrientation.Coronal => sliderCoronal,
        FrameOrientation.Sagittal => sliderSagittal,
        _ => throw new ArgumentOutOfRangeException(),
    };

    private (FrameOrientation, IDictionary<FrameOrientation, Quaternion>) GetViewerPlaneOrientations(IEnumerable<FrameOrientation> datasetOrientations)
    {
        Func<FrameOrientation, bool> checkForOrientation = datasetOrientations.Contains;
        var mainOrientation = (
            checkForOrientation(FrameOrientation.Coronal),
            checkForOrientation(FrameOrientation.Axial),
            checkForOrientation(FrameOrientation.Sagittal)
        ) switch
        {
            (true, _, _) => FrameOrientation.Coronal,
            (false, true, _) => FrameOrientation.Axial,
            (false, false, true) => FrameOrientation.Sagittal,
            _ => throw new ArgumentOutOfRangeException()
        };
        var planeOrientations = mainOrientation switch
        {
            FrameOrientation.Coronal => new Dictionary<FrameOrientation, Quaternion>()
            {
                {FrameOrientation.Coronal,  Quaternion.identity},
                {FrameOrientation.Axial,  Quaternion.Euler(Vector3.right * -90)},
                {FrameOrientation.Sagittal,  Quaternion.Euler(Vector3.up * -90)},
            },
            FrameOrientation.Axial => new Dictionary<FrameOrientation, Quaternion>()
            {
                {FrameOrientation.Coronal,  Quaternion.Euler(Vector3.right * 90)},
                {FrameOrientation.Axial,  Quaternion.identity},
                {FrameOrientation.Sagittal,  Quaternion.Euler(Vector3.up * -90) * Quaternion.Euler(Vector3.forward * -90)},
            },
            FrameOrientation.Sagittal => new Dictionary<FrameOrientation, Quaternion>()
            {
                {FrameOrientation.Coronal,  Quaternion.Euler(Vector3.up * 90)},
                {FrameOrientation.Axial, Quaternion.Euler(Vector3.up * 90) * Quaternion.Euler(Vector3.right * -90)},
                {FrameOrientation.Sagittal,  Quaternion.identity},
            },
            _ => throw new ArgumentOutOfRangeException()
        };
        return (mainOrientation, planeOrientations);
    }

    private async void Start()
    {
        var modelPath = Path.Combine(Application.streamingAssetsPath, "Models", "ACT.glb");
        var modelMesh = await meshLoader.LoadGltfModelAsync(modelPath);
        attachedModel.GetComponent<MeshFilter>().sharedMesh = modelMesh.sharedMesh;
        attachedModel.transform.rotation = modelMesh.transform.rotation;

        //---------------------------------------------------------------------------------
        /*var mesh = attachedModel.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        // create new colors array where the colors will be created.
        UnityEngine.Color32[] colors = new UnityEngine.Color32[vertices.Length];

        var maxValue = vertices.Max(v => v.z);
        var minValue = vertices.Min(v => v.z);

        for (int i = 0; i < vertices.Length; i++)
        {
            //normalizzazione dei valori z da 0 a 1
            var normailze = (vertices[i].z - minValue) / (maxValue - minValue);
            colors[i] = Color.Lerp(Color.black, Color.white, normailze);
            //Debug.Log("Normalize: " + normailze);
            //Debug.Log("Position z: " + vertices[i].z);
        }

        // assign the array of colors to the Mesh.
        mesh.colors32 = colors;*/
        //---------------------------------------------------------------------------------

        var dicomPath = Path.Combine(DicomFileUtils.DicomDirectoryPath, dicomFolderName);
        var dicomGroups = (await DicomFileUtils.ReadFromDirectoryAsync(dicomPath))
            .GroupBy(x =>
            {
                var frame = new FrameGeometry(x.Dataset);
                return frame.Orientation;
            });

        var datasetOrientations = dicomGroups.Select(x => x.Key).ToArray();
        var (mainOrientation, planeOrientations) = GetViewerPlaneOrientations(datasetOrientations);
        foreach (var currentGroup in dicomGroups)
        {
            var orientation = currentGroup.Key;
            var currentPlane = GetSlider(orientation);
            var viewingPlane = currentPlane.transform.GetChild(0).gameObject;
            var imageRenderer = viewingPlane.GetComponentInChildren<Renderer>();
            var geometryInfo = currentGroup
                .Select(x => new FrameGeometry(x.Dataset));
            var sliceDepths = geometryInfo
                .Select(x => x.ProjectOnRotatedSpace()) //aggiunta del meno per visualizzare il DICOM dritto ma interazione al contrario
                .Select(x => x.z);
            var modelDepth = sliceDepths.Max() - sliceDepths.Min();
            var frameGeometry = geometryInfo.First();
            var textures = currentGroup
                .Select(DicomFileUtils.ExtractTexture)
                .ToList();
            currentPlane.UseImages(sliceDepths, e => imageRenderer.material.mainTexture = textures[e.NewIndex]);
            currentPlane.gameObject.SetActive(true);
            FixImageScale(viewingPlane, frameGeometry.GetScalingVector);
            currentPlane.transform.rotation = planeOrientations[orientation];
            currentPlane.transform.localPosition = Vector3.forward * modelDepth / 2;
            if (orientation == mainOrientation)
            {
                FixOrientation(frameGeometry.GetRotation);
                FixBoxCollider(frameGeometry.GetScalingVector, modelDepth);
                AlignSlices(() =>
                {
                    var translation = currentPlane.transform.position - attachedModel.bounds.center;
                    translation.x = 0;
                    return translation;
                });
            }

            //scalare il modello
            //attachedModel.GetComponent<MeshFilter>().transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            //posizionare il modello al centro del BoxCollider
            //attachedModel.GetComponent<MeshFilter>().transform.position = new Vector3(0.2f, GetComponent<BoxCollider>().center.y, GetComponent<BoxCollider>().center.z);
            //attachedModel.GetComponent<MeshFilter>().transform.position = new Vector3(0.2f, -0.1720783f, -0.02775002f);
            //attachedModel.GetComponent<MeshFilter>().transform.localPosition = Vector3.forward * modelDepth / 2;

            /*var outline = attachedModel.gameObject.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineVisible; //OutlineHidden
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 6f;*/
        }
    }

    private void FixImageScale(GameObject viewingPlane, Func<Vector3> scalingSupplier)
    {
        var imageRenderer = viewingPlane.GetComponentInChildren<Renderer>();
        var scaling = scalingSupplier();
        viewingPlane.transform.localScale = scaling * 1.25f + Vector3.forward; //viewingPlane.transform.localRotation * (scaling * 1.25f + Vector3.forward);
        imageRenderer.transform.localScale = (Vector3.up + Vector3.right) / 1.25f;


        //bisogna scalare allo stesso modo anche il modello
        //poichè la sua dimensione e la sua posizione sono corrette e coerenti con i file DICOM
        //attachedModel.GetComponent<MeshFilter>().transform.localScale = scaling * 1.25f + Vector3.forward;
    }

    private void FixBoxCollider(Func<Vector3> scalingSupplier, float modelDepth)
    {
        var collider = GetComponent<BoxCollider>();
        collider.size = scalingSupplier() + Vector3.forward * modelDepth * 1.001f;
        collider.center += Vector3.forward * modelDepth / 2;

        //scala il modello alla stessa dimensione del BoxCollider
        //attachedModel.GetComponent<MeshFilter>().transform.localScale = scalingSupplier() + Vector3.forward * modelDepth * 1.001f;
    }

    private void FixOrientation(Func<Quaternion> rotationSupplier)
    {
        attachedModel.transform.Rotate(rotationSupplier().eulerAngles, Space.World);
        attachedModel.transform.Rotate(Vector3.forward * 180, Space.World);
        //attachedModel.transform.Rotate(Vector3.up, Space.World); //aggiunto per capovolgerlo da sotto a sopra
    }

    private void AlignSlices(Func<Vector3> translationSupplier) => attachedModel.transform.localPosition += translationSupplier();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        var unityWorldBounds = attachedModel.GetComponent<Renderer>().bounds;
        Gizmos.DrawWireCube(unityWorldBounds.center, unityWorldBounds.size);
        Gizmos.DrawSphere(unityWorldBounds.center, 0.003f);
        Gizmos.color = Color.red;
        var modelPosition = attachedModel.transform.position;
        Gizmos.DrawSphere(modelPosition, 0.003f);
        Gizmos.color = Color.cyan;
    }
}