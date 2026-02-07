using UnityEngine;
using Zenject;
using System.Text.RegularExpressions;
using System;

namespace BS_CameraMovement.Components
{
    public class CameraControlUI : MonoBehaviour
    {
        private Camera _mainCamera;
        private Rect _windowRect = new Rect(20, 20, 300, 250);
        private bool _showMenu = true;
        
        // Settings
        private bool _qFormat = false;

        void Start()
        {
            // Find Main Camera if not injected
            if (_mainCamera == null)
            {
                var cameraObj = GameObject.Find("Wrapper/MainCamera");
                if (cameraObj != null)
                {
                    _mainCamera = cameraObj.GetComponent<Camera>();
                }
            }
        }

        void OnGUI()
        {
            if (_mainCamera == null) return;

            // Toggle Menu Button
            if (GUI.Button(new Rect(10, 10, 100, 20), _showMenu ? "Hide Camera UI" : "Show Camera UI"))
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
            DrawFloatField("X", _mainCamera.transform.position.x, (v) => { var p = _mainCamera.transform.position; p.x = v; _mainCamera.transform.position = p; });
            DrawFloatField("Y", _mainCamera.transform.position.y, (v) => { var p = _mainCamera.transform.position; p.y = v; _mainCamera.transform.position = p; });
            DrawFloatField("Z", _mainCamera.transform.position.z, (v) => { var p = _mainCamera.transform.position; p.z = v; _mainCamera.transform.position = p; });
            GUILayout.EndHorizontal();

            // Rotation
            GUILayout.Label("Rotation");
            GUILayout.BeginHorizontal();
            DrawFloatField("X", _mainCamera.transform.eulerAngles.x, (v) => { var r = _mainCamera.transform.eulerAngles; r.x = v; _mainCamera.transform.eulerAngles = r; });
            DrawFloatField("Y", _mainCamera.transform.eulerAngles.y, (v) => { var r = _mainCamera.transform.eulerAngles; r.y = v; _mainCamera.transform.eulerAngles = r; });
            DrawFloatField("Z", _mainCamera.transform.eulerAngles.z, (v) => { var r = _mainCamera.transform.eulerAngles; r.z = v; _mainCamera.transform.eulerAngles = r; });
            GUILayout.EndHorizontal();

            // FOV
            GUILayout.BeginHorizontal();
            GUILayout.Label("FOV", GUILayout.Width(40));
            float fov = _mainCamera.fieldOfView;
            string fovStr = fov.ToString("0.##");
            string newFovStr = GUILayout.TextField(fovStr);
            if (newFovStr != fovStr)
            {
                if (float.TryParse(newFovStr, out float newFov))
                {
                    _mainCamera.fieldOfView = newFov;
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
            var position = _mainCamera.transform.position;
            var rotation = _mainCamera.transform.eulerAngles;
            string text;
            if (_qFormat)
            {
                text = $"q_{position.x:0.##}_{position.y:0.##}_{position.z:0.##}_{rotation.x:0.#}_{rotation.y:0.#}_{rotation.z:0.#}_{_mainCamera.fieldOfView:0.#}";
            }
            else
            {
                text = $"{position.x:0.##}\t{position.y:0.##}\t{position.z:0.##}\tFALSE\t{rotation.x:0.#}\t{rotation.y:0.#}\t{rotation.z:0.#}\t{_mainCamera.fieldOfView:0.#}";
            }
            GUIUtility.systemCopyBuffer = text;
        }

        private void PasteCameraData()
        {
            var cp = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(cp)) return;

            var position = _mainCamera.transform.position;
            var rotation = _mainCamera.transform.eulerAngles;
            float fov = _mainCamera.fieldOfView;

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

            _mainCamera.transform.position = new Vector3(px, py, pz);
            _mainCamera.transform.eulerAngles = new Vector3(rx, ry, rz);
            _mainCamera.fieldOfView = fov;
        }
    }
}
