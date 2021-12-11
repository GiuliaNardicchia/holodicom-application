using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Sockets;
using WebSocketSharp;

public class ServerGateway : MonoBehaviour
{
    private const string LIMIT = "l:";
    private const string ID = "i:";

    private const string url = "ws://localhost:8080"; //ws://192.168.40.100:8080
    private WebSocket ws;

    private List<string> modelNames;
    private List<string> dicom;
    private string model;
    private bool selected;
    //private bool first;

    public void Init()
    {
        this.modelNames = new List<string>();
        this.dicom = new List<string>();
        this.model = null;
        this.selected = false;
        //this.first = false;

        ws = new WebSocket(url);
        ws.OnOpen += (sender, e) => Debug.Log("Socket connected!");
        ws.OnError += (sender, e) => Debug.Log("Error: " + e.Message);
        ws.OnClose += (sender, e) => Debug.Log("Socket connection closed " + e.Code + " " + e.Reason);
        ws.OnMessage += OnMessage;
        ws.Connect();
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        //Debug.Log("Message Received from " + ((WebSocket)sender).Url);
        if (!this.selected)
        {
            this.modelNames.Add(e.Data);
        }
        else
        {
            this.model = e.Data;

            //per caricare anche i file dicom, ma si blocca
            /*if (!this.first)
            {
                this.model = e.Data;
                this.first = this.first;
            }
            else
            {
                this.dicom.Add(e.Data);
            }*/

        }
    }

    public void SetLimit(int limit)
    {
        this.selected = false;
        ws.Send(LIMIT + limit.ToString());
    }

    public void SetId(int id)
    {
        this.selected = true;
        ws.Send(ID + id.ToString());
    }

    public void ResetModel()
    {
        this.model = null;
    }

    public List<string> GetListModel()
    {
        return this.modelNames;
    }

    public string GetModel()
    {
        return this.model;
    }
}
