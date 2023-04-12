using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Text;
using Assets.Script.Core;
using System.Collections.Concurrent;

namespace Assets.Script.Comm
{
    public class RaftManager : MonoBehaviour
    {

        private static RaftManager _instance;
        public static RaftManager Instance => _instance;

        private ConcurrentQueue<GameLog> _received;
        private ConcurrentQueue<GameLog> _send;
        
        public GameLog NewestAction
        {
            get
            {
                if (_received.Count > 0)
                {
                    if (_received.TryDequeue(out GameLog last)) return last;
                }

                return null;

            }
        }

        public void AppendAction(GameLog action)
        {
            _send.Enqueue(action);
        }

        private int _actionCounter;
        private readonly int _messageSize = 512;

        private NamedPipeServerStream replicationEntryPoint;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private Thread recvThread, connThread;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
            }
            else
            {
                _instance = this;
            }

            DontDestroyOnLoad(gameObject);

            _received = new();
            _send = new();

            _actionCounter = 2;

        }

        void CommunicateAsync()
        {
            replicationEntryPoint.WaitForConnection();

            while (replicationEntryPoint.IsConnected)
            {

                if (_send.Count > 0)
                {
                    if (_send.TryDequeue(out GameLog gL))
                    {
                        gL.ActionId = _actionCounter++;

                        char[] outBuf = new char[_messageSize];
                        string sendMsg = JsonUtility.ToJson(gL);
                        sendMsg.ToCharArray().CopyTo(outBuf, 0);

                        streamWriter.WriteLine(outBuf, 0, _messageSize);
                        streamWriter.Flush();
                    }
                }
            }
        }

        void ReceiveAsync()
        {
            replicationEntryPoint.WaitForConnection();

            while (replicationEntryPoint.IsConnected)
            {
                char[] inBuf = new char[_messageSize];
                streamReader.Read(inBuf);

                string data = new(inBuf);
                if (data.Length > 0)// && !string.IsNullOrWhiteSpace(data) && !string.IsNullOrEmpty(data))
                {
                    try
                    {
                        GameLog gL = JsonUtility.FromJson<GameLog>(data);
                        _received.Enqueue(gL);
                    }
                    catch (System.ArgumentException)
                    {
                        ;// Debug.Log("Insufficient Data: -" + data + "-"); 
                    }

                }
            }
        }

        public void StartCommunication(string pathName)
        {
                                                                        //PipeTransmissionMode.Message
            replicationEntryPoint = new(pathName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            streamReader = new(replicationEntryPoint, Encoding.ASCII);
            streamWriter = new(replicationEntryPoint, Encoding.ASCII);

            recvThread = new(ReceiveAsync);
            recvThread.IsBackground = true;
            recvThread.Start();
            connThread = new(CommunicateAsync);
            connThread.IsBackground = true;
            connThread.Start();
        }

        private void OnApplicationQuit()
        {
            if (recvThread != null && recvThread.IsAlive)
            {

                recvThread.Abort();
            }

            if (connThread != null && connThread.IsAlive)
            {
                connThread.Abort();
            }

            while (!_send.IsEmpty && replicationEntryPoint.IsConnected)
            {
                if (_send.TryDequeue(out GameLog gL))
                {
                    gL.ActionId = _actionCounter++;

                    char[] outBuf = new char[_messageSize];
                    string sendMsg = JsonUtility.ToJson(gL);
                    sendMsg.ToCharArray().CopyTo(outBuf, 0);

                    streamWriter.WriteLine(outBuf, 0, _messageSize);
                    streamWriter.Flush();
                }
                else
                {
                    break;
                }
            }

            if (replicationEntryPoint != null)
            {
                if (replicationEntryPoint.IsConnected) replicationEntryPoint.Disconnect();
                streamReader.Close();
                streamReader.Dispose();
                replicationEntryPoint.Close();
                replicationEntryPoint.Dispose();
            }
        }
    }
}
