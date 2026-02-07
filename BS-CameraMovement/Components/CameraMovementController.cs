using System.IO;
using UnityEngine;
using Zenject;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BS_CameraMovement.Configuration;

namespace BS_CameraMovement.Components
{
    public class CameraMovementController : IInitializable, ITickable
    {
        private BeatmapProjectManager _beatmapProjectManager;
        private IAudioTimeSource _audioTimeSyncController;
        private CameraMovement _cameraMovement;
        private AudioDataModel _audioDataModel;
        private Camera _mainCamera;
        private string _scriptPath;
        private bool _isActive;
        public float beforeSeconds;

        public bool IsEnabled
        {
            get => PluginConfig.Instance.enable;
            set
            {
                if (PluginConfig.Instance.enable == value) return;
                PluginConfig.Instance.enable = value;
                UpdateCameraState();
            }
        }

        public CameraMovementController(
            BeatmapProjectManager beatmapProjectManager,
            IAudioTimeSource audioTimeSyncController,
            AudioDataModel audioDataModel)
        {
            _beatmapProjectManager = beatmapProjectManager;
            _audioTimeSyncController = audioTimeSyncController;
            _cameraMovement = new CameraMovement();
            _audioDataModel = audioDataModel;
       }

        public void Initialize()
        {
            Debug.Log("BS-CameraMovement: CameraMovementController Initializing...");
            _mainCamera = GameObject.Find("Wrapper/MainCamera").GetComponent<Camera>();
            UpdateCameraState();
            Debug.Log($"usePhysicalProperties:{_mainCamera.usePhysicalProperties}");

            // Get project path and script path
            string projectPath = _beatmapProjectManager.originalBeatmapProject;
            if (string.IsNullOrEmpty(projectPath))
            {
                 // Fallback to working project if original is not set (e.g. new project not saved yet?)
                 projectPath = _beatmapProjectManager.workingBeatmapProject;
            }

            if (!string.IsNullOrEmpty(projectPath))
            {
                _scriptPath = Path.Combine(projectPath, "SongScript.json");
                Debug.Log($"BS-CameraMovement: Looking for script at {_scriptPath}");
                
                if (File.Exists(_scriptPath))
                {
                    bool loaded = _cameraMovement.LoadCameraData(_scriptPath);
                    if (loaded)
                    {
                        Debug.Log("BS-CameraMovement: SongScript.json loaded successfully.");
                        _isActive = true;
                    }
                    else
                    {
                        Debug.LogWarning("BS-CameraMovement: Failed to load SongScript.json data.");
                    }
                }
                else
                {
                    Debug.Log("BS-CameraMovement: SongScript.json not found in project directory.");
                }
            }
            else
            {
                Debug.LogError("BS-CameraMovement: Could not determine project path.");
            }
        }

        private void UpdateCameraState()
        {
            if (_mainCamera == null) return;
            if (PluginConfig.Instance.enable)
            {
                _mainCamera.rect = new Rect(0, 0.23f, 1f, 0.77f);
            }
            else
            {
                _mainCamera.rect = new Rect(0, 0, 1f, 1f);
                Camera.main.transform.position = new Vector3(0, 2, -6);
                Camera.main.transform.eulerAngles = new Vector3(15, 0, 0);
                _mainCamera.fieldOfView = 60;
            }
        }

        public void Tick()
        {
            if (!PluginConfig.Instance.enable || !_isActive || _mainCamera == null) return;
           
            float currentSeconds = _audioDataModel.bpmData.BeatToSeconds(_audioTimeSyncController.songTime);
            if (beforeSeconds == currentSeconds)
            {
                return;
            }
            if (currentSeconds < beforeSeconds)
            {
                _cameraMovement.MovementPositionReset();
                beforeSeconds = 0;
            }
            beforeSeconds = currentSeconds;
            _cameraMovement.CameraUpdate(currentSeconds, _mainCamera);
        }
    }
}
