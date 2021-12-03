using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class ServerGateway : MonoBehaviour
{
    /*void Start()
    {
        string uri = "http://192.168.40.100:8080/";
        UnityEngine.WSA.Launcher.LaunchUri(uri, false);
    }*/

    private string address = "http://localhost:8000/";

    private string[] ProcessServerResponse(string response)
    {
        response = response.TrimStart('[').TrimEnd(']');
        string[] models = response.Split(',');
        return models;
        //process the file json
        //import SimpleJSON.cs -> parser of a JSON object
    }

    public IEnumerator GetModels(int limit, Action<string[]> callback)
    {
        yield return StartCoroutine(GetListModel(this.address, limit, callback));
    }

    IEnumerator GetListModel(string address, int limit, Action<string[]> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(this.address + "model?limit=" + limit);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Something went wrong: " + www.error);
        }
        else
        {
            callback(ProcessServerResponse(www.downloadHandler.text));
        }
    }

    public IEnumerator GetModelSelected(int id, Action<string> callback)
    {
        yield return StartCoroutine(GetModelById(this.address, id.ToString(), callback));
    }

    IEnumerator GetModelById(string address, string id, Action<string> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(address + "model?id=" + id);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Something went wrong: " + www.error);
            if (callback != null)
                callback(www.error);
        }
        else
        {
            if (callback != null)
                callback(www.downloadHandler.text);
        }
    }
}
