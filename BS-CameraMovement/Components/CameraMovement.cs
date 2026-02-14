// MovementScript周りは、すのーさんのCameraPlus(https://github.com/Snow1226/CameraPlus)のソースコードをコピー・修正して使用しています。
// コピー元:https://github.com/Snow1226/CameraPlus/blob/master/CameraPlus/Configuration/MovementScriptJson.cs
// コピー元:https://github.com/Snow1226/CameraPlus/blob/master/CameraPlus/Behaviours/CameraMovement.cs
// CameraPlusの著作権表記・ライセンスは以下の通りです。
// https://github.com/Snow1226/CameraPlus/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace BS_CameraMovement.Components
{
    public class AxizWithFoVElements
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }
        public string FOV { get; set; }
    }

    public class AxisElements
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }
    }
    public class VisibleObject
    {
        public bool? avatar { get; set; }
        public bool? ui { get; set; }
        public bool? wall { get; set; }
        public bool? wallFrame { get; set; }
        public bool? saber { get; set; }
        public bool? notes { get; set; }
        public bool? debris { get; set; }
    }

    [JsonObject("Movements")]
    public class JSONMovement
    {
        [JsonProperty("StartPos")]
        public AxizWithFoVElements startPos { get; set; }
        [JsonProperty("StartRot")]
        public AxisElements startRot { get; set; }
        [JsonProperty("StartHeadOffset")]
        public AxisElements startHeadOffset { get; set; }
        [JsonProperty("EndPos")]
        public AxizWithFoVElements endPos { get; set; }
        [JsonProperty("EndRot")]
        public AxisElements endRot { get; set; }
        [JsonProperty("EndHeadOffset")]
        public AxisElements endHeadOffset { get; set; }
        [JsonProperty("VisibleObject")]
        public VisibleObject visibleObject { get; set; }

        public string TurnToHead { get; set; }
        public string TurnToHeadHorizontal { get; set; }
        public string Duration { get; set; }
        public string Delay { get; set; }
        public string EaseTransition { get; set; }
    }

    public class MovementScriptJson
    {
        public string ActiveInPauseMenu { get; set; }
        public string TurnToHeadUseCameraSetting { get; set; }
        [JsonProperty("Movements")]
        public JSONMovement[] Jsonmovement { get; set; }
    }

    public class CameraMovement
    {
        public bool dataLoaded = false;
        public CameraData data = new CameraData();
        public Vector3 StartPos = Vector3.zero;
        public Vector3 EndPos = Vector3.zero;
        public Vector3 StartRot = Vector3.zero;
        public Vector3 EndRot = Vector3.zero;
        public Vector3 StartHeadOffset = Vector3.zero;
        public Vector3 EndHeadOffset = Vector3.zero;
        public float StartFOV = 0;
        public float EndFOV = 0;
        public bool easeTransition = true;
        public float movePerc;
        public int eventID;
        public float movementStartTime, movementEndTime, movementNextStartTime;
        public bool turnToHead;
        public bool turnToHeadHorizontal;
        public float NowFOV = 60f; // Default FOV
        public DateTime movementsFileTime;
        
        // Configuration placeholders
        public bool turnToHeadGlobal = false;

        public class Movements
        {
            public Vector3 StartPos;
            public Vector3 StartRot;
            public Vector3 StartHeadOffset;
            public float StartFOV;
            public Vector3 EndPos;
            public Vector3 EndRot;
            public Vector3 EndHeadOffset;
            public float EndFOV;
            public float Duration;
            public float Delay;
            public VisibleObject SectionVisibleObject;
            public bool TurnToHead = false;
            public bool TurnToHeadHorizontal = false;
            public bool EaseTransition = true;
        }

        public class CameraData
        {
            public bool ActiveInPauseMenu = true;
            public bool TurnToHeadUseCameraSetting = false;
            public List<Movements> Movements = new List<Movements>();
            public float MaxDurationError = 0;
            public float MinDurationError = 0;
            public float TotalDurationError = 0;
            public bool durationErrorWarning = false;

            public bool LoadFromJson(string jsonString)
            {
                durationErrorWarning = false;
                float duration_sum = 0;
                decimal duration_sumd = 0;
                MaxDurationError = 0;
                MinDurationError = 0;
                TotalDurationError = 0;
                Movements.Clear();
                MovementScriptJson movementScriptJson = null;
                string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                string sepCheck = (sep == "." ? "," : ".");
                try
                {
                    movementScriptJson = JsonConvert.DeserializeObject<MovementScriptJson>(jsonString);
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"CameraMovement:JSON file syntax error. {ex.Message}");
                }
                if (movementScriptJson != null && movementScriptJson.Jsonmovement != null)
                {
                    if (movementScriptJson.ActiveInPauseMenu != null)
                        ActiveInPauseMenu = System.Convert.ToBoolean(movementScriptJson.ActiveInPauseMenu);
                    if (movementScriptJson.TurnToHeadUseCameraSetting != null)
                        TurnToHeadUseCameraSetting = System.Convert.ToBoolean(movementScriptJson.TurnToHeadUseCameraSetting);

                    foreach (JSONMovement jsonmovement in movementScriptJson.Jsonmovement)
                    {
                        Movements newMovement = new Movements();

                        AxizWithFoVElements startPos = jsonmovement.startPos;
                        AxisElements startRot = new AxisElements();
                        AxisElements startHeadOffset = new AxisElements();
                        if (jsonmovement.startRot != null) startRot = jsonmovement.startRot;
                        if (jsonmovement.startHeadOffset != null) startHeadOffset = jsonmovement.startHeadOffset;

                        if (startPos.x != null) newMovement.StartPos = new Vector3(float.Parse(startPos.x.Contains(sepCheck) ? startPos.x.Replace(sepCheck, sep) : startPos.x),
                                                                                    float.Parse(startPos.y.Contains(sepCheck) ? startPos.y.Replace(sepCheck, sep) : startPos.y),
                                                                                    float.Parse(startPos.z.Contains(sepCheck) ? startPos.z.Replace(sepCheck, sep) : startPos.z));
                        if (startRot.x != null) newMovement.StartRot = new Vector3(float.Parse(startRot.x.Contains(sepCheck) ? startRot.x.Replace(sepCheck, sep) : startRot.x),
                                                                                    float.Parse(startRot.y.Contains(sepCheck) ? startRot.y.Replace(sepCheck, sep) : startRot.y),
                                                                                    float.Parse(startRot.z.Contains(sepCheck) ? startRot.z.Replace(sepCheck, sep) : startRot.z));
                        else
                            newMovement.StartRot = Vector3.zero;

                        if (startHeadOffset.x != null) newMovement.StartHeadOffset = new Vector3(float.Parse(startHeadOffset.x.Contains(sepCheck) ? startHeadOffset.x.Replace(sepCheck, sep) : startHeadOffset.x),
                                                                                    float.Parse(startHeadOffset.y.Contains(sepCheck) ? startHeadOffset.y.Replace(sepCheck, sep) : startHeadOffset.y),
                                                                                    float.Parse(startHeadOffset.z.Contains(sepCheck) ? startHeadOffset.z.Replace(sepCheck, sep) : startHeadOffset.z));
                        else
                            newMovement.StartHeadOffset = Vector3.zero;

                        if (startPos.FOV != null)
                            newMovement.StartFOV = float.Parse(startPos.FOV.Contains(sepCheck) ? startPos.FOV.Replace(sepCheck, sep) : startPos.FOV);
                        else
                            newMovement.StartFOV = 0;

                        AxizWithFoVElements endPos = jsonmovement.endPos;
                        AxisElements endRot = new AxisElements();
                        AxisElements endHeadOffset = new AxisElements();
                        if (jsonmovement.endRot != null) endRot = jsonmovement.endRot;
                        if (jsonmovement.endHeadOffset != null) endHeadOffset = jsonmovement.endHeadOffset;

                        if (endPos.x != null) newMovement.EndPos = new Vector3(float.Parse(endPos.x.Contains(sepCheck) ? endPos.x.Replace(sepCheck, sep) : endPos.x),
                                                                                    float.Parse(endPos.y.Contains(sepCheck) ? endPos.y.Replace(sepCheck, sep) : endPos.y),
                                                                                    float.Parse(endPos.z.Contains(sepCheck) ? endPos.z.Replace(sepCheck, sep) : endPos.z));
                        if (endRot.x != null) newMovement.EndRot = new Vector3(float.Parse(endRot.x.Contains(sepCheck) ? endRot.x.Replace(sepCheck, sep) : endRot.x),
                                                                                    float.Parse(endRot.y.Contains(sepCheck) ? endRot.y.Replace(sepCheck, sep) : endRot.y),
                                                                                    float.Parse(endRot.z.Contains(sepCheck) ? endRot.z.Replace(sepCheck, sep) : endRot.z));
                        else
                            newMovement.EndRot = Vector3.zero;
                        if (endHeadOffset.x != null) newMovement.EndHeadOffset = new Vector3(float.Parse(endHeadOffset.x.Contains(sepCheck) ? endHeadOffset.x.Replace(sepCheck, sep) : endHeadOffset.x),
                                                            float.Parse(endHeadOffset.y.Contains(sepCheck) ? endHeadOffset.y.Replace(sepCheck, sep) : endHeadOffset.y),
                                                            float.Parse(endHeadOffset.z.Contains(sepCheck) ? endHeadOffset.z.Replace(sepCheck, sep) : endHeadOffset.z));
                        else
                            newMovement.EndHeadOffset = Vector3.zero;


                        if (endPos.FOV != null)
                            newMovement.EndFOV = float.Parse(endPos.FOV.Contains(sepCheck) ? endPos.FOV.Replace(sepCheck, sep) : endPos.FOV);
                        else
                            newMovement.EndFOV = 0;

                        if (jsonmovement.visibleObject != null) newMovement.SectionVisibleObject = jsonmovement.visibleObject;
                        if (jsonmovement.TurnToHead != null) newMovement.TurnToHead = System.Convert.ToBoolean(jsonmovement.TurnToHead);
                        if (jsonmovement.TurnToHeadHorizontal != null) newMovement.TurnToHeadHorizontal = System.Convert.ToBoolean(jsonmovement.TurnToHeadHorizontal);
                        if (jsonmovement.Delay != null) newMovement.Delay = float.Parse(jsonmovement.Delay.Contains(sepCheck) ? jsonmovement.Delay.Replace(sepCheck, sep) : jsonmovement.Delay);
                        if (jsonmovement.Duration != null)
                        {
                            newMovement.Duration = Mathf.Clamp(float.Parse(jsonmovement.Duration.Contains(sepCheck) ? jsonmovement.Duration.Replace(sepCheck, sep) : jsonmovement.Duration), 0.01f, float.MaxValue);
                            duration_sum += newMovement.Duration;
                            var duration_d = decimal.Parse(jsonmovement.Duration.Contains(sepCheck) ? jsonmovement.Duration.Replace(sepCheck, sep) : jsonmovement.Duration, NumberStyles.Number | NumberStyles.AllowExponent);
                            duration_sumd += duration_d;
                            var duration_error = duration_sum - (float)duration_sumd;
                            if (MaxDurationError < duration_error)
                                MaxDurationError = duration_error;
                            if (MinDurationError > duration_error)
                                MinDurationError = duration_error;
                        }

                        if (jsonmovement.EaseTransition != null)
                            newMovement.EaseTransition = System.Convert.ToBoolean(jsonmovement.EaseTransition);

                        Movements.Add(newMovement);
                    }
                    TotalDurationError = duration_sum - (float)duration_sumd;
                    // Plugin.Log.Error($"Duration total error: {Math.Round(TotalDurationError, 4, MidpointRounding.AwayFromZero) * 1000f}ms");
                    return true;
                }
                return false;
            }
        }

        public void CameraUpdate(float currentSeconds, Camera mainCamera)
        {
            if (!dataLoaded) return;

            // Simplified update loop
            while (movementNextStartTime <= currentSeconds && eventID < data.Movements.Count)
            {
                UpdatePosAndRot();
            }

            if (eventID >= data.Movements.Count && currentSeconds >= movementEndTime)
            {
                // Reached end of script
                return;
            }

            float difference = movementEndTime - movementStartTime;
            float current = currentSeconds - movementStartTime;
            if (difference != 0)
                movePerc = Mathf.Clamp(current / difference, 0, 1);

            Vector3 cameraPos = LerpVector3(StartPos, EndPos, Ease(movePerc));
            Vector3 cameraRot = LerpAngleVector3(StartRot, EndRot, Ease(movePerc));

            if (turnToHead)
            {
                // Simplified turn to head - assuming head is at origin or handled elsewhere for now
                // For now, just ignoring specific turnToHead logic that requires external target
            }
            
            Camera.main.transform.SetPositionAndRotation(cameraPos, Quaternion.Euler(cameraRot));
            
            NowFOV = Mathf.Lerp(StartFOV, EndFOV, Ease(movePerc));
            mainCamera.fieldOfView = NowFOV;
        }

        protected Vector3 LerpVector3(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.Lerp(from.x, to.x, percent), Mathf.Lerp(from.y, to.y, percent), Mathf.Lerp(from.z, to.z, percent));
        }

        protected Vector3 LerpAngleVector3(Vector3 from, Vector3 to, float percent)
        {
            return new Vector3(Mathf.LerpAngle(from.x, to.x, percent), Mathf.LerpAngle(from.y, to.y, percent), Mathf.LerpAngle(from.z, to.z, percent));
        }

        public bool LoadCameraData(string pathFile)
        {
            string path = pathFile;
            if (File.Exists(path))
            {
                var movementTimeStamp = File.GetLastWriteTime(path);
                if (movementTimeStamp == movementsFileTime) return true;
                movementsFileTime = movementTimeStamp;
                dataLoaded = false;
                string jsonText = File.ReadAllText(path);
                if (data.LoadFromJson(jsonText))
                {
                    if (data.Movements.Count == 0)
                    {
                        Plugin.Log.Info("CameraMovement:No movement data!");
                        return false;
                    }
                    eventID = 0;
                    // Reset timings
                    movementNextStartTime = 0; 
                    UpdatePosAndRot();
                    dataLoaded = true;

                    Plugin.Log.Info($"CameraMovement:Found {data.Movements.Count} entries in: {path}");
                    return true;
                }
            }
            dataLoaded = false;
            return false;
        }

        protected void FindShortestDelta(ref Vector3 from, ref Vector3 to)
        {
            if (Mathf.DeltaAngle(from.x, to.x) < 0)
                from.x += 360.0f;
            if (Mathf.DeltaAngle(from.y, to.y) < 0)
                from.y += 360.0f;
            if (Mathf.DeltaAngle(from.z, to.z) < 0)
                from.z += 360.0f;
        }

        protected void UpdatePosAndRot()
        {
            if (eventID >= data.Movements.Count)
                return;

            turnToHead = data.TurnToHeadUseCameraSetting ? turnToHeadGlobal : data.Movements[eventID].TurnToHead;
            turnToHeadHorizontal = data.Movements[eventID].TurnToHeadHorizontal;

            easeTransition = data.Movements[eventID].EaseTransition;

            StartRot = new Vector3(data.Movements[eventID].StartRot.x, data.Movements[eventID].StartRot.y, data.Movements[eventID].StartRot.z);
            StartPos = new Vector3(data.Movements[eventID].StartPos.x, data.Movements[eventID].StartPos.y, data.Movements[eventID].StartPos.z);

            EndRot = new Vector3(data.Movements[eventID].EndRot.x, data.Movements[eventID].EndRot.y, data.Movements[eventID].EndRot.z);
            EndPos = new Vector3(data.Movements[eventID].EndPos.x, data.Movements[eventID].EndPos.y, data.Movements[eventID].EndPos.z);

            StartHeadOffset = new Vector3(data.Movements[eventID].StartHeadOffset.x, data.Movements[eventID].StartHeadOffset.y, data.Movements[eventID].StartHeadOffset.z);
            EndHeadOffset = new Vector3(data.Movements[eventID].EndHeadOffset.x, data.Movements[eventID].EndHeadOffset.y, data.Movements[eventID].EndHeadOffset.z);

            if (data.Movements[eventID].StartFOV != 0)
                StartFOV = data.Movements[eventID].StartFOV;
            else
                StartFOV = NowFOV;
            if (data.Movements[eventID].EndFOV != 0)
                EndFOV = data.Movements[eventID].EndFOV;
            else
                EndFOV = NowFOV;

            FindShortestDelta(ref StartRot, ref EndRot);

            movementStartTime = movementNextStartTime;
            movementEndTime = movementNextStartTime + data.Movements[eventID].Duration;
            movementNextStartTime = movementEndTime + data.Movements[eventID].Delay;

            eventID++;
        }

        protected float Ease(float p)
        {
            if (!easeTransition)
                return p;

            if (p < 0.5f) //Cubic Hopefully
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }
        
        public void MovementPositionReset()
        {
            movementNextStartTime = 0;
            eventID = 0;
            movementStartTime = 0;
            movementEndTime = 0;
            if (dataLoaded && data.Movements.Count > 0)
            {
                UpdatePosAndRot();
            }
        }
    }
}
