using FellowOakDicom.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DicomViewer : MonoBehaviour
{

    public string dicomFolderName;
    //public string modelName;
    public SliceSlider sliderAxial;
    public SliceSlider sliderCoronal;
    public SliceSlider sliderSagittal;
    public MeshRenderer attachedModel;
    public MeshLoader meshLoader;

    /*public GameObject buttonCollection;
    public GameObject buttonPrefab;*/

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

        /*GameObject button = (GameObject)Instantiate(buttonPrefab);
        button.transform.SetParent(buttonCollection.transform);
        button.name = "Button" + 1;
        //button.GetComponent<Button>().onClick.AddListener(OnClick);
        //button.transform.GetChild(0).GetComponent<Text>().text = "kidney";
        //Text myText = GameObject.Find("ButtonOne").GetComponent<UnityEngine.UI.Text>();
        //myText.text = "Your text changed!";
        button.gameObject.GetComponentInChildren<Text>().text = "----";*/

        var choosedModel = "kidney";

        var models = Directory.GetFiles(@"c:" + Path.DirectorySeparatorChar + "Users"
                                              + Path.DirectorySeparatorChar + "HP" //"giulia.nardicchia"
                                              + Path.DirectorySeparatorChar + "Desktop"
                                              + Path.DirectorySeparatorChar + "UNIBO" //"Giulia Nardicchia"
                                              + Path.DirectorySeparatorChar + "Application", "*.glb");
        /*foreach (string file in models)
        {
            Debug.Log("file: " + file);
            //show them in a list that the user can choose
        }*/

        var dicomFolderName = Directory.GetDirectories(@"c:" + Path.DirectorySeparatorChar + "Users"
                                               + Path.DirectorySeparatorChar + "HP" //"giulia.nardicchia"
                                               + Path.DirectorySeparatorChar + "Desktop"
                                               + Path.DirectorySeparatorChar + "UNIBO" //"Giulia Nardicchia"
                                               + Path.DirectorySeparatorChar + "Application", choosedModel);


        var modelPath = models.Where(f => Path.GetFileName(f).Contains(choosedModel)).Select(f => f).ToList();
        //var modelPath = Path.Combine(Application.streamingAssetsPath, "Models", "kidney.glb");

        var modelMesh = await meshLoader.LoadGltfModelAsync(modelPath.First());

        attachedModel.GetComponent<MeshFilter>().sharedMesh = modelMesh.sharedMesh;
        attachedModel.transform.rotation = modelMesh.transform.rotation;

        var dicomPath = Path.Combine(DicomFileUtils.DicomDirectoryPath, dicomFolderName.First());
        var dicomGroups = (await DicomFileUtils.ReadFromDirectoryAsync(dicomPath))
            .GroupBy(x =>
            {
                var frame = new FrameGeometry(x.Dataset);
                return frame.Orientation;
            });
        var datasetOrientations = dicomGroups.Select(x => x.Key).ToArray();
        var (mainOrientation, planeOrientations) = GetViewerPlaneOrientations(datasetOrientations);

        foreach (var currentGroup in dicomGroups.Reverse())
        {
            var orientation = currentGroup.Key;
            var currentPlane = GetSlider(orientation);
            var viewingPlane = currentPlane.transform.GetChild(0).gameObject;

            var imageRenderer = viewingPlane.GetComponentInChildren<Renderer>();
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

                    //sposta il modello in avanti della dimensione della prima slice
                    //risolve il problema della sovrapposizione
                    //nuovo problema il file DICOM si ferma poco prima della fine del modello
                    //translation.z = ((float)sliceDepths.First()); //0.005005371

                    translation.x = 0;
                    return translation;
                });
            }
        }
    }

    void OnClick()
    {
        Debug.Log("clicked");
    }

    private void FixImageScale(GameObject viewingPlane, Func<Vector3> scalingSupplier)
    {
        var imageRenderer = viewingPlane.GetComponentInChildren<Renderer>();
        var scaling = scalingSupplier();
        viewingPlane.transform.localScale = scaling * 1.25f + Vector3.forward;
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