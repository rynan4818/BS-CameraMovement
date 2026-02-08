using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BS_CameraMovement.Configuration;
using System;
using UnityEngine;
using Zenject;

namespace BS_CameraMovement.Components
{
    public class OscReceiverController : IInitializable, ITickable, IDisposable
    {
        private SignalBus _signalBus;
        private IReadonlyBeatmapState _readonlyBeatmapState;
        private OscCameraReceiver _receiver;
        private Camera _mainCameraFov;
        private Camera _mainCameraTrans;

        public OscReceiverController(SignalBus signalBus, IReadonlyBeatmapState readonlyBeatmapState, OscCameraReceiver oscCameraReceiver)
        {
            _signalBus = signalBus;
            _readonlyBeatmapState = readonlyBeatmapState;
            _receiver = oscCameraReceiver;
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
            _receiver.Start(PluginConfig.Instance.oscPort);
        }

        public void Tick()
        {
            if (_mainCameraTrans == null || _mainCameraFov == null || _receiver == null || !_receiver.HasData) return;

            var (pos, rot, fov, songTime) = _receiver.ReadData();

            // エディタの再生 停止状態の判定方法
            // https://github.com/rynan4818/BS-CameraMovement/wiki/%E3%82%A8%E3%83%87%E3%82%A3%E3%82%BF%E3%81%AE%E5%86%8D%E7%94%9F-%E5%81%9C%E6%AD%A2%E7%8A%B6%E6%85%8B%E3%81%AE%E5%88%A4%E5%AE%9A%E6%96%B9%E6%B3%95
            if (_readonlyBeatmapState.isPlaying)
            {
                _receiver.ClearData();
            }
            else
            {
                _mainCameraTrans.transform.position = pos;
                _mainCameraTrans.transform.rotation = rot;
                if (fov > 0)
                {
                    _mainCameraFov.fieldOfView = fov;
                }
                // エディタの再生位置を設定する方法
                // https://github.com/rynan4818/BS-CameraMovement/wiki/%E3%82%A8%E3%83%87%E3%82%A3%E3%82%BF%E3%81%AE%E5%86%8D%E7%94%9F%E4%BD%8D%E7%BD%AE%E3%82%92%E8%A8%AD%E5%AE%9A%E3%81%99%E3%82%8B%E6%96%B9%E6%B3%95
                _signalBus.Fire<UpdatePlayHeadSignal>(
                    new UpdatePlayHeadSignal(songTime, UpdatePlayHeadSignal.SnapType.None, false)
                );
            }
        }

        public void Dispose()
        {
            if (_receiver != null)
            {
                _receiver.Dispose();
                _receiver = null;
            }
        }
    }
}