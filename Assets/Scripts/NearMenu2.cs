using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using System.Threading.Tasks;

public class NearMenu2 : MonoBehaviour
{
    public GameObject buttonCollection;
    public GameObject buttonPrefab;
    public ServerGateway2 serverGateway2;
    public DicomViewerObjImporter dicomViewerObjImporter;

    private List<string> models;
    private string model;
    private string modelSelected;

    private Dictionary<string, int> modelDictionary = new Dictionary<string, int>();

    private void Awake()
    {
        this.serverGateway2.Init();
        this.serverGateway2.SetLimit(6);
    }

    private void Start()
    {
        StartCoroutine(this.GetListModel());
        this.CreateButtons(this.models);

        var allInteractables = GameObject.FindObjectsOfType<Interactable>();
        foreach (var interactable in allInteractables)
        {
            interactable.OnClick.AddListener(() => {

                int value;
                this.modelSelected = interactable.gameObject.name;
                bool hasValue = modelDictionary.TryGetValue(this.modelSelected, out value);
                if (hasValue)
                {
                    this.serverGateway2.SetId(value);
                    StartCoroutine(SetModel());
                    //Debug.Log(Time.time + ": " + interactable.gameObject.name + " was clicked " + "id: " + value);
                }
                else
                {
                    Debug.Log("Key not present");
                }
            });
        }
    }

    private void Update()
    {
        GridObjectCollection gridObjectCollection = buttonCollection.GetComponent<GridObjectCollection>();
        gridObjectCollection.UpdateCollection();
    }

    private void CreateButtons(List<string> models)
    {
        var modelsArray = models.ToArray();

        for (int i = 0; i < modelsArray.Length; i++)
        {
            GameObject button = Instantiate(buttonPrefab);
            button.transform.SetParent(buttonCollection.transform);
            string buttonName = modelsArray[i].Trim('"');
            button.name = buttonName;
            button.GetComponentInChildren<TMP_Text>().text = buttonName;
            //TMP_Text comprende TextPro e TextProUGUI nel nuovo aggiornamento
            button.SetActive(true);

            modelDictionary.Add(buttonName, i);
        }
    }

    public IEnumerator GetListModel()
    {
        this.models = this.serverGateway2.GetListModel();
        yield return new WaitUntil(() => (!this.serverGateway2.GetListModel().Equals(null)));
    }

    public IEnumerator SetModel()
    {
        this.serverGateway2.ResetModel();
        yield return new WaitUntil(() => !(String.IsNullOrEmpty(this.serverGateway2.GetModel())));
        this.model = this.serverGateway2.GetModel();

        DicomViewerObjImporter newDicomViewerObjImporter = Instantiate(dicomViewerObjImporter);
        newDicomViewerObjImporter.SetModel(this.model, this.modelSelected.Replace(".obj", ""));
        newDicomViewerObjImporter.gameObject.SetActive(true);
    }
}
