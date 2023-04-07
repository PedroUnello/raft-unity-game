using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace Assets.Script.Comm
{
    public class RaftInitializer : MonoBehaviour
    {

        enum System
        {
            Windows,
            Linux,
            MacOS,
        }

        private System localOS;
        readonly Dictionary<System, string> SystemExtension = new Dictionary<System, string>
        {
            { System.Windows, ".exe"},
            { System.Linux, "" },
            { System.MacOS, ""},
        };

        private Process handle;

        void Awake()
        {
            if (SystemInfo.operatingSystem.Contains("Windows"))
            {
                localOS = System.Windows;
            }
            else if (SystemInfo.operatingSystem.Contains("Linux"))
            {
                localOS = System.Linux;
            }
            else if (SystemInfo.operatingSystem.Contains("Mac"))
            {
                localOS = System.MacOS;
            }
        }

        public void InitiazeRaftServer(string localIP, string[] externalIP)
        {
            //Start compiled Golang program

            //process.StartInfo.RedirectStandardInput = true;
            //process.StartInfo.CreateNoWindow = true;

            //process.EnableRaisingEvents = false;

            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;

            //process.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
            //process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);

            string pathName = Application.productName + Application.buildGUID + Application.identifier + Random.Range(0, 99999);

            GetComponent<RaftManager>().StartCommunication(pathName);

            string[] args =
            {
                pathName,
                localIP,
                string.Join(" ", externalIP)
            };

            ProcessStartInfo startInfo = new(Application.dataPath + "/Raft/go_conn" + SystemExtension.GetValueOrDefault(localOS, ""), string.Join(" ", args));
            //startInfo.CreateNoWindow = true;
            //startInfo.UseShellExecute = false;
            handle = Process.Start(startInfo);
        }

        private void OnApplicationQuit()
        {
            UnityEngine.Debug.Log("Ending Process");

            if (handle != null && !handle.HasExited)
            {
                handle.Kill();
                handle.WaitForExit();
                handle.Dispose();
            }
        }

    }
}