using FellowOakDicom.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Dummiesman;

public class DicomViewerObjImporter : MonoBehaviour
{
    public SliceSlider sliderAxial;
    public SliceSlider sliderCoronal;
    public SliceSlider sliderSagittal;
    public MeshRenderer attachedModel;
    public MeshLoader meshLoader;

    private GameObject viewingPlane;
    private SliceSlider currentPlane;
    private GameObject invisiblePlane;

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

    public async void SetModel(string model, string dicomFolderName)
    {
        var textStream = new MemoryStream(Encoding.UTF8.GetBytes(model.ToString()));
        var loadedObj = new OBJLoader().Load(textStream);

        MeshFilter[] meshFilters = loadedObj.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }
        attachedModel.GetComponent<MeshFilter>().mesh = new Mesh();
        attachedModel.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        var modelMesh = attachedModel.GetComponent<MeshFilter>();

        Destroy(loadedObj);

        attachedModel.transform.rotation = modelMesh.transform.rotation;
        attachedModel.transform.localPosition = modelMesh.transform.position;

        //-------------------------------------------------------------------------

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
            currentPlane = GetSlider(orientation);
            invisiblePlane = currentPlane.transform.GetChild(0).gameObject;

            var geometryInfo = currentGroup
                .Select(x => new FrameGeometry(x.Dataset));
            var sliceDepths = geometryInfo
                .Select(x => x.ProjectOnRotatedSpace())
                .Select(x => x.z);
            var modelDepth = sliceDepths.Max() - sliceDepths.Min();
            var frameGeometry = geometryInfo.First();
            var textures = currentGroup
                .Select(DicomFileUtils.ExtractTexture)
                .ToList();

            viewingPlane = Instantiate(invisiblePlane, currentPlane.transform.position, currentPlane.transform.rotation);
            viewingPlane.transform.SetParent(this.transform);
            var imageRenderer = viewingPlane.GetComponentInChildren<Renderer>();

            currentPlane.UseImages(sliceDepths, e => imageRenderer.material.mainTexture = textures[e.NewIndex]);
            currentPlane.gameObject.SetActive(true);
            FixImageScale(viewingPlane, frameGeometry.GetScalingVector);
            currentPlane.transform.rotation = planeOrientations[orientation];
            currentPlane.transform.localPosition = Vector3.forward * modelDepth / 2;

            currentPlane.GetComponentInChildren<Renderer>().material = new Material(Shader.Find("Custom/Outline Mask"));
            invisiblePlane.transform.localScale = viewingPlane.transform.localScale;

            if (orientation == mainOrientation)
            {
                FixOrientation(frameGeometry.GetRotation);
                FixBoxCollider(frameGeometry.GetScalingVector, modelDepth);
                AlignSlices(() =>
                {
                    //Kidney.obj   
                    var translation = currentPlane.transform.position - attachedModel.bounds.center;
                    translation.x = 0;

                    //ACTright.obj e ACTinverted.obj
                    /*var translation = currentPlane.transform.position - attachedModel.bounds.center;
                    translation.x = -translation.y;
                    translation.y = 0;
                    translation.z = translation.z + 0.022f;*/

                    //sposta il modello in avanti della dimensione della prima slice
                    //risolve il problema della sovrapposizione
                    //nuovo problema il file DICOM si ferma poco prima della fine del modello
                    //translation.z = ((float)sliceDepths.First()); //0.005005371
                    return translation;
                });
            }
        }

        //attachedModel.transform.localScale = new Vector3(0.001f,0.001f,0.001f); //ACT.obj
        //attachedModel.transform.localPosition = new Vector3(-0.126f,0.124f,0); //ACT.obj

        //attachedModel.transform.localPosition = new Vector3(-0.03f, 0, 0.28f); //ACTright.obj e ACTinverted.obj
    }

    public void Update()
    {
        if (viewingPlane != null && invisiblePlane != null)
        {
            viewingPlane.transform.position = new Vector3(invisiblePlane.transform.position.x, invisiblePlane.transform.position.y, invisiblePlane.transform.position.z + 0.002f);
            viewingPlane.transform.rotation = invisiblePlane.transform.rotation;
        }
    }

    private void FixImageScale(GameObject invisiblePlane, Func<Vector3> scalingSupplier)
    {
        var imageRenderer = invisiblePlane.GetComponentInChildren<Renderer>();
        var scaling = scalingSupplier();
        invisiblePlane.transform.localScale = scaling * 1.25f + Vector3.forward;
        imageRenderer.transform.localScale = (Vector3.up + Vector3.right) / 1.25f;
    }

    private void FixBoxCollider(Func<Vector3> scalingSupplier, float modelDepth)
    {
        var collider = GetComponent<BoxCollider>();
        collider.size = scalingSupplier() + Vector3.forward * modelDepth * 1.001f;
        collider.center += Vector3.forward * modelDepth / 2;
    }

    private void FixOrientation(Func<Quaternion> rotationSupplier)
    {
        attachedModel.transform.Rotate(rotationSupplier().eulerAngles, Space.World);
        attachedModel.transform.Rotate(Vector3.forward * 180, Space.World);
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