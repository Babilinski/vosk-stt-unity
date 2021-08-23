/* Copyright (C) 2021 Adrian Babilinski
* You may use, distribute and modify this code under the
* terms of the MIT License
*/


using System.IO;
using MagicLeapSetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace MagicLeapSetupTool.Editor
{
    [InitializeOnLoad]
    public class MagicLeapSetupWindow : EditorWindow
    {
    #region EDITOR PREFS

        internal const string PREVIOUS_CERTIFICATE_PROMPT_KEY = "PREVIOUS_CERTIFICATE_PROMPT_KEY";
        internal const string MAGIC_LEAP_SETUP_POSTFIX_KEY = "MAGIC_LEAP_SETUP_KEY";

    #endregion

    #region TEXT AND LABELS

        private const string WINDOW_PATH = "Magic Leap/Project Setup Utility";
        private const string WINDOW_TITLE_LABEL = "Magic Leap Project Setup";
        private const string TITLE_LABEL = "MAGIC LEAP";
        private const string SUBTITLE_LABEL = "PROJECT SETUP";
        private const string HELP_BOX_TEXT = "Required settings For Lumin SDK";
        private const string LOADING_TEXT = "   Loading and Importing...";
        private const string CONDITION_MET_LABEL = "Done";
        private const string CONDITION_MET_CHANGE_LABEL = "Change";
        private const string FIX_SETTING_BUTTON_LABEL = "Fix Setting";

        private const string COLOR_SPACE_LABEL = "Set Color Space To Linear";

        private const string BUILD_SETTING_LABEL = "Set build target to Lumin";
        private const string INSTALL_PLUGIN_LABEL = "Install the Lumin XR plug-in";
        private const string INSTALL_PLUGIN_BUTTON_LABEL = "Install Package";

        private const string ENABLE_PLUGIN_SETTINGS_LABEL = "Enable the Lumin XR plug-in";
        private const string ENABLE_PLUGIN_LABEL = "Enable Plugin";
        private const string ENABLE_PLUGIN_FAILED_PLUGIN_NOT_INSTALLED_MESSAGE = "Magic Leap Pug-in is not installed.";

        private const string LOCATE_SDK_FOLDER_LABEL = "Set external Lumin SDK Folder";
        private const string LOCATE_SDK_FOLDER_BUTTON_LABEL = "Locate SDK";


        private const string UPDATE_MANIFEST_LABEL = "Update the manifest file";
        private const string UPDATE_MANIFEST_BUTTON_LABEL = "Update";
        private const string LINKS_TITLE = "Helpful Links:";
        private readonly string SET_CERTIFICATE_PATH_LABEL = "Locate developer certificate";
        private const string SET_CERTIFICATE_PATH_BUTTON_LABEL = "Locate";

        private const string SET_CERTIFICATE_HELP_TEXT = "Get a developer certificate";

        private const string IMPORT_MAGIC_LEAP_SDK = "Import the Magic Leap SDK";
        private const string IMPORT_MAGIC_LEAP_SDK_BUTTON = "Import package";
        private const string FAILED_TO_IMPORT_TITLE = "Failed to import Unity Package.";
        private const string FAILED_TO_IMPORT_MESSAGE = "Failed to find the Magic Leap SDK Package. Please make sure your development enviornment is setup correctly.";
        private const string FAILED_TO_IMPORT_OK = "Try Again";
        private const string FAILED_TO_IMPORT_CANCEL = "Cancel";
        private const string FAILED_TO_IMPORT_ALT = "Setup Developer Environment";
        private const string FAILED_TO_IMPORT_HELP_TEXT = "Setup the developer environment";

        private const string FOUND_PREVIOUS_CERTIFICATE_TITLE = "Found Previously Used Developer Certificate";
        private const string FOUND_PREVIOUS_CERTIFICATE_MESSAGE = "Magic Leap Setup has found a previously used developer certificate. Would you like to use it in this project?";
        private const string FOUND_PREVIOUS_CERTIFICATE_OK = "Yes";
        private const string FOUND_PREVIOUS_CERTIFICATE_CANCEL = "Cancel";
        private const string FOUND_PREVIOUS_CERTIFICATE_ALT = "Browse For Certificate";

        private const string REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_TITLE = "Found Incompatable Magic Leap SDK";
        private const string REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_MESSAGE = "The Magic Leap SDK found in your project does not support the selected Lumin SDK Version. Would you like to remove it?";
        private const string REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_OK = "Remove";
        private const string REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_CANCEL = "Cancel";
        private const string REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_ALT = "Remove And Update";

        private const string SET_CORRECT_GRAPHICS_API_LABEL = "Add OpenGLCore to Graphics API";
        private const string SET_CORRECT_GRAPHICS_BUTTON_LABEL = "Update";
        private const string GETTING_STARTED_HELP_TEXT = "Read the getting started guide";

    #endregion

    #region HELP URLS

        internal const string Get_CERTIFICATE_URL = "https://developer.magicleap.com/en-us/learn/guides/developer-certificates";
        internal const string SETUP_ENVIRONMENT_URL = "https://developer.magicleap.com/en-us/learn/guides/set-up-development-environment#installing-lumin-sdk-packages";
        internal const string GETTING_STARTED_URL = "https://developer.magicleap.com/en-us/learn/guides/get-started-developing-in-unity";

    #endregion

        internal static MagicLeapSetupWindow _setupWindow;
        private static bool _subscribedToUpdate;
        private static bool _loading;
        private static bool _showPreviousCertificatePrompt = true;


        static MagicLeapSetupWindow()
        {
            EditorApplication.update += OnEditorApplicationUpdate;
            _subscribedToUpdate = true;
        }

        private static string AutoShowEditorPrefKey
        {
            get
            {
                var projectKey = UnityProjectSettingsUtility.GetProjectKey();
                var path = Path.GetFullPath(Application.dataPath);
                return $"{MAGIC_LEAP_SETUP_POSTFIX_KEY}_[{projectKey}]-[{path}]";
            }
        }

        private void OnEnable()
        {
            EditorApplication.UnlockReloadAssemblies();
            FullRefresh();


            if (EditorPrefs.GetBool($"{Application.dataPath}-DeletedFoldersReset", false) && EditorPrefs.GetBool($"{Application.dataPath}-Install", false))
            {
                ImportSdkFromUnityPackageManagerPackage();
                EditorPrefs.SetBool($"{Application.dataPath}-DeletedFoldersReset", false);
                EditorPrefs.SetBool($"{Application.dataPath}-Install", false);
            }
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(PREVIOUS_CERTIFICATE_PROMPT_KEY, true);
            EditorPrefs.SetBool(AutoShowEditorPrefKey, !MagicLeapSetup.ValidCertificatePath || !MagicLeapSetupAutoRun._allAutoStepsComplete || !MagicLeapSetup.HasCompatibleMagicLeapSdk);
        }


        public void OnGUI()
        {
            DrawHeader();
            _loading = AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isCompiling || MagicLeapSetup.IsBusy || EditorApplication.isUpdating;

            if (_loading)
            {
                DrawWaitingInfo();
            }
            else
            {
                DrawInfoBox();
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);

            DrawBrowseForSDK();

            GUI.enabled = MagicLeapSetup.HasRootSDKPath && !_loading;
            DrawSwitchPlatform();

            //Makes sure the user changes to the Lumin Build Target before being able to set the other options
            GUI.enabled = MagicLeapSetup.HasRootSDKPath && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && !_loading;

            DrawInstallPlugin();

            //Check for Lumin SDK before allowing user to change sdk settings
            GUI.enabled = MagicLeapSetup.HasRootSDKPath && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin && MagicLeapSetup.HasLuminInstalled && !_loading;

            DrawEnablePlugin();

            //Check that lumin is enabled before being able to import package and change color space
            GUI.enabled = MagicLeapSetup.LuminSettingEnabled && !_loading;

            DrawUpdateManifest();

            DrawSetCertificate();

            DrawSetColorSpace();
            DrawImportMagicLeapPackage();


            DrawUpdateGraphicsApi();

            GUI.backgroundColor = Color.clear;

            GUILayout.EndVertical();
            GUILayout.Space(30);
            DrawHelpLinks();
            DrawFooter();
        }

        private void OnFocus()
        {
            if (!MagicLeapSetup.CheckingAvailability)
            {
                MagicLeapSetup.RefreshVariables();
            }
        }

        private void OnInspectorUpdate()
        {
            if (!_loading && MagicLeapSetup.LuminSettingEnabled && !MagicLeapSetup.ValidCertificatePath && _showPreviousCertificatePrompt && !string.IsNullOrWhiteSpace(MagicLeapSetup.PreviousCertificatePath))
            {
                FoundPreviousCertificateLocationPrompt();
            }
        }

        private static void OnEditorApplicationUpdate()
        {
            Open();
        }

        [MenuItem(WINDOW_PATH)]
        public static void Open()
        {
            var autoShow = EditorPrefs.GetBool(AutoShowEditorPrefKey, true) && _subscribedToUpdate;
            if (!MagicLeapSetup.HasRootSDKPathInEditorPrefs || !MagicLeapSetup.HasLuminInstalled || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Lumin || !MagicLeapSetup.HasCompatibleMagicLeapSdk)
            {
                autoShow = true;
                EditorPrefs.SetBool(AutoShowEditorPrefKey, true);
            }

            if (_subscribedToUpdate)
            {
                EditorApplication.update -= OnEditorApplicationUpdate;
                _subscribedToUpdate = false;
                if (!autoShow)
                {
                    return;
                }
            }


            _showPreviousCertificatePrompt = EditorPrefs.GetBool(PREVIOUS_CERTIFICATE_PROMPT_KEY, true);
            MagicLeapSetupAutoRun.Stop();
            _setupWindow = GetWindow<MagicLeapSetupWindow>(false, WINDOW_TITLE_LABEL);
            _setupWindow.minSize = new Vector2(350, 520);
            _setupWindow.maxSize = new Vector2(350, 580);
            EditorApplication.projectChanged += FullRefresh;
        }


        internal static void EnableLuminPlugin()
        {
            if (!MagicLeapSetup.HasLuminInstalled)
            {
                Debug.LogWarning(ENABLE_PLUGIN_FAILED_PLUGIN_NOT_INSTALLED_MESSAGE);
                return;
            }

            MagicLeapSetup.EnableLuminXRPluginAndRefresh();
            UnityProjectSettingsUtility.OpenXrManagementWindow();
            FullRefresh();
        }

        internal static void UpdateManifest()
        {
            if (!MagicLeapSetup.HasLuminInstalled)
            {
                Debug.LogWarning(ENABLE_PLUGIN_FAILED_PLUGIN_NOT_INSTALLED_MESSAGE);
                return;
            }

            MagicLeapSetup.UpdateManifest();
        }

        internal static void FullRefresh()
        {
            if (!MagicLeapSetup.CheckingAvailability)
            {
                MagicLeapSetup.CheckSDKAvailability();
            }
        }


        internal static void ImportSdkFromUnityAssetPackage()
        {
            MagicLeapSetup.FailedToImportPackage += OnFailedToImport;
            MagicLeapSetup.ImportPackageProcessFailed += OnProcessFailed;
            MagicLeapSetup.ImportPackageProcessCancelled += OnProcessFailed;
            MagicLeapSetup.ImportPackageProcessComplete += OnProcessComplete;

            MagicLeapSetup.ImportOldUnityAssetPackage();



            void OnFailedToImport()
            {
                MagicLeapSetup.FailedToImportPackage -= OnFailedToImport;
                var failedToImportOptions = EditorUtility.DisplayDialogComplex(FAILED_TO_IMPORT_TITLE, FAILED_TO_IMPORT_MESSAGE,
                                                                               FAILED_TO_IMPORT_OK, FAILED_TO_IMPORT_CANCEL, FAILED_TO_IMPORT_ALT);

                switch (failedToImportOptions)
                {
                    case 0: //Try again
                        ImportSdkFromUnityAssetPackage();
                        break;
                    case 1: //Stop
                        MagicLeapSetupAutoRun.Stop();
                        break;
                    case 2: //Go to documentation
                        Help.BrowseURL(SETUP_ENVIRONMENT_URL);
                        break;
                }
            }



            void OnProcessFailed()
            {
                _setupWindow.Focus();
                MagicLeapSetupAutoRun.Stop();
                MagicLeapSetup.ImportPackageProcessFailed -= OnProcessFailed;
                MagicLeapSetup.ImportPackageProcessCancelled -= OnProcessFailed;
            }



            void OnProcessComplete()
            {
                _setupWindow.Focus();
                MagicLeapSetup.ImportPackageProcessComplete -= OnProcessComplete;
            }
        }

        internal static void ImportSdkFromUnityPackageManagerPackage()
        {
            EditorPrefs.SetBool($"{Application.dataPath}-Install", false);
            MagicLeapSetup.ImportPackageProcessComplete += OnProcessComplete;
            MagicLeapSetup.ImportPackageProcessFailed += OnProcessFailed;
            MagicLeapSetup.ImportSdkFromPackageManager();



            void OnProcessFailed()
            {
                _setupWindow?.Focus();
                MagicLeapSetupAutoRun.Stop();
                MagicLeapSetup.ImportPackageProcessFailed -= OnProcessFailed;
            }



            void OnProcessComplete()
            {
                _setupWindow?.Focus();
                MagicLeapSetup.ImportPackageProcessComplete -= OnProcessComplete;
            }
        }

    #region Draw Window Controls

        private void DrawHeader()
        {
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField(TITLE_LABEL, Styles.TitleStyle);
            EditorGUILayout.LabelField(SUBTITLE_LABEL, Styles.TitleStyle);
            GUILayout.EndVertical();
            CustomGuiContent.DrawUILine(Color.grey, 1, 5);
            GUI.backgroundColor = Color.white;
            GUILayout.Space(2);
        }

        private void DrawInfoBox()
        {
            var luminLogo = EditorGUIUtility.IconContent("BuildSettings.Lumin").image as Texture2D;

            GUILayout.Space(5);

            var content = new GUIContent(HELP_BOX_TEXT, luminLogo);
            EditorGUILayout.LabelField(content, Styles.InfoTitleStyle);

            GUILayout.Space(5);
            GUI.backgroundColor = Color.white;
        }

        private void DrawHelpLinks()
        {
            var currentGUIEnabledStatus = GUI.enabled;
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            EditorGUILayout.LabelField(LINKS_TITLE, Styles.HelpTitleStyle);
            CustomGuiContent.DisplayLink(GETTING_STARTED_HELP_TEXT, GETTING_STARTED_URL, 3);
            CustomGuiContent.DisplayLink(SET_CERTIFICATE_HELP_TEXT, Get_CERTIFICATE_URL, 3);
            CustomGuiContent.DisplayLink(FAILED_TO_IMPORT_HELP_TEXT, SETUP_ENVIRONMENT_URL, 3);

            GUILayout.Space(2);
            GUILayout.Space(2);
            GUILayout.EndVertical();
            GUI.enabled = currentGUIEnabledStatus;
        }

        public void DrawWaitingInfo()
        {
            var luminLogo = EditorGUIUtility.IconContent("BuildSettings.Lumin").image as Texture2D;

            GUILayout.Space(5);

            var content = new GUIContent(LOADING_TEXT, luminLogo);
            EditorGUILayout.LabelField(content, Styles.InfoTitleStyle);
            GUI.enabled = false;
            GUILayout.Space(5);
            GUI.backgroundColor = Color.white;
        }

        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            var currentGUIEnabledStatus = GUI.enabled;
            GUI.enabled = !_loading;
            if (MagicLeapSetupAutoRun._allAutoStepsComplete && MagicLeapSetup.ValidCertificatePath)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Close", GUILayout.MinWidth(20)))
                {
                    Close();
                }
            }
            else
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Apply All", GUILayout.MinWidth(20)))
                {
                    MagicLeapSetupAutoRun.RunApplyAll();
                }
            }

            GUI.enabled = currentGUIEnabledStatus;
            GUI.backgroundColor = Color.clear;
        }

    #endregion

    #region Draw GUI Buttons

        private void DrawBrowseForSDK()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(new GUIContent(LOCATE_SDK_FOLDER_LABEL), MagicLeapSetup.HasRootSDKPath, new GUIContent(CONDITION_MET_CHANGE_LABEL, MagicLeapSetup.SdkRoot), new GUIContent(LOCATE_SDK_FOLDER_BUTTON_LABEL), Styles.FixButtonStyle, false))
            {
                MagicLeapSetup.BrowseForSDK();
            }
        }

        private void DrawSwitchPlatform()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(BUILD_SETTING_LABEL, EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin, CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Lumin, BuildTarget.Lumin);
            }
        }

        private void DrawInstallPlugin()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(INSTALL_PLUGIN_LABEL, MagicLeapSetup.HasLuminInstalled, CONDITION_MET_LABEL, INSTALL_PLUGIN_BUTTON_LABEL, Styles.FixButtonStyle))
            {
                MagicLeapSetup.AddLuminSdkAndRefresh();
                Repaint();
            }
        }

        private void DrawEnablePlugin()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(ENABLE_PLUGIN_SETTINGS_LABEL, MagicLeapSetup.LuminSettingEnabled, CONDITION_MET_LABEL, ENABLE_PLUGIN_LABEL, Styles.FixButtonStyle))
            {
                EnableLuminPlugin();
            }
        }

        private void DrawUpdateManifest()
        {
            if (!_loading && CustomGuiContent.CustomButtons.DrawConditionButton(UPDATE_MANIFEST_LABEL, MagicLeapSetup.ManifestIsUpdated, CONDITION_MET_LABEL, UPDATE_MANIFEST_BUTTON_LABEL, Styles.FixButtonStyle))
            {
                UpdateManifest();
                Repaint();
            }
        }

        private void DrawSetCertificate()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_CERTIFICATE_PATH_LABEL, MagicLeapSetup.ValidCertificatePath, new GUIContent(CONDITION_MET_CHANGE_LABEL, MagicLeapSetup.CertificatePath), SET_CERTIFICATE_PATH_BUTTON_LABEL, Styles.FixButtonStyle, SET_CERTIFICATE_HELP_TEXT, Get_CERTIFICATE_URL, false))
            {
                MagicLeapSetup.BrowseForCertificate();
            }
        }

        private void DrawSetColorSpace()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(COLOR_SPACE_LABEL, PlayerSettings.colorSpace == ColorSpace.Linear, CONDITION_MET_LABEL, FIX_SETTING_BUTTON_LABEL, Styles.FixButtonStyle))
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Repaint();
            }
        }

        private void DrawImportMagicLeapPackage()
        {
            if (!MagicLeapSetup.HasCompatibleMagicLeapSdk)
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(IMPORT_MAGIC_LEAP_SDK, MagicLeapSetup.HasCompatibleMagicLeapSdk, "....", "Incompatible", Styles.FixButtonStyle, conditionMissingColor: Color.red))
                {
                    UpgradePrompt();
                    Repaint();
                }
            }
            else
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(IMPORT_MAGIC_LEAP_SDK, MagicLeapSetup.HasMagicLeapSdkInstalled, CONDITION_MET_LABEL, IMPORT_MAGIC_LEAP_SDK_BUTTON, Styles.FixButtonStyle))
                {
                    if (MagicLeapSetup.GetSdkFromPackageManager)
                    {
                        ImportSdkFromUnityPackageManagerPackage();
                    }
                    else
                    {
                        ImportSdkFromUnityAssetPackage();
                    }

                    Repaint();
                }
            }
        }

        private void DrawUpdateGraphicsApi()
        {
            if (CustomGuiContent.CustomButtons.DrawConditionButton(SET_CORRECT_GRAPHICS_API_LABEL, MagicLeapSetup.HasCorrectGraphicConfiguration, CONDITION_MET_LABEL, SET_CORRECT_GRAPHICS_BUTTON_LABEL, Styles.FixButtonStyle))
            {
                MagicLeapSetup.UpdatedGraphicSettings += OnGraphicsSettingsUpdated;
                MagicLeapSetup.UpdateGraphicsSettings();



                void OnGraphicsSettingsUpdated(bool resetRequired)
                {
                    UnityProjectSettingsUtility.UpdateGraphicsApi(resetRequired);
                    MagicLeapSetupAutoRun.Stop();
                    MagicLeapSetup.UpdatedGraphicSettings -= OnGraphicsSettingsUpdated;
                }



                Repaint();
            }
        }

    #endregion

    #region Prompts

        private void UpgradePrompt()
        {
            var usePreviousCertificateOption = EditorUtility.DisplayDialogComplex(REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_TITLE, REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_MESSAGE,
                                                                                  REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_OK, REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_CANCEL, REMOVE_INCOMPATIBLE_MAGIC_LEAP_SDK_ALT);

            switch (usePreviousCertificateOption)
            {
                case 0: //Remove
                    if (MagicLeapSetup.HasIncompatibleSDKAssetPackage)
                    {
                        UnityProjectSettingsUtility.DeleteFolder(Path.Combine(Application.dataPath, "MagicLeap"), null, _setupWindow, $"{Application.dataPath}-DeletedFoldersReset");
                    }
                    else
                    {
                        MagicLeapSetup.RemoveMagicLeapPackageManagerSDK(null);
                    }

                    break;
                case 1: //Cancel
                    break;
                case 2: //Remove and update
                    if (MagicLeapSetup.HasIncompatibleSDKAssetPackage)
                    {
                        EditorPrefs.SetBool($"{Application.dataPath}-Install", true);
                        UnityProjectSettingsUtility.DeleteFolder(Path.Combine(Application.dataPath, "MagicLeap"), ImportSdkFromUnityPackageManagerPackage, _setupWindow, $"{Application.dataPath}-DeletedFoldersReset");
                    }
                    else
                    {
                        MagicLeapSetup.RemoveMagicLeapPackageManagerSDK(ImportSdkFromUnityAssetPackage);
                    }

                    break;
            }
        }

        private void FoundPreviousCertificateLocationPrompt()
        {
            var usePreviousCertificateOption = EditorUtility.DisplayDialogComplex(FOUND_PREVIOUS_CERTIFICATE_TITLE, FOUND_PREVIOUS_CERTIFICATE_MESSAGE,
                                                                                  FOUND_PREVIOUS_CERTIFICATE_OK, FOUND_PREVIOUS_CERTIFICATE_CANCEL, FOUND_PREVIOUS_CERTIFICATE_ALT);

            switch (usePreviousCertificateOption)
            {
                case 0: //Yes
                    MagicLeapSetup.CertificatePath = MagicLeapSetup.PreviousCertificatePath;
                    break;
                case 1: //Cancel
                    EditorPrefs.SetBool(PREVIOUS_CERTIFICATE_PROMPT_KEY, false);
                    _showPreviousCertificatePrompt = false;
                    break;
                case 2: //Browse
                    MagicLeapSetup.BrowseForCertificate();
                    break;
            }
        }

    #endregion
    }
}
