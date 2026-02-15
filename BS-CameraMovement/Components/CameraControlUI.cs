using BS_CameraMovement.Configuration;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Zenject;

namespace BS_CameraMovement.Components
{
    public class CameraControlUI : MonoBehaviour
    {
        private Camera _mainCameraFov;
        private Camera _mainCameraTrans;
        private Rect _windowRect;

        private CameraMovementController _cameraMovementController;

        [Inject]
        public void Constractor(CameraMovementController cameraMovementController)
        {
            _cameraMovementController = cameraMovementController;
        }

        public void Start()
        {
            var x = PluginConfig.Instance.menuPosX;
            var y = PluginConfig.Instance.menuPosY;
            if (x > Screen.width - 300) x = Screen.width - 300;
            if (y > Screen.height - 250) y = Screen.height - 250;
            _windowRect = new Rect(x, y, 300, 250);
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
        }

        public void OnGUI()
        {
            if (_mainCameraFov == null) return;
            // Toggle Menu Button
            if (GUI.Button(new Rect(Screen.width - 130, 10, 120, 20), PluginConfig.Instance.showMenu ? "Hide Camera UI" : "Show Camera UI"))
            {
                PluginConfig.Instance.showMenu = !PluginConfig.Instance.showMenu;
            }

            if (PluginConfig.Instance.showMenu)
            {
                _windowRect = GUI.Window(1001, _windowRect, DrawWindow, "Camera Control");
            }
        }

        void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // Save position if it changed
            if (PluginConfig.Instance != null)
            {
                if (PluginConfig.Instance.menuPosX != _windowRect.x || PluginConfig.Instance.menuPosY != _windowRect.y)
                {
                    PluginConfig.Instance.menuPosX = _windowRect.x;
                    PluginConfig.Instance.menuPosY = _windowRect.y;
                }
            }

            GUILayout.BeginVertical();

            if (_cameraMovementController != null)
            {
                _cameraMovementController.IsEnabled = GUILayout.Toggle(_cameraMovementController.IsEnabled, "Enable CameraMovement");
            }

            // Position
            GUILayout.Label("Position");
            GUILayout.BeginHorizontal();
            DrawFloatField("X", _mainCameraTrans.transform.position.x, (v) => { var p = _mainCameraTrans.transform.position; p.x = v; _mainCameraTrans.transform.position = p; });
            DrawFloatField("Y", _mainCameraTrans.transform.position.y, (v) => { var p = _mainCameraTrans.transform.position; p.y = v; _mainCameraTrans.transform.position = p; });
            DrawFloatField("Z", _mainCameraTrans.transform.position.z, (v) => { var p = _mainCameraTrans.transform.position; p.z = v; _mainCameraTrans.transform.position = p; });
            GUILayout.EndHorizontal();

            // Rotation
            GUILayout.Label("Rotation");
            GUILayout.BeginHorizontal();
            DrawFloatField("X", _mainCameraTrans.transform.eulerAngles.x, (v) => { var r = _mainCameraTrans.transform.eulerAngles; r.x = v; _mainCameraTrans.transform.eulerAngles = r; });
            DrawFloatField("Y", _mainCameraTrans.transform.eulerAngles.y, (v) => { var r = _mainCameraTrans.transform.eulerAngles; r.y = v; _mainCameraTrans.transform.eulerAngles = r; });
            DrawFloatField("Z", _mainCameraTrans.transform.eulerAngles.z, (v) => { var r = _mainCameraTrans.transform.eulerAngles; r.z = v; _mainCameraTrans.transform.eulerAngles = r; });
            GUILayout.EndHorizontal();

            // FOV
            GUILayout.BeginHorizontal();
            GUILayout.Label("FOV", GUILayout.Width(40));
            float fov = _mainCameraFov.fieldOfView;
            string fovStr = fov.ToString("0.##");
            string newFovStr = GUILayout.TextField(fovStr, GUILayout.Width(40));
            if (newFovStr != fovStr)
            {
                if (float.TryParse(newFovStr, out float newFov))
                {
                    _mainCameraFov.fieldOfView = newFov;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Copy/Paste
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy"))
            {
                CopyCameraData();
            }
            if (GUILayout.Button("Paste"))
            {
                PasteCameraData();
            }
            GUILayout.EndHorizontal();
            var qFormat = GUILayout.Toggle(PluginConfig.Instance.qFormat, "q_format");
            var playerOsc = GUILayout.Toggle(PluginConfig.Instance.playerOsc, "Player OSC Receiver");
            if (PluginConfig.Instance.qFormat != qFormat)
            {
                PluginConfig.Instance.qFormat = qFormat;
            }
            if (PluginConfig.Instance.playerOsc != playerOsc)
            {
                PluginConfig.Instance.playerOsc = playerOsc;
            }
            GUILayout.EndVertical();
        }

        private void DrawFloatField(string label, float currentValue, Action<float> onIndexChanged)
        {
            GUILayout.Label(label, GUILayout.Width(15));
            string str = currentValue.ToString("0.##");
            string newStr = GUILayout.TextField(str, GUILayout.Width(60));
            if (newStr != str)
            {
                if (float.TryParse(newStr, out float newValue))
                {
                    onIndexChanged(newValue);
                }
            }
        }

        private void CopyCameraData()
        {
            var position = _mainCameraTrans.transform.position;
            var rotation = _mainCameraTrans.transform.eulerAngles;
            string text;
            if (PluginConfig.Instance.qFormat)
            {
                text = $"q_{position.x:0.##}_{position.y:0.##}_{position.z:0.##}_{rotation.x:0.#}_{rotation.y:0.#}_{rotation.z:0.#}_{_mainCameraFov.fieldOfView:0.#}";
            }
            else
            {
                text = $"{position.x:0.##}\t{position.y:0.##}\t{position.z:0.##}\tFALSE\t{rotation.x:0.#}\t{rotation.y:0.#}\t{rotation.z:0.#}\t{_mainCameraFov.fieldOfView:0.#}";
            }
            GUIUtility.systemCopyBuffer = text;
        }

        private void PasteCameraData()
        {
            var cp = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(cp)) return;

            var position = _mainCameraTrans.transform.position;
            var rotation = _mainCameraTrans.transform.eulerAngles;
            float fov = _mainCameraFov.fieldOfView;

            string[] text;
            bool tabFormat = true;

            if (Regex.IsMatch(cp, "^q_"))
            {
                cp = Regex.Replace(cp, "^q_", "");
                text = Regex.Split(cp, "_");
                tabFormat = false;
            }
            else
            {
                text = Regex.Split(cp, "\t");
            }

            float res;
            float px = position.x, py = position.y, pz = position.z;
            float rx = rotation.x, ry = rotation.y, rz = rotation.z;

            // Simplified parsing based on expectation
            int idx = 0;
            
            // Position
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { px = res; idx++; } else idx++;
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { py = res; idx++; } else idx++;
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { pz = res; idx++; } else idx++;

            // Tab format has "FALSE" (or TRUE for lookat?) at index 3
            if (tabFormat)
            {
                if (idx < text.Length)
                {
                    // Check for LookAt (TRUE)
                    if (Regex.IsMatch(text[idx], "true", RegexOptions.IgnoreCase))
                    {
                        // LookAtロジックはAvatarPositionを容易に入手できないため、ここでは完全には実装されていません。
                        // 現時点ではスキップするか、TRUEでない場合は通常通り回転を処理すると仮定します。
                        // ただしTRUEの場合、回転が計算されます。
                    }
                    idx++; 
                }
            }

            // Rotation
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { rx = res; idx++; } else idx++;
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { ry = res; idx++; } else idx++;
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { rz = res; idx++; } else idx++;

            // FOV
            if (idx < text.Length && float.TryParse(text[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out res)) { fov = res; idx++; }

            _mainCameraTrans.transform.position = new Vector3(px, py, pz);
            _mainCameraTrans.transform.eulerAngles = new Vector3(rx, ry, rz);
            _mainCameraFov.fieldOfView = fov;
        }
    }
}
