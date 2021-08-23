/* Copyright (C) 2021 Adrian Babilinski
* You may use, distribute and modify this code under the
* terms of the MIT License
*/

using System;
using System.IO;
using MagicLeapSetupTool.Editor.Templates;
using MagicLeapSetupTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLeapSetupTool.Editor
{
    /// <summary>
    ///     <para>
    ///         Script responsible for communicating and managing values provided by
    ///         <see cref="MagicLeapLuminPackageUtility" /> as well as changing the project settings for lumin
    ///     </para>
    /// </summary>
    public static class MagicLeapSetup
    {
    #region DEBUG LOGS

        private const string IMPORTING_PACKAGE_TEXT = "importing [{0}]"; // {0} is the path  to the unity package
        private const string CANNOT_FIND_PACKAGE_TEXT = "Could not find Unity Package at path [{0}].\n SDK Path: [{1}]\nSDK Version: [{2}]";
        private const string SDK_NOT_INSTALLED_TEXT = "Cannot preform that action while the SDK is not installed in the project";
        private const string FAILED_TO_EXECUTE_ERROR = "Failed to execute [{0}]";                                                                                     //0 is method/action name
        private const string ENABLE_LUMIN_FINISHED_UNSUCCESSFULLY_WARNING = "Unsuccessful call:[{0}]. action finished, but Lumin XR Settings are still not enabled."; //0 is method/action name
        private const string FAILED_TO_IMPORT_PACKAGE_ERROR = "Failed To Import [{0}.unitypackage] : {1}";                                                            //[0] is package name | [1] is Unity error message
        private const string SET_MAGIC_LEAP_DIR_MESSAGE = "Updated Magic Leap SDK path to [{0}].";                                                                    //[0] folder path

    #endregion

    #region GUI TEXT

        private const string CERTIFICATE_FILE_BROWSER_TITLE = "Locate developer certificate"; //Title text of certificate file path browser
        private const string CERTIFICATE_EXTENSTION = "cert";                                 //extension to look for while browsing
        private const string SDK_FILE_BROWSER_TITLE = "Set external Lumin SDK Folder";        //Title text of SDK path browser

    #endregion

        private const string TEST_FOR_PACKAGE_MANAGER_ML_SCRIPT = "LuminUnity";       // Test this assembly. If it does not exist. The package is not imported. 
        private const string TEST_FOR_ML_SCRIPT = "UnityEngine.XR.MagicLeap.MLInput"; // Test this assembly. If it does not exist. The package is not imported. 
        private const string MAGIC_LEAP_DEFINES_SYMBOL = "MAGICLEAP";
        private const string DEFINES_SYMBOL_SEARCH_TARGET = "UnityEngine.XR.MagicLeap"; //Type to search for to enable MAGICLEAP defines symbol
        private const string MAGIC_LEAP_UNITYPACKAGE = "MAGICLEAP";

        private const string LUMIN_SDK_PATH_KEY = "LuminSDKRoot";       //Editor Pref key to set/get the Lumin SDK
        private const string CERTIFICATE_PATH_KEY = "LuminCertificate"; //Editor Pref key to set/get previously used certificate
        private static string _certificatePath = "";

        private static int _busyCounter; //Add value when task starts and remove it when finished
        public static Action FailedToImportPackage;
        public static Action ImportPackageProcessComplete;
        public static Action ImportPackageProcessCancelled;
        public static Action ImportPackageProcessFailed;
        public static Action<bool> UpdatedGraphicSettings;
        public static bool IsBusy => _busyCounter > 0;

        public static bool HasLuminInstalled
        {
            get
            {
#if MAGICLEAP
                return true;
#else
                return false;
#endif
            }
        }

        public static bool CheckingAvailability { get; private set; }
        public static bool HasCompatibleMagicLeapSdk { get; private set; }
        public static bool HasMagicLeapSdkInstalled { get; private set; }
        public static bool HasCorrectGraphicConfiguration { get; private set; }
        public static bool LuminSettingEnabled { get; private set; }
        public static bool GetSdkFromPackageManager { get; private set; }
        public static bool ValidCertificatePath { get; private set; }
        public static bool ImportMagicLeapPackageFromPackageManager { get; private set; }
        public static bool HasIncompatibleSDKAssetPackage { get; private set; }
        public static int SdkApiLevel { get; private set; }
        public static string PreviousCertificatePath { get; private set; }

        public static string SdkRoot { get; private set; }

        // expensive call that should not be called frequently
        public static bool HasRootSDKPathInEditorPrefs => !string.IsNullOrEmpty(EditorPrefs.GetString(LUMIN_SDK_PATH_KEY, null));
        public static bool HasRootSDKPath { get; private set; }

        public static string CertificatePath
        {
            get
            {
                if (HasLuminInstalled && (string.IsNullOrEmpty(_certificatePath) || !File.Exists(_certificatePath)))
                {
                    _certificatePath = UnityProjectSettingsUtility.Lumin.GetInternalCertificatePath();
                }

                ValidCertificatePath = HasLuminInstalled && !string.IsNullOrEmpty(_certificatePath) && File.Exists(_certificatePath);
                return _certificatePath;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    EditorPrefs.SetString(CERTIFICATE_PATH_KEY, value);
                }

                UnityProjectSettingsUtility.Lumin.SetInternalCertificatePath(value);
                _certificatePath = value;
                ValidCertificatePath = HasLuminInstalled && !string.IsNullOrEmpty(_certificatePath) && File.Exists(_certificatePath);
            }
        }

        public static bool ManifestIsUpdated
        {
            get
            {
#if MAGICLEAP
                if (MagicLeapLuminPackageUtility.MagicLeapManifest == null)
                {
                    return false;
                }

                return MagicLeapLuminPackageUtility.MagicLeapManifest.minimumAPILevel == SdkApiLevel;
#else
                return false;
#endif
            }
        }


        public static void UpdateManifest()
        {
            _busyCounter++;
#if MAGICLEAP
            Debug.Log($"Setting SDK Version To: {SdkApiLevel}");
            RefreshVariables();
            MagicLeapLuminPackageUtility.MagicLeapManifest.minimumAPILevel = SdkApiLevel;
            var serializedObject = new SerializedObject(MagicLeapLuminPackageUtility.MagicLeapManifest);
            var priv_groups = serializedObject.FindProperty("m_PrivilegeGroups");

            for (var i = 0; i < priv_groups.arraySize; i++)
            {
                var group = priv_groups.GetArrayElementAtIndex(i);


                var privs = group.FindPropertyRelative("m_Privileges");
                for (var j = 0; j < privs.arraySize; j++)
                {
                    var priv = privs.GetArrayElementAtIndex(j);
                    var enabled = priv.FindPropertyRelative("m_Enabled");
                    var name = priv.FindPropertyRelative("m_Name").stringValue;
                    if (DefaultPackageTemplate.DEFAULT_PRIVILEGES.Contains(name))
                    {
                        enabled.boolValue = true;
                    }
                }
            }

            Debug.Log("Updated Privileges!");

            serializedObject.ApplyModifiedProperties();


            serializedObject.Update();
#endif
            _busyCounter--;
        }

        public static void RefreshVariables()
        {
            SdkRoot = EditorPrefs.GetString(LUMIN_SDK_PATH_KEY, null);
            HasRootSDKPath = !string.IsNullOrEmpty(SdkRoot) && Directory.Exists(SdkRoot);
            _busyCounter++;
#if MAGICLEAP
            CertificatePath = UnityProjectSettingsUtility.Lumin.GetInternalCertificatePath();
            PreviousCertificatePath = EditorPrefs.GetString(CERTIFICATE_PATH_KEY, "");
            HasCompatibleMagicLeapSdk = MagicLeapLuminPackageUtility.HasCompatibleMagicLeapSdk();
            SdkApiLevel = MagicLeapLuminPackageUtility.GetSdkApiLevel();
            GetSdkFromPackageManager = MagicLeapLuminPackageUtility.UseSdkFromPackageManager();
            LuminSettingEnabled = MagicLeapLuminPackageUtility.IsLuminXREnabled();
            HasMagicLeapSdkInstalled = TypeUtility.FindTypeByPartialName(TEST_FOR_ML_SCRIPT) != null || TypeUtility.AssemblyExists(TEST_FOR_PACKAGE_MANAGER_ML_SCRIPT);
            ValidCertificatePath = !string.IsNullOrEmpty(CertificatePath) && File.Exists(CertificatePath);

            var versionLabel = MagicLeapLuminPackageUtility.GetSdkVersion();
            if (Version.TryParse(versionLabel, out var currentVersion))
            {
                if (currentVersion < new Version(0, 26, 0))
                {
                    ImportMagicLeapPackageFromPackageManager = false;
                }
                else
                {
                    ImportMagicLeapPackageFromPackageManager = true;
                }
            }

            HasIncompatibleSDKAssetPackage = MagicLeapLuminPackageUtility.HasIncompatibleUnityAssetPackage();


#endif

            HasCorrectGraphicConfiguration = CorrectGraphicsConfiguration();
            _busyCounter--;
        }


        public static void SetRootSDK(string path)
        {
            SdkRoot = path;
            EditorPrefs.SetString(LUMIN_SDK_PATH_KEY, path);
            Debug.Log(string.Format(SET_MAGIC_LEAP_DIR_MESSAGE, path));
            HasRootSDKPath = !string.IsNullOrEmpty(SdkRoot) && Directory.Exists(SdkRoot);
        }

        public static string GetCurrentSDKFolderName()
        {
            var currentPath = EditorPrefs.GetString(LUMIN_SDK_PATH_KEY, null);
            if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
            {
                currentPath = FindSDKPath();
            }

            //version folder i.e: v[x].[x].[x]
            if (currentPath.Contains("v"))
            {
                var dirName = new DirectoryInfo(currentPath).Name;
                return dirName;
            }

            return "";
        }

        public static string GetCurrentSDKLocation()
        {
            var currentPath = EditorPrefs.GetString(LUMIN_SDK_PATH_KEY, null);
            if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
            {
                currentPath = DefaultSDKPath();
            }

            //select folder just outside of the version folder i.e: PATH/v[x].[x].[x]
            if (currentPath.Contains("v"))
            {
                return Path.GetFullPath(Path.Combine(currentPath, "../"));
            }

            return currentPath;
        }

        public static string DefaultSDKPath()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(root))
            {
                root = Environment.GetEnvironmentVariable("HOME");
            }

            if (!string.IsNullOrEmpty(root))
            {
                var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk/");
                return sdkRoot;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public static string FindSDKPath()
        {
            var editorSdkPath = EditorPrefs.GetString(LUMIN_SDK_PATH_KEY, null);
            if (string.IsNullOrEmpty(editorSdkPath) || !Directory.Exists(editorSdkPath) /* && File.Exists(Path.Combine(editorSdkPath, MANIFEST_PATH))*/)
            {
                var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");


                if (!string.IsNullOrEmpty(root))
                {
                    var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk/");
                    if (!string.IsNullOrEmpty(sdkRoot))
                    {
                        var getVersionDirectories = Directory.EnumerateDirectories(sdkRoot, "v*");
                        var newestVersion = new Version(0, 0, 0);
                        var newestFolder = "";

                        foreach (var versionDirectory in getVersionDirectories)
                        {
                            var dirName = new DirectoryInfo(versionDirectory).Name;
                            var versionOfFolder = new Version(dirName.Replace("v", ""));
                            var result = versionOfFolder.CompareTo(newestVersion);
                            if (result > 0)
                            {
                                newestVersion = versionOfFolder;
                                newestFolder = versionDirectory;
                            }
                        }

                        if (!string.IsNullOrEmpty(newestFolder))
                        {
                            return editorSdkPath;
                        }
                    }
                }
            }
            else
            {
                return editorSdkPath;
            }

            return null;
        }

        public static void CheckSDKAvailability()
        {
            UpdateDefineSymbols();
            RefreshVariables();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Lumin)
            {
                CheckingAvailability = true;
                _busyCounter++;
                MagicLeapLuminPackageUtility.CheckForLuminSdkPackage(OnCheckForLuminRequestFinished);
            }



            static void OnCheckForLuminRequestFinished(bool success, bool hasLumin)
            {
                if (success && hasLumin)
                {
                    LuminSettingEnabled = MagicLeapLuminPackageUtility.IsLuminXREnabled();
                    CertificatePath = UnityProjectSettingsUtility.Lumin.GetInternalCertificatePath();
                    HasMagicLeapSdkInstalled = TypeUtility.FindTypeByPartialName(TEST_FOR_ML_SCRIPT) != null || TypeUtility.AssemblyExists(TEST_FOR_PACKAGE_MANAGER_ML_SCRIPT);
                }

                _busyCounter--;
                CheckingAvailability = false;
            }
        }

        public static void AddLuminSdkAndRefresh()
        {
            _busyCounter++;

            MagicLeapLuminPackageUtility.AddLuminSdkPackage(OnAddLuminPackageRequestFinished);



            void OnAddLuminPackageRequestFinished(bool success)
            {
                if (success)
                {
                    if (!LuminSettingEnabled)
                    {
                        CheckSDKAvailability();
                    }
                }
                else
                {
                    Debug.LogError(string.Format(FAILED_TO_EXECUTE_ERROR, "Add Lumin Sdk Package"));
                }


                _busyCounter--;
            }
        }

        private static void AddMagicLeapSdkFromPackageManagerAndRefresh()
        {
            _busyCounter++;

            MagicLeapLuminPackageUtility.AddMagicLeapSdkPackage(OnAddMagicLeapPackageRequestFinished);



            void OnAddMagicLeapPackageRequestFinished(bool success)
            {
                if (success)
                {
                    if (!LuminSettingEnabled)
                    {
                        ImportPackageProcessComplete?.Invoke();
                        CheckSDKAvailability();
                    }
                }
                else
                {
                    ImportPackageProcessFailed?.Invoke();
                    Debug.LogError(string.Format(FAILED_TO_EXECUTE_ERROR, "Add Magic Leap Sdk Package"));
                }

                _busyCounter--;
            }
        }

        private static void UpdateDefineSymbols()
        {
            var sdkPath = EditorPrefs.GetString("LuminSDKRoot");


            if (!string.IsNullOrWhiteSpace(sdkPath) && Directory.Exists(sdkPath) && DefineSymbolsUtility.TypeExists(DEFINES_SYMBOL_SEARCH_TARGET))
            {
                if (!DefineSymbolsUtility.ContainsDefineSymbol(MAGIC_LEAP_DEFINES_SYMBOL))
                {
                    DefineSymbolsUtility.AddDefineSymbol(MAGIC_LEAP_DEFINES_SYMBOL);
                }
            }
            else
            {
                if (DefineSymbolsUtility.ContainsDefineSymbol(MAGIC_LEAP_DEFINES_SYMBOL))
                {
                    DefineSymbolsUtility.RemoveDefineSymbol(MAGIC_LEAP_DEFINES_SYMBOL);
                }
            }
        }


        public static void EnableLuminXRPluginAndRefresh()
        {
            _busyCounter++;
            MagicLeapLuminPackageUtility.EnableLuminXRFinished += OnEnableMagicLeapPluginFinished;
            MagicLeapLuminPackageUtility.EnableLuminXRPlugin();
        }

        private static void OnEnableMagicLeapPluginFinished(bool success)
        {
            if (success)
            {
                LuminSettingEnabled = MagicLeapLuminPackageUtility.IsLuminXREnabled();
                if (!LuminSettingEnabled)
                {
                    Debug.LogWarning(string.Format(ENABLE_LUMIN_FINISHED_UNSUCCESSFULLY_WARNING, "Enable Lumin XR action"));
                }
            }
            else
            {
                Debug.LogError(string.Format(FAILED_TO_EXECUTE_ERROR, "Enable Lumin XR Package"));
            }

            _busyCounter--;
            MagicLeapLuminPackageUtility.EnableLuminXRFinished -= OnEnableMagicLeapPluginFinished;
        }


        public static void ImportSdkFromPackageManager()
        {
            // _busyCounter++;
            if (HasLuminInstalled)
            {
                AddMagicLeapSdkFromPackageManagerAndRefresh();
            }
            else
            {
                Debug.LogError(SDK_NOT_INSTALLED_TEXT);
            }
        }

        public static void RemoveMagicLeapPackageManagerSDK(Action finished)
        {
            _busyCounter++;
            MagicLeapLuminPackageUtility.RemoveMagicLeapPackageManagerSDK(() =>
                                                                          {
                                                                              _busyCounter--;
                                                                              finished?.Invoke();
                                                                          });
        }

        public static void ImportOldUnityAssetPackage()
        {
            if (HasLuminInstalled)
            {
                MagicLeapLuminPackageUtility.RemoveMagicLeapPackageManagerSDK(() =>
                                                                              {
                                                                                  _busyCounter++;
                                                                                  var unityPackagePath = MagicLeapLuminPackageUtility.GetUnityPackagePath;
                                                                                  if (File.Exists(unityPackagePath))
                                                                                  {
                                                                                      // "importing [{0}]"
                                                                                      Debug.Log(string.Format(IMPORTING_PACKAGE_TEXT, Path.GetFullPath(unityPackagePath)));
                                                                                      AssetDatabase.importPackageCompleted += ImportPackageCompleted;
                                                                                      AssetDatabase.importPackageCancelled += ImportPackageCancelled;
                                                                                      AssetDatabase.importPackageFailed += ImportPackageFailed;
                                                                                      AssetDatabase.ImportPackage(Path.GetFullPath(unityPackagePath), true);
                                                                                  }
                                                                                  else
                                                                                  {
                                                                                      FailedToImportPackage?.Invoke();
                                                                                      // "Could not find Unity Package at path [{0}].\n SDK Path: [{1}]\nSDK Version: [{2}]"
                                                                                      Debug.LogError(string.Format(CANNOT_FIND_PACKAGE_TEXT, Path.GetFullPath(unityPackagePath), MagicLeapLuminPackageUtility.GetSDKPath(), MagicLeapLuminPackageUtility.GetSdkVersion()));
                                                                                      FailedToImportPackage = null;
                                                                                  }
                                                                              });
            }
            else
            {
                Debug.LogError(SDK_NOT_INSTALLED_TEXT);
            }
        }

        private static void ImportPackageFailed(string packageName, string errorMessage)
        {
            if (packageName.ToUpper().Contains(MAGIC_LEAP_UNITYPACKAGE))
            {
                AssetDatabase.importPackageCompleted -= ImportPackageCompleted;
                AssetDatabase.importPackageCancelled -= ImportPackageCancelled;
                AssetDatabase.importPackageFailed -= ImportPackageFailed;
                Debug.LogError(string.Format(FAILED_TO_IMPORT_PACKAGE_ERROR, packageName, errorMessage));
                ImportPackageProcessFailed?.Invoke();
                ImportPackageProcessCancelled = null;
                ImportPackageProcessComplete = null;
                ImportPackageProcessFailed = null;
                _busyCounter--;
            }
        }

        private static void ImportPackageCancelled(string packageName)
        {
            if (packageName.ToUpper().Contains(MAGIC_LEAP_UNITYPACKAGE))
            {
                AssetDatabase.importPackageCompleted -= ImportPackageCompleted;
                AssetDatabase.importPackageCancelled -= ImportPackageCancelled;
                AssetDatabase.importPackageFailed -= ImportPackageFailed;
                ImportPackageProcessCancelled?.Invoke();
                ImportPackageProcessCancelled = null;
                ImportPackageProcessComplete = null;
                ImportPackageProcessFailed = null;
                _busyCounter--;
            }
        }

        private static void ImportPackageCompleted(string packageName)
        {
            if (packageName.ToUpper().Contains(MAGIC_LEAP_UNITYPACKAGE))
            {
                AssetDatabase.importPackageCompleted -= ImportPackageCompleted;
                AssetDatabase.importPackageCancelled -= ImportPackageCancelled;
                AssetDatabase.importPackageFailed -= ImportPackageFailed;
                ImportPackageProcessComplete?.Invoke();
                ImportPackageProcessCancelled = null;
                ImportPackageProcessComplete = null;
                ImportPackageProcessFailed = null;
                _busyCounter--;
            }
        }

        public static void UpdateGraphicsSettings()
        {
            _busyCounter++;

            var standaloneWindowsResetRequired = UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneWindows, GraphicsDeviceType.OpenGLCore, 0);
            var standaloneWindows64ResetRequired = UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneWindows64, GraphicsDeviceType.OpenGLCore, 0);
            var standaloneOSXResetRequired = UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneOSX, GraphicsDeviceType.OpenGLCore, 0);
            var standaloneLinuxResetRequired = UnityProjectSettingsUtility.SetGraphicsApi(BuildTarget.StandaloneLinux64, GraphicsDeviceType.OpenGLCore, 0);


            UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneWindows, false);
            UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneWindows64, false);
            UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneOSX, false);
            UnityProjectSettingsUtility.SetAutoGraphicsApi(BuildTarget.StandaloneLinux64, false);
            RefreshVariables();

            if (standaloneWindowsResetRequired || standaloneWindows64ResetRequired || standaloneOSXResetRequired || standaloneLinuxResetRequired)
            {
                UpdatedGraphicSettings?.Invoke(true);
            }
            else
            {
                UpdatedGraphicSettings?.Invoke(false);
            }


            _busyCounter--;
        }

        private static bool CorrectGraphicsConfiguration()
        {
            _busyCounter++;

        #region Windows

            var correctSetup = false;
            var hasGraphicsDevice = UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneWindows, GraphicsDeviceType.OpenGLCore, 0);
            correctSetup = hasGraphicsDevice && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneWindows);
            if (!correctSetup)
            {
                _busyCounter--;
                return false;
            }

        #endregion

        #region OSX

            hasGraphicsDevice = UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneOSX, GraphicsDeviceType.OpenGLCore, 0);
            correctSetup = hasGraphicsDevice && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneOSX);
            if (!correctSetup)
            {
                _busyCounter--;
                return false;
            }

        #endregion

        #region Linux

            hasGraphicsDevice = UnityProjectSettingsUtility.HasGraphicsDeviceTypeAtIndex(BuildTarget.StandaloneLinux64, GraphicsDeviceType.OpenGLCore, 0);
            correctSetup = hasGraphicsDevice && !UnityProjectSettingsUtility.GetAutoGraphicsApi(BuildTarget.StandaloneLinux64);
            if (!correctSetup)
            {
                _busyCounter--;
                return false;
            }

        #endregion

            _busyCounter--;
            return correctSetup;
        }

        public static void BrowseForCertificate()
        {
            var startDirectory = PreviousCertificatePath;
            if (!string.IsNullOrEmpty(startDirectory))
            {
                startDirectory = Path.GetDirectoryName(startDirectory);
            }

            var path = EditorUtility.OpenFilePanel(CERTIFICATE_FILE_BROWSER_TITLE, startDirectory, CERTIFICATE_EXTENSTION);
            if (path.Length != 0)
            {
                CertificatePath = path;
            }
        }

        public static void BrowseForSDK()
        {
            var path = EditorUtility.OpenFolderPanel(SDK_FILE_BROWSER_TITLE, GetCurrentSDKLocation(), GetCurrentSDKFolderName());
            if (path.Length != 0)
            {
                SetRootSDK(path);
            }
        }


    }
}
