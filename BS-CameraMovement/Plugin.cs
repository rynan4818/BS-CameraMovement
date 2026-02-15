using BS_CameraMovement.Installers;
using BeatmapEditor3D;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using HarmonyLib;
using System.Reflection;
using IPALogger = IPA.Logging.Logger;

namespace BS_CameraMovement
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        private const string HARMONY_ID = "com.github.rynan4818.BS-CameraMovement";
        private Harmony _harmony;

        [Init]
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            zenjector.UseLogger(Log);
            Log.Info("BS-CameraMovement initialized.");
            _harmony = new Harmony(HARMONY_ID);
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            zenjector.Install<BSCameraMovementInstaller, BeatmapLevelEditorInstaller>();
            zenjector.Install<GamePlayerInstaller>(Location.StandardPlayer);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
            _harmony.UnpatchSelf();
        }
    }
}
