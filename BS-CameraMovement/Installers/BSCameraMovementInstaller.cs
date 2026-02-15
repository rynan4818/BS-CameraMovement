using BS_CameraMovement.Components;
using Zenject;

namespace BS_CameraMovement.Installers
{
    public class BSCameraMovementInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<CameraMovement>().AsSingle();
            Container.BindInterfacesAndSelfTo<CameraMovementController>().AsSingle();
            Container.BindInterfacesAndSelfTo<OscCameraReceiver>().AsSingle();
            Container.BindInterfacesAndSelfTo<OscReceiverController>().AsSingle();
            Container.BindInterfacesAndSelfTo<CameraControlUI>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
        }
    }
}
