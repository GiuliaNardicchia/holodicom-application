using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;

public class NearMenu : MonoBehaviour
{
    public GameObject buttonCollection;
    public GameObject buttonPrefab;
    public ServerGateway serverGateway;
    public DicomViewerObjImporter dicomViewerObjImporter;

    private Dictionary<string, int> modelDictionary = new Dictionary<string, int>();

    async void Start()
    {

        StartCoroutine(this.serverGateway.GetModels(6, (models) =>
        {
            /*this.elementsNumber = models.Length;
            buttonCollection.transform.Height = 0.32f * (elementsNumber + 1);
            buttonCollection.transform.Widht = 0.32f * (elementsNumber + 1);*/

            this.CreateButtons(models);

            var allInteractables = GameObject.FindObjectsOfType<Interactable>();
            foreach (var iteractable in allInteractables)
            {

                iteractable.OnClick.AddListener(() =>
                {
                    int value;
                    bool hasValue = modelDictionary.TryGetValue(iteractable.gameObject.name, out value);
                    if (hasValue)
                    {
                        Debug.Log(Time.time + ": " + iteractable.gameObject.name + " was clicked " + "id: " + value);

                        StartCoroutine(this.serverGateway.GetModelSelected(value, (m) =>
                        {
                            if (m != null)
                            {
                                DicomViewerObjImporter newDicomViewerObjImporter = Instantiate(dicomViewerObjImporter);
                                string dicomFolderName = "ACTright";
                                newDicomViewerObjImporter.SetModel(m, dicomFolderName);
                                newDicomViewerObjImporter.gameObject.SetActive(true);
                            }
                        }));
                    }
                    else
                    {
                        Debug.Log("Key not present");
                    }
                });
            }

        }));
    }

    void Update()
    {
        GridObjectCollection gridObjectCollection = buttonCollection.GetComponent<GridObjectCollection>();
        gridObjectCollection.UpdateCollection();
    }

    private void CreateButtons(string[] models)
    {
        for (int i = 0; i < models.Length; i++)
        {
            GameObject button = Instantiate(buttonPrefab);
            button.transform.SetParent(buttonCollection.transform);
            string buttonName = models[i].Trim('"');
            button.name = buttonName;
            button.GetComponentInChildren<TMP_Text>().text = buttonName; //TMP_Text comprende TextPro e TextProUGUI nel nuovo aggiornamento
            button.SetActive(true);

            modelDictionary.Add(buttonName, i);
        }
    }
}
