using System;
using System.IO;
using UnityEngine;
using Zenject;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BS_CameraMovement.Configuration;

namespace BS_CameraMovement.Components
{
    public class CameraMovementController : IInitializable, ILateTickable, IDisposable
    {
        private BeatmapProjectManager _beatmapProjectManager;
        private IAudioTimeSource _audioTimeSyncController;
        private CameraMovement _cameraMovement;
        private AudioDataModel _audioDataModel;
        private OscCameraReceiver _receiver;
        private Camera _mainCamera;
        private string _scriptPath;
        private bool _isActive;
        public float beforeSeconds;

        private FileSystemWatcher _fileWatcher;
        private bool _reloadPending;
        private bool disposedValue;

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
            CameraMovement cameraMovement,
            AudioDataModel audioDataModel,
            OscCameraReceiver oscCameraReceiver)
        {
            _beatmapProjectManager = beatmapProjectManager;
            _audioTimeSyncController = audioTimeSyncController;
            _cameraMovement = cameraMovement;
            _audioDataModel = audioDataModel;
            _receiver = oscCameraReceiver;
        }

        public void Initialize()
        {
            Plugin.Log.Info("BS-CameraMovement: CameraMovementController Initializing...");
            _mainCamera = GameObject.Find("Wrapper/MainCamera").GetComponent<Camera>();
            UpdateCameraState();
            Plugin.Log.Info($"usePhysicalProperties:{_mainCamera.usePhysicalProperties}");

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
                Plugin.Log.Info($"BS-CameraMovement: Looking for script at {_scriptPath}");
                
                if (File.Exists(_scriptPath))
                {
                    bool loaded = _cameraMovement.LoadCameraData(_scriptPath);
                    if (loaded)
                    {
                        Plugin.Log.Info("BS-CameraMovement: SongScript.json loaded successfully.");
                        _isActive = true;
                        InitializeWatcher(projectPath);
                    }
                    else
                    {
                        Plugin.Log.Warn("BS-CameraMovement: Failed to load SongScript.json data.");
                    }
                }
                else
                {
                    Plugin.Log.Info("BS-CameraMovement: SongScript.json not found in project directory.");
                }
            }
            else
            {
                Plugin.Log.Error("BS-CameraMovement: Could not determine project path.");
            }
        }

        private void InitializeWatcher(string directory)
        {
            if (_fileWatcher != null) return;
            try
            {
                _fileWatcher = new FileSystemWatcher
                {
                    Path = directory,
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "SongScript.json",
                    EnableRaisingEvents = true
                };
                _fileWatcher.Changed += OnFileChanged;
                Plugin.Log.Info($"BS-CameraMovement: Started watching {directory} for SongScript.json changes.");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"BS-CameraMovement: Failed to initialize file watcher. {ex.Message}");
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            _reloadPending = true;
        }

        private void UpdateCameraState()
        {
            if (_mainCamera == null) return;
            if (PluginConfig.Instance.enable)
            {
                _mainCamera.rect = new Rect(0.05f, 0.23f, 0.95f, 0.77f);
            }
            else
            {
                _mainCamera.rect = new Rect(0, 0, 1f, 1f);
                // TransformはCamera.main、FOVはWrapper/MainCameraに設定が必要な理由
                // https://github.com/rynan4818/BS-CameraMovement/wiki/%E3%82%AB%E3%83%A1%E3%83%A9%E3%81%AE%E5%88%B6%E5%BE%A1%E6%96%B9%E6%B3%95
                Camera.main.transform.position = new Vector3(0, 2, -6);
                Camera.main.transform.eulerAngles = new Vector3(15, 0, 0);
                _mainCamera.fieldOfView = 60;
            }
        }

        public void LateTick()
        {
            if (_reloadPending)
            {
                _reloadPending = false;
                Plugin.Log.Info("BS-CameraMovement: Detected change in SongScript.json. Reloading...");
                if (_cameraMovement.LoadCameraData(_scriptPath))
                {
                    Plugin.Log.Info("BS-CameraMovement: Reloaded successfully.");
                }
                else
                {
                    Plugin.Log.Warn("BS-CameraMovement: Failed to reload data.");
                }
            }

            if (!PluginConfig.Instance.enable || !_isActive || _mainCamera == null) return;
           
            float currentSeconds = _audioDataModel.bpmData.BeatToSeconds(_audioTimeSyncController.songTime);
            if (beforeSeconds == currentSeconds)
            {
                _receiver.ClearData();
                return;
            }
            if (currentSeconds < beforeSeconds)
            {
                _cameraMovement.MovementPositionReset();
                beforeSeconds = 0;
            }
            beforeSeconds = currentSeconds;
            if (_receiver.HasData)
            {
                _receiver.ClearData();
                return;
            }
            _receiver.ClearData();
            _cameraMovement.CameraUpdate(currentSeconds, _mainCamera);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                if (_fileWatcher != null)
                {
                    _fileWatcher.Changed -= OnFileChanged;
                    _fileWatcher.Dispose();
                    _fileWatcher = null;
                }
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~CameraMovementController()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
