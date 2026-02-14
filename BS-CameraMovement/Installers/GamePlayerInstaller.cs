using BS_CameraMovement.Components;
using Zenject;

namespace BS_CameraMovement.Installers
{
    internal class GamePlayerInstaller : MonoInstaller
    {
        [Inject]
        private GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;

        public override void InstallBindings()
        {
            if (_gameplayCoreSceneSetupData.practiceSettings == null)
            {
                return;
            }

            Container.BindInterfacesAndSelfTo<OscCameraReceiver>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerCameraController>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
        }
    }
}
