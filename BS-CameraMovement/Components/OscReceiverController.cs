using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BS_CameraMovement.Configuration;
using BS_CameraMovement.Osc;
using System;
using UnityEngine;
using Zenject;

namespace BS_CameraMovement.Components
{
    public class OscReceiverController : IInitializable, ITickable, IDisposable
    {
        private SignalBus _signalBus;
        private IReadonlyBeatmapState _readonlyBeatmapState;
        private OscServer _server;
        private Camera _mainCameraFov;
        private Camera _mainCameraTrans;

        // Cache for received data
        private readonly object _lock = new object();
        private Vector3 _targetPos;
        private Quaternion _targetRot;
        private float _targetFov;
        private float _targetSongTime;
        public bool hasData { get; set; } = false;

        public OscReceiverController(SignalBus signalBus, IReadonlyBeatmapState readonlyBeatmapState)
        {
            _signalBus = signalBus;
            _readonlyBeatmapState = readonlyBeatmapState;
        }

        public void Initialize()
        {
            // Find Main Camera if not injected
            if (_mainCameraFov == null)
            {
                var cameraObj = GameObject.Find("Wrapper/MainCamera");
                if (cameraObj != null)
                {
                    _mainCameraFov = cameraObj.GetComponent<Camera>();
                }
            }
            if (_mainCameraTrans == null)
            {
                _mainCameraTrans = Camera.main;
            }

            // Start OSC Server
            try
            {
                _server = new OscServer(PluginConfig.Instance.ocsPort);
                _server.OnMessageReceived += OnMessageReceived;
                _server.Start();
                Plugin.Log.Info($"OscReceiverController: Server started on port {PluginConfig.Instance.ocsPort}.");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"OscReceiverController: Failed to start server. {ex.Message}");
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
                            // Format: "Camera" (string), pos x, y, z, rot x, y, z, w, fov, currentSongBeatTime
                            if (message.Arguments.Count >= 9)
                            {
                                // Skip index 0 (string "Camera")
                                float x = message.GetFloat(1);
                                float y = message.GetFloat(2);
                                float z = message.GetFloat(3);
                                _targetPos = new Vector3(x, y, z);
                                
                                float rx = message.GetFloat(4);
                                float ry = message.GetFloat(5);
                                float rz = message.GetFloat(6);
                                float rw = message.GetFloat(7);
                                _targetRot = new Quaternion(rx, ry, rz, rw);
                                
                                _targetFov = message.GetFloat(8);
                                _targetSongTime = message.GetFloat(9);
                                hasData = true;
                            }
                            break;
                    }
                } 
                catch (Exception e) 
                {
                    Plugin.Log.Error($"OscReceiverController: Error parsing OSC message {message.Address}: {e.Message}");
                }
            }
        }

        public void Tick()
        {
            if (_mainCameraTrans == null || _mainCameraFov == null || !hasData) return;
            lock (_lock)
            {
                if (_readonlyBeatmapState.isPlaying)
                    hasData = false;
                else
                {
                    _mainCameraTrans.transform.position = _targetPos;
                    _mainCameraTrans.transform.rotation = _targetRot;
                    if (_targetFov > 0)
                    {
                        _mainCameraFov.fieldOfView = _targetFov;
                    }
                    _signalBus.Fire<UpdatePlayHeadSignal>(
                        new UpdatePlayHeadSignal(_targetSongTime, UpdatePlayHeadSignal.SnapType.None, false)
                    );
                }
            }
        }

        public void Dispose()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }
        }
    }
}
