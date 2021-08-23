using System.Collections.Generic;

namespace MagicLeapSetupTool.Editor.Templates
{
    //TODO: Move to external file (.txt,.json.yaml,...)
    public static class DefaultPackageTemplate
    {
        public static readonly List<string> DEFAULT_PRIVILEGES = new List<string>()
                                                                 {
                                                                     "ControllerPose",
                                                                     "GesturesConfig",
                                                                     "GesturesSubscribe",
                                                                     "HandMesh",
                                                                     "ImuCapture",
                                                                     "Internet",
                                                                     "PcfRead",
                                                                     "WifiStatusRead",
                                                                     "WorldReconstruction",
                                                                     "AddressBookRead",
                                                                     "AddressBookWrite",
                                                                     "LocalAreaNetwork",
                                                                     "ObjectData",
                                                                     "AudioCaptureMic",
                                                                     "CameraCapture",
                                                                     "ComputerVision",
                                                                     "FineLocation"
                                                                 };
    }
}
