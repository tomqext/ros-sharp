/*
© Siemens AG, 2017-2019
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

<http://www.apache.org/licenses/LICENSE-2.0>.

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections;
using System.Threading;
using RosSharp.RosBridgeClient.Protocols;
using UnityEngine;
using UnityEngine.Serialization;

namespace RosSharp.RosBridgeClient
{
    public class RosConnector : MonoBehaviour
    {
        public int SecondsTimeout = 10;

        public RosSocket RosSocket { get; private set; }
        public RosSocket.SerializerEnum Serializer;
        public Protocol protocol;
        [FormerlySerializedAs("RosBridgeServerUrl")] public string IPAddress = "ws://192.168.0.1:9090";

        public ManualResetEvent IsConnected { get; private set; }

        public bool connection_reset = false;   /*NSXX*/
        private static ManualResetEvent tester = new ManualResetEvent(false);

        public volatile bool ros_connected = false;      /*NSXX*/
        public volatile bool ros_unavail = false;        /*NSXX*/

        //public UIInteractionManager _uimanager;

        private void Awake()
        {
            Connect();
        }


        public void Connect()
        {
            IsConnected = new ManualResetEvent(false);
            new Thread(ConnectAndWait).Start();
        }

        protected void ConnectAndWait()
        {
            ros_connected = false;      /*NSXX*/
            ros_unavail = false;        /*NSXX*/
            RosSocket = ConnectToRos(protocol, IPAddress, OnConnected, OnClosed, Serializer);

            if (!IsConnected.WaitOne(SecondsTimeout * 1000))
            {
                connection_reset = false;
                ros_unavail = true;
                Debug.LogWarning("Failed to connect to RosBridge at: " + IPAddress);
            }

        }

        public static RosSocket ConnectToRos(Protocol protocolType, string serverUrl, EventHandler onConnected = null, EventHandler onClosed = null, RosSocket.SerializerEnum serializer = RosSocket.SerializerEnum.Microsoft)
        {
            tester.WaitOne(1000);
            IProtocol protocol = ProtocolInitializer.GetProtocol(protocolType, serverUrl);
            protocol.OnConnected += onConnected;
            protocol.OnClosed += onClosed;

            return new RosSocket(protocol, serializer);
        }

        IEnumerator Co_Reconnect()
        {
            if (_activeConnection)
            {
                Close();
                while (_activeConnection)
                {
                    yield return null;
                }
            }
            Connect();
        }
        [ContextMenu("Reconnect")]
        public void Reconnect()
        {
            connection_reset = true;
            protocol = new Protocol();
            IsConnected.Reset();
            _activeConnection = false;
            RosSocket.Close();
            StartCoroutine(Co_Reconnect());
        }


        public void Close()
        {
            RosSocket.Close();
        }


        private void OnApplicationQuit()
        {
            Close();
        }

        private bool _activeConnection;
        //public event Action Connected;

        private void OnConnected(object sender, EventArgs e)
        {
            IsConnected.Set();
            _activeConnection = true;
            connection_reset = false;
            ros_connected = true;
            Debug.Log("Connected to RosBridge: " + IPAddress);
            //Connected?.Invoke();
           // _uimanager.Connected();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            IsConnected.Reset();
            ros_connected = false;
            _activeConnection = false;
            Debug.Log("Disconnected from RosBridge: " + IPAddress);
        }
    }
}
