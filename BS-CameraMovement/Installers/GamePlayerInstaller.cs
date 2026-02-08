using BS_CameraMovement.Components;
using Zenject;

namespace BS_CameraMovement.Installers
{
    internal class GamePlayerInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<OscCameraReceiver>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerCameraController>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
        }
    }
}
