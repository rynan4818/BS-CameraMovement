using BS_CameraMovement.Components;
using Zenject;

namespace BS_CameraMovement.Installers
{
    public class BSCameraMovementInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<CameraMovementController>().AsSingle();
        }
    }
}
