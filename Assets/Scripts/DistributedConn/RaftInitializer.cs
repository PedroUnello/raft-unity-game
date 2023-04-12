using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Script.Comm
{
    public class RaftInitializer : MonoBehaviour
    {

        private static RaftInitializer _instance;
        public static RaftInitializer Instance => _instance;

        enum System
        {
            Windows,
            Linux,
            MacOS,
        }

        private System localOS;
        readonly Dictionary<System, string> SystemExtension = new()
        {
            { System.Windows, ".exe"},
            { System.Linux, "" },
            { System.MacOS, ""},
        };

        private Process handle;

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

            string pathName = Application.productName + Application.buildGUID + Application.identifier + Random.Range(0, 99999);

            string[] args =
            {
                pathName,
                localIP,
                string.Join(" ", externalIP)
            };

            //ProcessStartInfo startInfo = new(Application.dataPath + "/Raft/go_conn" + SystemExtension.GetValueOrDefault(localOS, ""), string.Join(" ", args));
            ProcessStartInfo startInfo = new("CMD.exe", "/K " + Application.dataPath + "/Raft/go_conn" + SystemExtension.GetValueOrDefault(localOS, "") + " " + string.Join(" ", args));
            
            /*
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            */

            handle = Process.Start(startInfo);
            
            //handle.EnableRaisingEvents = false;
            //handle.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
            //handle.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);

            GetComponent<RaftManager>().StartCommunication(pathName);

        }

        private void OnApplicationQuit()
        {
            UnityEngine.Debug.Log("Ending Process");

            if (handle != null && !handle.HasExited)
            {
                //handle.StandardInput.WriteLine("\x3");
                //Process.Start("CMD.exe", "/K taskkill /F /pid " + handle.Id.ToString());
                //handle.Close();
                //handle.CloseMainWindow();

                handle.Kill();
                handle.WaitForExit();
                handle.Dispose();
            }
        }
    }
}