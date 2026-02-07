using BeatmapEditor3D.Views;
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
        private Rect _windowRect = new Rect(20, 20, 300, 250);
        private bool _showMenu = true;
        
        // Settings
        private bool _qFormat = true;

        private BeatmapObjectsInputBinder _inputBinder;

        [Inject]
        private void Constractor(BeatmapObjectsInputBinder beatmapObjectsInputBinder)
        {
            _inputBinder = beatmapObjectsInputBinder;
        }

        void Start()
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
        }

        void OnGUI()
        {
            if (_mainCameraFov == null) return;

            // Prevent input conflict
            bool isInputActive = GUIUtility.keyboardControl != 0;
            if (_inputBinder != null)
            {
                if (isInputActive)
                {
                    _inputBinder.Disable();
                }
                else if (!isInputActive)
                {
                    _inputBinder.Enable();
                }
            }

            // Toggle Menu Button
            if (GUI.Button(new Rect(Screen.width - 130, 10, 120, 20), _showMenu ? "Hide Camera UI" : "Show Camera UI"))
            {
                _showMenu = !_showMenu;
            }

            if (_showMenu)
            {
                _windowRect = GUI.Window(1001, _windowRect, DrawWindow, "Camera Control");
            }
        }

        void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginVertical();

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
            string newFovStr = GUILayout.TextField(fovStr);
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

            _qFormat = GUILayout.Toggle(_qFormat, "q_format");

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
            if (_qFormat)
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
            int i = 0;
            
            // Logic adapted from ChroMapper-CameraMovement
            // Index 0-2: Pos X, Y, Z (Index might start later if text length is weird? ChroMapper logic is a bit checking length)
            
            // ChroMapper logic:
            // if (text.Length > 8 || !(text.Length > 4 && float.TryParse(text[4], ...))) i++;
            // This suggests it tries to detect if there is a header or something? Or maybe compatible with some other format.
            // Let's assume standard format matches the Copy format.
            // Copy q_: x_y_z_rx_ry_rz_fov (7 items)
            // Copy tab: x y z FALSE rx ry rz fov (8 items)

            // Let's try to parse simpler first, or stick to ChroMapper logic exactly.
            
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
                        // LookAt logic not fully implemented here as I don't have AvatarPosition easily.
                        // Assuming just skip for now or handle rotation normally if not true.
                         // But if TRUE, rotation is calculated.
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
