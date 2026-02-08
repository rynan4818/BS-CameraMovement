using BS_CameraMovement.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace BS_CameraMovement.Components
{
    public class PlayerCameraController : MonoBehaviour
    {
        private OscCameraReceiver _receiver;
        private IGamePause _gamePause;
        private List<Camera> _cameras = new List<Camera>();
        private string[] _cameraNames = new string[0];
        private int _selectedCameraIndex = 0;
        private Vector2 _scrollPosition;
        private bool _showGui = true;
        private Rect _windowRect = new Rect(10, 10, 280, 300);

        [Inject]
        public void Construct(OscCameraReceiver oscCameraReceiver, IGamePause gamePause)
        {
            _receiver = oscCameraReceiver;
            _gamePause = gamePause;
        }

        public void Start()
        {
            if (!PluginConfig.Instance.playerOsc)
            {
                _showGui = false;
                enabled = false;
                return;
            }
            RefreshCameraList();
            StartOsc();
        }

        private void StartOsc()
        {
            if (_receiver != null) return;
            _receiver.Start(PluginConfig.Instance.oscPort);
        }

        private void StopOsc()
        {
            if (_receiver != null)
            {
                _receiver.Dispose();
                _receiver = null;
            }
        }

        private void RefreshCameraList()
        {
            _cameras.Clear();
            var allCameras = Camera.allCameras;
            foreach (var cam in allCameras)
            {
                if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    _cameras.Add(cam);
                }
            }
            _cameraNames = _cameras.Select(c => c.gameObject.name).ToArray();
            if (_selectedCameraIndex >= _cameras.Count)
                _selectedCameraIndex = 0;
        }

        public void Update()
        {
            if (_receiver == null || !_receiver.HasData || _cameras.Count == 0 || !_gamePause.isPaused) return;
            if (_selectedCameraIndex < 0 || _selectedCameraIndex >= _cameras.Count) return;

            var cam = _cameras[_selectedCameraIndex];
            if (cam == null)
            {
                RefreshCameraList();
                return;
            }

            var (pos, rot, fov, _) = _receiver.ReadData();
            _receiver.ClearData();

            cam.transform.position = pos;
            cam.transform.rotation = rot;
            if (fov > 0)
            {
                cam.fieldOfView = fov;
            }
        }

        public void OnGUI()
        {
            if (!_showGui) return;
            _windowRect = GUI.Window(1002, _windowRect, DrawWindow, "Player OSC Camera");
        }

        private void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginVertical();

            // 無効化ボタン
            if (GUILayout.Button("Disable Player OSC"))
            {
                PluginConfig.Instance.playerOsc = false;
                StopOsc();
                _showGui = false;
                enabled = false;
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Space(5);

            // カメラ一覧更新ボタン
            if (GUILayout.Button("Refresh Camera List"))
            {
                RefreshCameraList();
            }

            GUILayout.Space(5);
            GUILayout.Label($"Cameras ({_cameras.Count}):");

            // カメラ選択リストボックス
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(160));
            if (_cameraNames.Length > 0)
            {
                int newIndex = GUILayout.SelectionGrid(_selectedCameraIndex, _cameraNames, 1);
                if (newIndex != _selectedCameraIndex)
                {
                    _selectedCameraIndex = newIndex;
                }
            }
            else
            {
                GUILayout.Label("No active cameras found.");
            }
            GUILayout.EndScrollView();

            // 選択中のカメラ情報
            if (_selectedCameraIndex >= 0 && _selectedCameraIndex < _cameras.Count && _cameras[_selectedCameraIndex] != null)
            {
                var cam = _cameras[_selectedCameraIndex];
                GUILayout.Label($"Selected: {cam.gameObject.name}");
            }

            GUILayout.EndVertical();
        }

        public void OnDestroy()
        {
            StopOsc();
        }
    }
}