/* Copyright (C) 2021 Adrian Babilinski
* You may use, distribute and modify this code under the
* terms of the MIT License
*/

using System;
using MagicLeapSetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeapSetupTool.Editor
{
    public static class MagicLeapSetupAutoRun
    {

    #region DEBUG TEXT

        private const string CHANGING_BUILD_PLATFORM_DEBUG = "Setting Build Platform To Lumin...";
        private const string INSTALLING_LUMIN_SDK_DEBUG = "Installing Magic Leap Plug-in...";
        private const string ENABLING_LUMIN_SDK_DEBUG = "Enabling Magic Leap Plug-in...";
        private const string UPDATING_MANIFEST_DEBUG = "Updating Magic Leap Manifest...";
        private const string IMPORTING_LUMIN_UNITYPACKAGE_DEBUG = "Importing Magic Leap UnityPackage...";
        private const string UPDATING_COLORSPACE_DEBUG = "Changing Color Space to Recommended Setting [Linear]...";
        private const string CHANGING_GRAPHICS_API_DEBUG = "Updating Graphics API To Include [OpenGLCore] (Auto Api = false)...";

    #endregion

    #region TEXT AND LABELS

        internal const string APPLY_ALL_PROMPT_TITLE = "Configure all settings";
        internal const string APPLY_ALL_PROMPT_MESSAGE = "This will update the project to the recommended settings for Magic leap EXCEPT FOR SETTING A DEVELOPMENT CERTIFICATE. Would you like to continue?";
        internal const string APPLY_ALL_PROMPT_OK = "Continue";
        internal const string APPLY_ALL_PROMPT_CANCEL = "Cancel";
        internal const string APPLY_ALL_PROMPT_ALT = "Setup Development Certificate";
        internal const string APPLY_ALL_PROMPT_NOTHING_TO_DO_MESSAGE = "All settings are configured. There is no need to run utility";
        internal const string APPLY_ALL_PROMPT_NOTHING_TO_DO_OK = "Close";
        internal const string APPLY_ALL_PROMPT_MISSING_CERT_MESSAGE = "All settings are configured except the developer certificate. Would you like to set it now?";
        internal const string APPLY_ALL_PROMPT_MISSING_CERT_OK = "Set Certificate";
        internal const string APPLY_ALL_PROMPT_MISSING_CERT_CANCEL = "Cancel";

    #endregion

        private static ApplyAllState _currentApplyAllState = ApplyAllState.Done;

        internal static bool _allAutoStepsComplete => MagicLeapSetup.HasCorrectGraphicConfiguration
                                                   && PlayerSettings.colorSpace == ColorSpace.Linear
                                                   && MagicLeapSetup.HasMagicLeapSdkInstalled
                                                   && MagicLeapSetup.ManifestIsUpdated
                                                   && MagicLeapSetup.HasRootSDKPath
                                                   && MagicLeapSetup.LuminSettingEnabled
                                                   && MagicLeapSetup.HasLuminInstalled
                                                   && MagicLeapSetup.HasCompatibleMagicLeapSdk
                                                   && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin;

        internal static void Stop()
        {
            if (_currentApplyAllState != ApplyAllState.Done)
            {
                EditorApplication.update -= OnEditorApplicationUpdate;
            }

            _currentApplyAllState = ApplyAllState.Done;
        }

        internal static void RunApplyAll()
        {
            if (!_allAutoStepsComplete)
            {
                var dialogComplex = EditorUtility.DisplayDialogComplex(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_MESSAGE,
                                                                       APPLY_ALL_PROMPT_OK, APPLY_ALL_PROMPT_CANCEL, APPLY_ALL_PROMPT_ALT);

                switch (dialogComplex)
                {
                    case 0: //Continue
                        _currentApplyAllState = ApplyAllState.SwitchBuildTarget;
                        break;
                    case 1: //Stop
                        _currentApplyAllState = ApplyAllState.Done;
                        break;
                    case 2: //Go to documentation
                        Help.BrowseURL(MagicLeapSetupWindow.SETUP_ENVIRONMENT_URL);
                        _currentApplyAllState = ApplyAllState.Done;
                        break;
                }

                EditorApplication.update += OnEditorApplicationUpdate;
            }
            else if (!MagicLeapSetup.ValidCertificatePath)
            {
                var dialogComplex = EditorUtility.DisplayDialogComplex(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_MISSING_CERT_MESSAGE,
                                                                       APPLY_ALL_PROMPT_MISSING_CERT_OK, APPLY_ALL_PROMPT_MISSING_CERT_CANCEL, APPLY_ALL_PROMPT_ALT);

                switch (dialogComplex)
                {
                    case 0: //Continue
                        MagicLeapSetup.BrowseForCertificate();
                        break;
                    case 1: //Stop
                        _currentApplyAllState = ApplyAllState.Done;
                        break;
                    case 2: //Go to documentation
                        Help.BrowseURL(MagicLeapSetupWindow.SETUP_ENVIRONMENT_URL);
                        _currentApplyAllState = ApplyAllState.Done;
                        break;
                }
            }
            else if (MagicLeapSetup.ValidCertificatePath)
            {
                EditorUtility.DisplayDialog(APPLY_ALL_PROMPT_TITLE, APPLY_ALL_PROMPT_NOTHING_TO_DO_MESSAGE,
                                            APPLY_ALL_PROMPT_NOTHING_TO_DO_OK);
            }
        }

        private static void OnEditorApplicationUpdate()
        {
            var _loading = AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isCompiling || MagicLeapSetup.IsBusy || EditorApplication.isUpdating;

            if (_currentApplyAllState != ApplyAllState.Done && !_loading)
            {
                ApplyAll();
            }

            if (_currentApplyAllState == ApplyAllState.Done)
            {
                EditorApplication.update -= OnEditorApplicationUpdate;
            }
        }

        private static void ApplyAll()
        {
            switch (_currentApplyAllState)
            {
                case ApplyAllState.SwitchBuildTarget:
                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Lumin)
                    {
                        Debug.Log(CHANGING_BUILD_PLATFORM_DEBUG);
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Lumin, BuildTarget.Lumin);
                    }

                    _currentApplyAllState = ApplyAllState.InstallLumin;
                    break;
                case ApplyAllState.InstallLumin:
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin)
                    {
                        if (!MagicLeapSetup.HasLuminInstalled)
                        {
                            Debug.Log(INSTALLING_LUMIN_SDK_DEBUG);
                            MagicLeapSetup.AddLuminSdkAndRefresh();
                        }

                        _currentApplyAllState = ApplyAllState.EnableXrPackage;
                    }

                    break;
                case ApplyAllState.EnableXrPackage:
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && MagicLeapSetup.HasLuminInstalled)
                    {
                        if (!MagicLeapSetup.LuminSettingEnabled)
                        {
                            Debug.Log(ENABLING_LUMIN_SDK_DEBUG);
                            MagicLeapSetup.EnableLuminXRPluginAndRefresh();
                            UnityProjectSettingsUtility.OpenXrManagementWindow();
                            if (!MagicLeapSetup.CheckingAvailability)
                            {
                                MagicLeapSetup.CheckSDKAvailability();
                            }
                        }

                        _currentApplyAllState = ApplyAllState.UpdateManifest;
                    }

                    break;
                case ApplyAllState.UpdateManifest:
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && MagicLeapSetup.HasLuminInstalled && MagicLeapSetup.LuminSettingEnabled)
                    {
                        if (!MagicLeapSetup.ManifestIsUpdated)
                        {
                            Debug.Log(UPDATING_MANIFEST_DEBUG);
                            MagicLeapSetup.UpdateManifest();
                        }

                        _currentApplyAllState = ApplyAllState.ChangeColorSpace;
                    }

                    break;

                case ApplyAllState.ChangeColorSpace:
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && MagicLeapSetup.HasLuminInstalled && MagicLeapSetup.LuminSettingEnabled && MagicLeapSetup.ManifestIsUpdated)
                    {
                        if (PlayerSettings.colorSpace != ColorSpace.Linear)
                        {
                            Debug.Log(UPDATING_COLORSPACE_DEBUG);
                            PlayerSettings.colorSpace = ColorSpace.Linear;
                        }

                        _currentApplyAllState = ApplyAllState.ImportSdkUnityPackage;
                    }

                    break;

                case ApplyAllState.ImportSdkUnityPackage:
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && MagicLeapSetup.HasLuminInstalled && MagicLeapSetup.LuminSettingEnabled && MagicLeapSetup.ManifestIsUpdated)
                    {
                        if (!MagicLeapSetup.HasMagicLeapSdkInstalled)
                        {
                            if (MagicLeapSetup.HasCompatibleMagicLeapSdk)
                            {
                                if (MagicLeapSetup.GetSdkFromPackageManager)
                                {
                                    MagicLeapSetupWindow.ImportSdkFromUnityPackageManagerPackage();
                                }
                                else
                                {
                                    MagicLeapSetupWindow.ImportSdkFromUnityAssetPackage();
                                }

                                Debug.Log(IMPORTING_LUMIN_UNITYPACKAGE_DEBUG);
                                _currentApplyAllState = ApplyAllState.ChangeGraphicsApi;
                            }
                            else
                            {
                                //TODO: Automate
                                Debug.LogError("Magic Leap SDK Conflict. Cannot resolve automatically.");
                                Stop();
                            }
                        }
                    }

                    break;

                case ApplyAllState.ChangeGraphicsApi:
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && MagicLeapSetup.HasLuminInstalled && MagicLeapSetup.LuminSettingEnabled && MagicLeapSetup.ManifestIsUpdated && MagicLeapSetup.HasMagicLeapSdkInstalled && PlayerSettings.colorSpace == ColorSpace.Linear)
                    {
                        if (!MagicLeapSetup.HasCorrectGraphicConfiguration)
                        {
                            Debug.Log(CHANGING_GRAPHICS_API_DEBUG);
                            MagicLeapSetup.UpdatedGraphicSettings += OnGraphicsSettingsUpdated;
                            MagicLeapSetup.UpdateGraphicsSettings();



                            void OnGraphicsSettingsUpdated(bool resetRequired)
                            {
                                UnityProjectSettingsUtility.UpdateGraphicsApi(resetRequired);

                                MagicLeapSetup.UpdatedGraphicSettings -= OnGraphicsSettingsUpdated;
                            }
                        }

                        _currentApplyAllState = ApplyAllState.Done;
                    }

                    break;
                case ApplyAllState.Done:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    #region ENUMS

        private enum ApplyAllState
        {
            SwitchBuildTarget,
            InstallLumin,
            EnableXrPackage,
            UpdateManifest,
            ChangeColorSpace,
            ImportSdkUnityPackage,
            ChangeGraphicsApi,
            Done
        }

    #endregion

    }
}
