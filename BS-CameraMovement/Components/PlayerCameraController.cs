// 再生箇所のシーク処理は、denpadokeiさんのPracticePlugin(https://github.com/denpadokei/PracticePlugin)のソースコードをコピー・修正して使用しています。
// コピー元:https://github.com/denpadokei/PracticePlugin/blob/master/PracticePlugin/Models/SongSeekBeatmapHandler.cs
// PracticePluginの著作権表記・ライセンスは以下の通りです。
// https://github.com/denpadokei/PracticePlugin/blob/master/LICENSE

using BS_CameraMovement.Configuration;
using System.Linq;
using UnityEngine;
using Zenject;
using IPA.Utilities;
using HarmonyLib;

namespace BS_CameraMovement.Components
{
    public class PlayerCameraController : MonoBehaviour
    {
        private OscCameraReceiver _receiver;
        private AudioTimeSyncController _audioTimeSyncController;
        private BeatmapCallbacksController _beatmapCallbacksController;
        private NoteCutSoundEffectManager _noteCutSoundEffectManager;
        private BasicBeatmapObjectManager _beatmapObjectManager;

        private bool _showGui = true;
        private Rect _windowRect = new Rect(10, 10, 220, 60);

        private GameObject _cameraObject;
        private Camera _oscCamera;

        private static readonly string[] _destroyComponents = new string[]
        {
            "AudioListener", "LIV", "MainCamera", "MeshCollider",
            "TrackedPoseDriver", "DepthTextureController"
        };

        private DiContainer _container;
        private float _lastDataTime = -10f;
        private CanvasGroup _pauseMenuCanvasGroup;

        [Inject]
        public void Construct(
            OscCameraReceiver oscCameraReceiver,
            AudioTimeSyncController audioTimeSyncController,
            BeatmapCallbacksController beatmapCallbacksController,
            NoteCutSoundEffectManager noteCutSoundEffectManager,
            BasicBeatmapObjectManager beatmapObjectManager,
            DiContainer container)
        {
            _receiver = oscCameraReceiver;
            _audioTimeSyncController = audioTimeSyncController;
            _beatmapCallbacksController = beatmapCallbacksController;
            _noteCutSoundEffectManager = noteCutSoundEffectManager;
            _beatmapObjectManager = beatmapObjectManager;
            _container = container;
        }

        public void Start()
        {
            if (!PluginConfig.Instance.playerOsc)
            {
                _showGui = false;
                enabled = false;
                return;
            }
            CreateOscCamera();
            StartOsc();
        }

        private void StartOsc()
        {
            if (_receiver == null) return;
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

        /// <summary>
        /// MainCameraを複製してOSC専用カメラを作成する。
        /// TrackedPoseDriver等を除去し、最前面・全画面表示にする。
        /// </summary>
        private void CreateOscCamera()
        {
            var mainCamObj = Camera.main?.gameObject;
            if (mainCamObj == null)
            {
                Plugin.Log.Error("PlayerCameraController: Camera.main not found.");
                return;
            }

            _cameraObject = Instantiate(mainCamObj);
            _cameraObject.name = "BS-CameraMovement OSC Camera";
            _cameraObject.tag = "Untagged";
            _cameraObject.transform.SetParent(null);
            _cameraObject.transform.localScale = Vector3.one;
            _cameraObject.SetActive(false);

            // 子オブジェクトを全削除
            foreach (Transform child in _cameraObject.transform.Cast<Transform>().ToArray())
                Destroy(child.gameObject);

            // VRトラッキング等の不要コンポーネントを削除
            foreach (var component in _cameraObject.GetComponents<Behaviour>())
            {
                if (_destroyComponents.Contains(component.GetType().Name))
                    Destroy(component);
            }

            _oscCamera = _cameraObject.GetComponent<Camera>();
            _oscCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _oscCamera.depth = float.MaxValue; // 最前面
            _oscCamera.rect = new Rect(0, 0, 1, 1); // 全画面
            _oscCamera.enabled = true;

            // 初期位置
            _cameraObject.transform.position = new Vector3(0, 1.7f, -3f);
            _cameraObject.transform.rotation = Quaternion.Euler(15, 0, 0);
            _oscCamera.fieldOfView = 60f;

            _cameraObject.SetActive(true);
            Plugin.Log.Info("PlayerCameraController: OSC Camera created.");
        }

        private void DestroyOscCamera()
        {
            if (_cameraObject != null)
            {
                Destroy(_cameraObject);
                _cameraObject = null;
                _oscCamera = null;
                Plugin.Log.Info("PlayerCameraController: OSC Camera destroyed.");
            }
        }

        public void Update()
        {
            if (_receiver != null && _receiver.HasData)
            {
                _lastDataTime = Time.time;

                if (_oscCamera != null)
                {
                    var (pos, rot, fov, songTime) = _receiver.ReadData();
                    
                    _oscCamera.transform.position = pos;
                    _oscCamera.transform.rotation = rot;
                    if (fov > 0)
                    {
                        _oscCamera.fieldOfView = fov;
                    }

                    // 一時停止中に songTime が指定されていればジャンプ
                    if (_audioTimeSyncController != null && _audioTimeSyncController.state == AudioTimeSyncController.State.Paused)
                    {
                        if (songTime >= 0 && Mathf.Abs(songTime - _audioTimeSyncController.songTime) > Mathf.Epsilon)
                        {
                            JumpToTime(songTime);
                        }
                    }
                }
                // データ読み出し後にフラグを下ろす（カメラがnullでもフラグは消費する）
                _receiver.ClearData();
            }

            // 一時停止中のメニュー表示制御
            try
            {
                if (_audioTimeSyncController != null && _audioTimeSyncController.state == AudioTimeSyncController.State.Paused)
                {
                    UpdatePauseMenuVisibility();
                }
                else
                {
                    _pauseMenuCanvasGroup = null;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.Error($"Error in UpdatePauseMenuVisibility: {ex.Message}");
            }
        }

        private float _nextFindTime = 0f;

        private void UpdatePauseMenuVisibility()
        {
            if (_pauseMenuCanvasGroup == null)
            {
                // 毎フレーム検索すると重いため、見つからない場合は1秒間隔でリトライする
                if (Time.time < _nextFindTime) return;
                _nextFindTime = Time.time + 1.0f;

                // ユーザー指定のパスで検索を試みる
                var mainBar = GameObject.Find("Wrapper/StandardGameplay/PauseMenu/Wrapper/MenuWrapper/Canvas/MainBar");
                if (mainBar == null)
                {
                    // パスで見つからない場合は名前だけで検索を試みる（重い処理なので頻度注意）
                    mainBar = GameObject.Find("MainBar");
                }

                if (mainBar != null)
                {
                    Plugin.Log.Info("PlayerCameraController: PauseMenu MainBar found.");
                    _pauseMenuCanvasGroup = mainBar.GetComponent<CanvasGroup>();
                    if (_pauseMenuCanvasGroup == null)
                    {
                        _pauseMenuCanvasGroup = mainBar.AddComponent<CanvasGroup>();
                    }
                }
            }

            if (_pauseMenuCanvasGroup != null)
            {
                // OSCデータ受信から3秒間は非表示（透明度を0にする）
                float targetAlpha = (Time.time - _lastDataTime < 3f) ? 0f : 1f;
                // スムーズに変化させる
                _pauseMenuCanvasGroup.alpha = Mathf.Lerp(_pauseMenuCanvasGroup.alpha, targetAlpha, Time.deltaTime * 10f);

                // 完全に透明な時はインタラクションも無効化する
                bool isVisible = _pauseMenuCanvasGroup.alpha > 0.05f;
                _pauseMenuCanvasGroup.interactable = isVisible;
                _pauseMenuCanvasGroup.blocksRaycasts = isVisible;
            }
        }

        private void JumpToTime(float targetTime)
        {
            if (_audioTimeSyncController == null || _beatmapCallbacksController == null) return;

            float currentTime = _audioTimeSyncController.songTime;
            float delta = targetTime - currentTime;

            // 1. 曲の再生位置を変更
            _audioTimeSyncController.SeekTo(targetTime);

            // 状態: 巻き戻しまたは大幅な早送り（10秒以上）の場合、完全リセット
            // わずかに前進する場合の連続更新（スムーズな再生／スクラブ）
            bool isContinuousForward = delta >= 0f && delta < 10f;

            if (isContinuousForward)
            {
                // 継続的な前進：ゲームループを手動で更新するだけで、オブジェクトを消滅させない
                if (_beatmapObjectManager != null)
                {
                    _beatmapObjectManager.spawnHidden = false;
                }
                _beatmapCallbacksController.ManualUpdate(targetTime);
            }
            else
            {
                // 後退または大ジャンプ：状態完全リセット
                // 2. 画面をクリアするには、まずアクティブなオブジェクトを消去する
                DespawnActiveObjects();

                // 3. BeatmapCallbacksControllerをリセットし、targetTimeまでのイベントを強制的に再再生する
                // _startFilterTimeを安全な時間枠（例：20秒前）に設定し、極端に古いノート/障害物の生成を回避する
                // 同時に、現在のノートと（フィルター時間を無視する）全ての照明イベントが確実に処理されるようにする。
                float safeFilterTime = Mathf.Max(0f, targetTime - 20f);
                _beatmapCallbacksController.SetField("_startFilterTime", safeFilterTime);
                _beatmapCallbacksController._prevSongTime = float.MinValue;

                var callbacksInTimes = _beatmapCallbacksController._callbacksInTimes;
                if (callbacksInTimes != null)
                {
                    foreach (var item in callbacksInTimes.Values)
                    {
                        item.lastProcessedNode = null;
                    }
                }

                // Noodle Extensionsのサポート
                var noodleManagerType = System.Type.GetType("NoodleExtensions.Managers.NoodleObjectsCallbacksManager, NoodleExtensions");
                if (noodleManagerType != null && _container != null)
                {
                    var noodleManager = _container.TryResolve(noodleManagerType);
                    if (noodleManager != null)
                    {
                        AccessTools.Field(noodleManagerType, "_startFilterTime")?.SetValue(noodleManager, safeFilterTime);
                        AccessTools.Field(noodleManagerType, "_prevSongtime")?.SetValue(noodleManager, float.MinValue);
                        
                        var callbacksInTimeField = AccessTools.Field(noodleManagerType, "_callbacksInTime");
                        var callbacksInTime = callbacksInTimeField?.GetValue(noodleManager) as CallbacksInTime;
                        if (callbacksInTime != null)
                        {
                            callbacksInTime.lastProcessedNode = null;
                        }
                    }
                }

                // 4. 新しい時刻に合わせてオブジェクトを生成し、照明を適用するために手動更新を強制する
                if (_beatmapObjectManager != null)
                {
                    _beatmapObjectManager.spawnHidden = false;
                }
                _beatmapCallbacksController.ManualUpdate(targetTime);
            }
        }

        private void DespawnActiveObjects()
        {
            if (_beatmapObjectManager == null) return;

            // NotePools
            var basicNotePool = _beatmapObjectManager._basicGameNotePoolContainer;
            var burstSliderHeadPool = _beatmapObjectManager._burstSliderHeadGameNotePoolContainer;
            var burstSliderPool = _beatmapObjectManager._burstSliderGameNotePoolContainer;
            var bombNotePool = _beatmapObjectManager._bombNotePoolContainer;
            var obstaclePool = _beatmapObjectManager._obstaclePoolContainer;

            DespawnNotes(basicNotePool);
            DespawnNotes(burstSliderHeadPool);
            DespawnNotes(burstSliderPool);
            DespawnNotes(bombNotePool);

            if (obstaclePool != null)
            {
                var activeObstacles = obstaclePool.activeItems.ToList();
                foreach (var item in activeObstacles)
                {
                    _beatmapObjectManager.Despawn(item);
                }
            }

            // Slider Interactions
            var sliderInteractionManagers = Resources.FindObjectsOfTypeAll<SliderInteractionManager>();
            foreach (var manager in sliderInteractionManagers)
            {
                var activeSliders = manager._activeSliders;
                if (activeSliders != null)
                {
                    var activeSlidersCopy = activeSliders.ToList();
                    foreach (var slider in activeSlidersCopy)
                    {
                        manager.RemoveActiveSlider(slider);
                    }
                }
            }

            // NoteCutSoundEffects
            if (_noteCutSoundEffectManager != null)
            {
                var cutSoundPool = _noteCutSoundEffectManager._noteCutSoundEffectPoolContainer;
                if (cutSoundPool != null)
                {
                    var activeEffects = cutSoundPool.activeItems.ToList();
                    foreach (var item in activeEffects)
                    {
                        item.StopPlayingAndFinish();
                    }
                }
                
                _noteCutSoundEffectManager._prevNoteATime = -1f;
                _noteCutSoundEffectManager._prevNoteBTime = -1f;
            }
        }

        private void DespawnNotes<T>(MemoryPoolContainer<T> container) where T : NoteController
        {
            if (container == null) return;
            var activeItems = container.activeItems.ToList();
            foreach (var item in activeItems)
            {
                _beatmapObjectManager.Despawn(item);
            }
        }

        public void OnGUI()
        {
            if (!_showGui) return;

            // 半透明背景
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            _windowRect = GUI.Window(1002, _windowRect, DrawWindow, "Player OSC Camera");
            GUI.backgroundColor = prevColor;
        }

        private void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // 半透明スタイル
            var prevColor = GUI.contentColor;
            GUI.contentColor = new Color(1f, 1f, 1f, 0.9f);

            GUILayout.BeginVertical();

            if (GUILayout.Button("Disable Player OSC"))
            {
                PluginConfig.Instance.playerOsc = false;
                StopOsc();
                DestroyOscCamera();
                _showGui = false;
                enabled = false;
            }

            GUILayout.EndVertical();
            GUI.contentColor = prevColor;
        }

        public void OnDestroy()
        {
            StopOsc();
            DestroyOscCamera();
        }
    }
}