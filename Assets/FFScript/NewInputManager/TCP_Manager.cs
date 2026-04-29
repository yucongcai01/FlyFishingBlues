using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;

public class TCP_Manager : MonoBehaviour
{
    [Header("TCP Settings")]
    [SerializeField] private string ipAddress = "127.0.0.1";
    [SerializeField] private int port = 8080;
    public float reconnectInterval = 2f;

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private byte[] receiveBuffer = new byte[1024];
    private StringBuilder messageBuilder = new StringBuilder();

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    private bool isConnected => tcpClient != null && tcpClient.Connected;
    // Start is called before the first frame update
    void Start()
    {
        ConnectToServer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            tcpClient.BeginConnect(ipAddress, port, new AsyncCallback(OnConnected), null);
            Debug.Log("Connecting to server...");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
            Reconnect();
        }
    }

    private void Reconnect()
    {
        if (isConnected)
        {
            tcpClient.Close();
            tcpClient = null;
        }
        Invoke("ConnectToServer", reconnectInterval);
        Debug.Log("Reconnecting to server...");
    }

    private void OnConnected(IAsyncResult result)
    {
        if (tcpClient.Connected)
        {
            Debug.Log("Connected to server");
            networkStream = tcpClient.GetStream();
            networkStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, new AsyncCallback(OnDataReceived), null);
        }
        else
        {
            Debug.LogError("Failed to connect to server");
            Reconnect();
        }
    }

    private void OnDataReceived(IAsyncResult result)
    {
        if (!isConnected) return;

        try
        {
            int bytesRead = networkStream.EndRead(result);
            if (bytesRead > 0)
            {
                //Debug.Log("Data received: " + bytesRead);
                //Debug.Log("Data: " + Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead));
                string data = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                messageBuilder.Append(data);

                string content = messageBuilder.ToString();
                int newlineIndex;
                while ((newlineIndex = content.IndexOf('\n')) >= 0)
                {
                    string line = content.Substring(0, newlineIndex);
                    messageQueue.Enqueue(line);
                    content = content.Substring(newlineIndex + 1);
                }
                messageBuilder.Clear();
                messageBuilder.Append(content);
                networkStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, new AsyncCallback(OnDataReceived), null);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in OnDataReceived: " + e.Message);
            CloseConnection();
        }
    }

    private void CloseConnection()
    {
        networkStream?.Close();
        tcpClient?.Close();
        tcpClient = null;
        networkStream = null;
        Debug.Log("Connection closed");
    }

    void OnDestroy()
    {
        CloseConnection();
    }

    public bool TryGetMessage(out string message)
    {
        return messageQueue.TryDequeue(out message);
    }
}
