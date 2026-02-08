using BS_CameraMovement.Configuration;
using BS_CameraMovement.Osc;
using System;
using UnityEngine;

namespace BS_CameraMovement.Components
{
    /// <summary>
    /// OSC受信でカメラデータを受け取る共通ヘルパークラス。
    /// OscReceiverController(エディタ)とPlayerCameraController(ゲームプレイ)の両方で使用する。
    /// </summary>
    public class OscCameraReceiver : IDisposable
    {
        private OscServer _server;
        private readonly object _lock = new object();

        public Vector3 TargetPos { get; private set; }
        public Quaternion TargetRot { get; private set; }
        public float TargetFov { get; private set; }
        public float TargetSongTime { get; private set; }
        public bool HasData { get; set; } = false;

        public void Start(int port)
        {
            if (_server != null) return;
            try
            {
                _server = new OscServer(port);
                _server.OnMessageReceived += OnMessageReceived;
                _server.Start();
                Plugin.Log.Info($"OscCameraReceiver: Server started on port {port}.");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"OscCameraReceiver: Failed to start server. {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
                Plugin.Log.Info("OscCameraReceiver: Server stopped.");
            }
        }

        private void OnMessageReceived(OscMessage message)
        {
            lock (_lock)
            {
                try
                {
                    switch (message.Address)
                    {
                        case "/camera/info":
                            if (message.Arguments.Count >= 9)
                            {
                                float x = message.GetFloat(1);
                                float y = message.GetFloat(2);
                                float z = message.GetFloat(3);
                                TargetPos = new Vector3(x, y, z);

                                float rx = message.GetFloat(4);
                                float ry = message.GetFloat(5);
                                float rz = message.GetFloat(6);
                                float rw = message.GetFloat(7);
                                TargetRot = new Quaternion(rx, ry, rz, rw);

                                TargetFov = message.GetFloat(8);
                                TargetSongTime = message.GetFloat(9);
                                HasData = true;
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.Error($"OscCameraReceiver: Error parsing OSC message {message.Address}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// ロック付きでデータを読み取るヘルパー。読み取り後HasDataの制御は呼び出し側に委ねる。
        /// </summary>
        public (Vector3 pos, Quaternion rot, float fov, float songTime) ReadData()
        {
            lock (_lock)
            {
                return (TargetPos, TargetRot, TargetFov, TargetSongTime);
            }
        }

        public void ClearData()
        {
            lock (_lock)
            {
                HasData = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
