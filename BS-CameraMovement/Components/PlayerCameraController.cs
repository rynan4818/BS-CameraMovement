using BS_CameraMovement.Configuration;
using System.Linq;
using UnityEngine;
using Zenject;

namespace BS_CameraMovement.Components
{
    public class PlayerCameraController : MonoBehaviour
    {
        private OscCameraReceiver _receiver;
        private bool _showGui = true;
        private Rect _windowRect = new Rect(10, 10, 220, 60);

        private GameObject _cameraObject;
        private Camera _oscCamera;

        private static readonly string[] _destroyComponents = new string[]
        {
            "AudioListener", "LIV", "MainCamera", "MeshCollider",
            "TrackedPoseDriver", "DepthTextureController"
        };

        [Inject]
        public void Construct(OscCameraReceiver oscCameraReceiver)
        {
            _receiver = oscCameraReceiver;
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
            if (_receiver == null || !_receiver.HasData || _oscCamera == null) return;

            var (pos, rot, fov, _) = _receiver.ReadData();
            _receiver.ClearData();

            _oscCamera.transform.position = pos;
            _oscCamera.transform.rotation = rot;
            if (fov > 0)
            {
                _oscCamera.fieldOfView = fov;
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