using BS_CameraMovement.Installers;
using BeatmapEditor3D;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace BS_CameraMovement
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            Log.Info("BS-CameraMovement initialized.");

            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");

            zenjector.Install<BSCameraMovementInstaller, BeatmapLevelEditorInstaller>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
        }
    }
}
