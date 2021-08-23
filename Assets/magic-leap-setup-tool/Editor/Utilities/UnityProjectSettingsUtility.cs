/* Copyright (C) 2021 Adrian Babilinski
* You may use, distribute and modify this code under the
* terms of the MIT License
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MagicLeapSetupTool.Editor.Utilities
{
    [ExecuteInEditMode]
    public static class UnityProjectSettingsUtility
    {
        private const string DELETE_FOLDER_REQUIRES_RESTART_TITLE = "Deleting folder";
        private const string CHANGE_EDITOR_GRAPHICS_API_TITLE = "Changing editor graphics API";
        private const string DELETE_FOLDER_REQUIRES_RESTART_MESSAGE = "The folder you are trying to delete requires a restart of the Editor. Do you want to save the Scene when restarting?";
        private const string CHANGE_EDITOR_GRAPHICS_API_MESSAGE = "You've changed the active graphics API. This requires a restart of the Editor. Do you want to save the Scene when restarting?";
        private const string CHANGE_EDITOR_GRAPHICS_API_SAVE_MESSAGE = "You've changed the active graphics API. This requires a restart of the Editor.";
        private const string DELETE_FOLDER_REQUIRES_RESTART_SAVE_MESSAGE = "The folder you are trying to delete requires a restart of the Editor";
        private const string CHANGE_EDITOR_GRAPHICS_API_OK = "Restart";
        private const string CHANGE_EDITOR_GRAPHICS_API_OK_SAVE = "Save and Restart";
        private const string CHANGE_EDITOR_GRAPHICS_API_DONT_SAVE_CANCEL = "Discard Changes and Restart";
        private const string CHANGE_EDITOR_GRAPHICS_API_CANCEL = "Not Now";
        private const string DELETE_FOLDER_REQUIRES_RESTART_CANCEL = "Cancel";
        private const string FOLDERS_TO_DELETE_KEY = "../FOLDERS_TO_DELETE";


        private static readonly Dictionary<BuildTarget, bool> _supportedPlatformByBuildTarget = new Dictionary<BuildTarget, bool>(); //memo to avoid requesting the same value multiple times

        static UnityProjectSettingsUtility()
        {
            DeleteFlaggedFoldersAndFile();
        }

        public static string[] GetFoldersMarkedForDeletion
        {
            get
            {
                if (!File.Exists(FolderToDeleteStorageFilePath))
                {
                    return null;
                }

                return Encoding.ASCII.GetString(File.ReadAllBytes(FolderToDeleteStorageFilePath)).Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public static string FolderToDeleteStorageFilePath => Path.GetFullPath(Path.Combine(Application.dataPath, FOLDERS_TO_DELETE_KEY));

  


        public static void AddPathToDeleteList(string path)
        {
            var storage = FolderToDeleteStorageFilePath;
            var storageData = "";
            if (File.Exists(storage))
            {
                storageData = Encoding.ASCII.GetString(File.ReadAllBytes(storage));
            }

            if (string.IsNullOrWhiteSpace(storageData))
            {
                storageData = path;
            }
            else
            {
                storageData += $"\n{path}";
            }

            File.WriteAllBytes(storage, Encoding.ASCII.GetBytes(storageData));
        }

        public static bool IsPlatformSupported(BuildTarget buildTargetToTest)
        {
            if (_supportedPlatformByBuildTarget.TryGetValue(buildTargetToTest, out var supported))
            {
                return supported;
            }

            var moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
            var isPlatformSupportLoaded = moduleManager.GetMethod("IsPlatformSupportLoaded", BindingFlags.Static | BindingFlags.NonPublic);
            var getTargetStringFromBuildTarget = moduleManager.GetMethod("GetTargetStringFromBuildTarget", BindingFlags.Static | BindingFlags.NonPublic);
            var buildTargetSupported = (bool) isPlatformSupportLoaded.Invoke(null, new object[] {(string) getTargetStringFromBuildTarget.Invoke(null, new object[] {buildTargetToTest})});
            _supportedPlatformByBuildTarget.Add(buildTargetToTest, buildTargetSupported);


            return buildTargetSupported;
        }

        public static void OpenProjectSettingsWindow(string providerName)
        {
            var window = EditorWindow.GetWindow(Type.GetType("UnityEditor.SettingsWindow,UnityEditor"));

            window.Show();
            var info = window.GetType().GetMethod("SelectProviderByName", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            info.Invoke(window, new[] {providerName});
        }

        public static void OpenXrManagementWindow()
        {
            OpenProjectSettingsWindow("Project/XR Plug-in Management");
        }

        public static void SetAutoGraphicsApi(BuildTarget buildTarget, bool automatic)
        {
            if (GetAutoGraphicsApi(buildTarget) != automatic)
            {
                var playerSettings = Type.GetType("UnityEditor.PlayerSettings,UnityEditor.dll");


                var methodInfo = playerSettings.GetMethod("SetUseDefaultGraphicsAPIs", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                methodInfo.Invoke(playerSettings, new object[] {buildTarget, automatic});
            }
        }

        public static bool GetAutoGraphicsApi(BuildTarget buildTarget)
        {
            var moduleManager = Type.GetType("UnityEditor.PlayerSettings,UnityEditor.dll");

            var isPlatformSupportLoaded = moduleManager.GetMethod("GetUseDefaultGraphicsAPIs", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var buildTargetSupported = (bool) isPlatformSupportLoaded.Invoke(moduleManager, new object[] {buildTarget});
            return buildTargetSupported;
        }

        public static void ClearSceneDirtiness(Scene scene)
        {
            var moduleManager = TypeUtility.FindTypeByPartialName("UnityEditor.SceneManagement.EditorSceneManager", "+");
            var methodInfo = moduleManager.GetMethod("ClearSceneDirtiness", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            methodInfo.Invoke(null, new object[] {scene});
        }

        public static void RequestCloseAndRelaunchWithCurrentArguments()
        {
            var editorApplicationDll = Type.GetType("UnityEditor.EditorApplication,UnityEditor.dll");
            var requestClose = editorApplicationDll.GetMethod("RequestCloseAndRelaunchWithCurrentArguments", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            requestClose.Invoke(null, null);
        }

        public static bool HasGraphicsDeviceType(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType)
        {
            if (IsPlatformSupported(BuildTarget.StandaloneWindows))
            {
                var graphics = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
                return graphics.Contains(graphicsDeviceType);
            }

            return true;
        }

        public static bool HasGraphicsDeviceTypeAtIndex(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType, int index)
        {
            //if (IsPlatformSupported(BuildTarget.StandaloneWindows))
            //{
            var graphics = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
            return graphics[index] == graphicsDeviceType;
            //}

            //return true;
        }

        public static bool SetGraphicsApi(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType, int index = 9999)
        {
            //if (IsPlatformSupported(buildTarget))
            //{
            var graphicsDeviceTypes = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
            index = Mathf.Clamp(index, 0, graphicsDeviceTypes.Count);
            if (graphicsDeviceTypes.Contains(graphicsDeviceType))
            {
                if (index == graphicsDeviceTypes.Count)
                {
                    //already added and we don't care about the index
                    return false;
                }

                if (graphicsDeviceTypes[index] == graphicsDeviceType)
                {
                    //already setup to the desired index
                    return false;
                }

                graphicsDeviceTypes.Remove(graphicsDeviceType);
            }

            if (index == graphicsDeviceTypes.Count)
            {
                graphicsDeviceTypes.Add(graphicsDeviceType);
            }
            else
            {
                graphicsDeviceTypes.Insert(index, graphicsDeviceType);
            }

            PlayerSettings.SetGraphicsAPIs(buildTarget, graphicsDeviceTypes.ToArray());
            return index == 0 && WillEditorUseFirstGraphicsAPI(buildTarget);
            //}
            //else return false;
        }

        private static bool WillEditorUseFirstGraphicsAPI(BuildTarget targetPlatform)
        {
            return
                Application.platform == RuntimePlatform.WindowsEditor && targetPlatform == BuildTarget.StandaloneWindows || Application.platform == RuntimePlatform.LinuxEditor && targetPlatform == BuildTarget.StandaloneLinux64 || Application.platform == RuntimePlatform.OSXEditor && targetPlatform == BuildTarget.StandaloneOSX;
        }

        public static string GetProjectKey()
        {
            return PlayerSettings.companyName + "." + PlayerSettings.productName;
        }

        /// <summary>
        ///     Return path of file relative to the project root. i.e Assets/PathToFile
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal static string AssetRelativePath(string p)
        {
            p = p.Trim();
            var newPath = p.Replace('\\', '/');
            var folders = newPath.Split('/');
            var returnPath = "";
            var add = false;
            for (var i = 0; i < folders.Length; i++)
            {
                if (folders[i].Contains("Assets"))
                {
                    add = true;
                }

                if (add)
                {
                    if (folders[i].Contains(".dll") || folders[i].Contains(".so") || folders[i].Contains(".lib") || folders[i].Contains(".jar"))
                    {
                        returnPath += folders[i];
                    }
                    else
                    {
                        returnPath += folders[i] + "/";
                    }
                }
            }

            return returnPath;
        }

        internal static bool TryDeleteLibrary(string libraryPath)
        {
            var relativePath = AssetRelativePath(libraryPath);

            if (!string.IsNullOrWhiteSpace(libraryPath) && AssetImporter.GetAtPath(relativePath) is PluginImporter)
            {
                var p = (PluginImporter) AssetImporter.GetAtPath(relativePath);
                if (p == null)
                {
                    return false;
                }


                p.SetCompatibleWithEditor(false);
                p.isPreloaded = false;
                p.SetCompatibleWithAnyPlatform(false);
                EditorUtility.SetDirty(p);
                AssetDatabase.WriteImportSettingsIfDirty(relativePath);
                p.SaveAndReimport();
                AssetDatabase.DeleteAsset(relativePath);


                try
                {
                    File.Delete(libraryPath);
                    return true;
                }
                catch
                {
                    try
                    {
                        Object.DestroyImmediate(p, true);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        internal static void DeleteLibraryInFolder(string folderPath)
        {
            foreach (var file in Directory.EnumerateFiles(folderPath, "*.dll", SearchOption.AllDirectories))
            {
                TryDeleteLibrary(file);
                AssetDatabase.Refresh();
            }

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.lib", SearchOption.AllDirectories))
            {
                TryDeleteLibrary(file);
                AssetDatabase.Refresh();
            }

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.so", SearchOption.AllDirectories))
            {
                TryDeleteLibrary(file);
                AssetDatabase.Refresh();
            }
        }


        public static void UpdateGraphicsApi(bool needsReset)
        {
            if (needsReset)
            {
                // If we have dirty scenes we need to save or discard changes before we restart editor.
                // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
                var dirtyScenes = new List<Scene>();
                for (var i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty)
                    {
                        dirtyScenes.Add(scene);
                    }
                }

                var restart = dirtyScenes.Count != 0 ? ShowSaveAndQuitGraphicsApiPrompt(dirtyScenes) : ShowQuitGraphicsApiPrompt();
                if (restart)
                {
                    RequestCloseAndRelaunchWithCurrentArguments();
                }
            }
        }

        public static bool DeleteFlaggedFoldersAndFile()
        {
            var foldersMarkedForDeletion = GetFoldersMarkedForDeletion;
            if (foldersMarkedForDeletion != null)
            {
                for (var i = 0; i < foldersMarkedForDeletion.Length; i++)
                {
                    if (Directory.Exists(foldersMarkedForDeletion[i]))
                    {
                        DeleteLibraryInFolder(foldersMarkedForDeletion[i]);

                        if (File.Exists($"{foldersMarkedForDeletion[i]}.meta"))
                        {
                            File.Delete($"{foldersMarkedForDeletion[i]}.meta");
                        }

                        Directory.Delete(foldersMarkedForDeletion[i], true);
                    }
                }

                if (File.Exists(FolderToDeleteStorageFilePath))
                {
                    File.Delete(FolderToDeleteStorageFilePath);
                }
            }
            else
            {
                return false;
            }

            AssetDatabase.Refresh();
            return true;
        }

        public static bool DeleteFlaggedFolders()
        {
            var foldersMarkedForDeletion = GetFoldersMarkedForDeletion;

            if (foldersMarkedForDeletion != null)
            {
                for (var i = 0; i < foldersMarkedForDeletion.Length; i++)
                {
                    if (Directory.Exists(foldersMarkedForDeletion[i]))
                    {
                        DeleteLibraryInFolder(foldersMarkedForDeletion[i]);
                        Debug.Log($"Deleting [{foldersMarkedForDeletion[i]}]");

                        var relativePath = $"Assets{foldersMarkedForDeletion[i].Remove(0, Application.dataPath.Length).Replace('\\', '/')}";
                        Directory.Delete(foldersMarkedForDeletion[i], true);
                        Debug.Log($"Delete {foldersMarkedForDeletion[i]}");
                        FileUtil.DeleteFileOrDirectory(relativePath);
                        if (File.Exists($"{foldersMarkedForDeletion[i]}.meta"))
                        {
                            Debug.Log($"Delete {foldersMarkedForDeletion[i]}.meta");
                            File.Delete($"{foldersMarkedForDeletion[i]}.meta");
                        }
                    }
                }


                return true;
            }


            return false;
        }

        /// <summary>
        ///     Deletes folder and restarts the editor if required.
        ///     if the editor is restarted, call DeleteFlaggedFolders() to finish the job
        /// </summary>
        /// <param name="folderPath">absolute path</param>
        /// <param name="window">window to close before closing the editor</param>
        /// <param name="editorPrefsKey">If not null, method will set an EditorPref value to true if relaunch starts</param>
        public static void DeleteFolder(string folderPath, Action finished, EditorWindow window = null, string editorPrefsKey = null)
        {
            AssetDatabase.ReleaseCachedFileHandles();
            EditorApplication.LockReloadAssemblies();
            AssetDatabase.DisallowAutoRefresh();
            folderPath = folderPath.Replace('\\', '/');
            if (!Directory.Exists(folderPath))
            {
                EditorApplication.UnlockReloadAssemblies();
                AssetDatabase.AllowAutoRefresh();
                return;
            }

            foreach (var childDirectory in Directory.EnumerateDirectories(folderPath))
            {
                var relativePath = $"Assets{childDirectory.Remove(0, Application.dataPath.Length).Replace('\\', '/')}";

                FileUtil.DeleteFileOrDirectory(relativePath);
            }

            DeleteLibraryInFolder(folderPath);

            FileUtil.DeleteFileOrDirectory(AssetRelativePath(folderPath));

            File.Delete($"{folderPath}.meta");
            AssetDatabase.Refresh();
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();


            if (Directory.Exists(folderPath))
            {
                // If we have dirty scenes we need to save or discard changes before we restart editor.
                // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
                var dirtyScenes = new List<Scene>();
                for (var i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty)
                    {
                        dirtyScenes.Add(scene);
                    }
                }

                var restart = dirtyScenes.Count != 0 ? ShowSaveAndQuitDeleteFolderPrompt(dirtyScenes) : ShowQuitDeleteFolderPrompt();
                if (restart)
                {
                    if (!string.IsNullOrWhiteSpace(editorPrefsKey))
                    {
                        EditorPrefs.SetBool(editorPrefsKey, true);
                    }

                    if (window != null)
                    {
                        window.Close();
                    }

                    AddPathToDeleteList(Path.GetFullPath(folderPath));

                    EditorApplication.quitting += OnQuitting;



                    void OnQuitting()
                    {
                        Task.Run(() =>
                                 {
                                     UnloadResources();

                                     var didDelete = DeleteMyfile(folderPath);


                                     if (File.Exists($"{folderPath}.meta"))
                                     {
                                         File.Delete($"{folderPath}.meta");
                                     }



                                     void UnloadResources()
                                     {
                                         foreach (var file in Directory.EnumerateFiles(folderPath, "*.dll", SearchOption.AllDirectories))
                                         {
                                             var p = (PluginImporter) AssetImporter.GetAtPath(file);
                                             Resources.UnloadAsset(p);
                                         }

                                         foreach (var file in Directory.EnumerateFiles(folderPath, "*.lib", SearchOption.AllDirectories))
                                         {
                                             var p = (PluginImporter) AssetImporter.GetAtPath(file);
                                             Resources.UnloadAsset(p);
                                         }

                                         foreach (var file in Directory.EnumerateFiles(folderPath, "*.so", SearchOption.AllDirectories))
                                         {
                                             var p = (PluginImporter) AssetImporter.GetAtPath(file);
                                             Resources.UnloadAsset(p);
                                         }
                                     }



                                     bool DeleteMyfile(string path)
                                     {
                                         try
                                         {
                                             if (File.Exists(path))
                                             {
                                                 FileUtil.DeleteFileOrDirectory(AssetRelativePath(path));
                                                 Directory.Delete(path, true);
                                             }

                                             return true;
                                         }
                                         catch
                                         {
                                             return false;
                                         }
                                     }
                                 });
                    }



                    RequestCloseAndRelaunchWithCurrentArguments();
                }
                else
                {
                    Debug.LogError($"Folder not deleted. Please delete manually: [Assets/{folderPath}]");
                }
            }
            else
            {
                finished?.Invoke();
            }

            AssetDatabase.Refresh();
        }

        private static bool ShowQuitDeleteFolderPrompt()
        {
            var dialogComplex = EditorUtility.DisplayDialog(DELETE_FOLDER_REQUIRES_RESTART_TITLE, DELETE_FOLDER_REQUIRES_RESTART_MESSAGE,
                                                            CHANGE_EDITOR_GRAPHICS_API_OK, DELETE_FOLDER_REQUIRES_RESTART_CANCEL);
            return dialogComplex;
        }

        private static bool ShowQuitGraphicsApiPrompt()
        {
            var dialogComplex = EditorUtility.DisplayDialog(CHANGE_EDITOR_GRAPHICS_API_TITLE, CHANGE_EDITOR_GRAPHICS_API_MESSAGE,
                                                            CHANGE_EDITOR_GRAPHICS_API_OK, CHANGE_EDITOR_GRAPHICS_API_CANCEL);
            return dialogComplex;
        }

        private static bool ShowSaveAndQuitDeleteFolderPrompt(List<Scene> dirtyScenes)
        {
            var doRestart = false;
            var dialogComplex = EditorUtility.DisplayDialogComplex(DELETE_FOLDER_REQUIRES_RESTART_TITLE, DELETE_FOLDER_REQUIRES_RESTART_SAVE_MESSAGE,
                                                                   CHANGE_EDITOR_GRAPHICS_API_OK_SAVE, DELETE_FOLDER_REQUIRES_RESTART_CANCEL, CHANGE_EDITOR_GRAPHICS_API_DONT_SAVE_CANCEL);

            switch (dialogComplex)
            {
                case 0: //Save and Restart
                    doRestart = true;
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                        if (saved == false)
                        {
                            doRestart = false;
                        }
                    }

                    break;
                case 1: //Cancel
                    break;
                case 2: //Discard Changes and Restart
                    doRestart = true;
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        ClearSceneDirtiness(dirtyScenes[i]);
                    }

                    break;
            }

            return doRestart;
        }

        private static bool ShowSaveAndQuitGraphicsApiPrompt(List<Scene> dirtyScenes)
        {
            var doRestart = false;
            var dialogComplex = EditorUtility.DisplayDialogComplex(CHANGE_EDITOR_GRAPHICS_API_TITLE, CHANGE_EDITOR_GRAPHICS_API_SAVE_MESSAGE,
                                                                   CHANGE_EDITOR_GRAPHICS_API_OK_SAVE, CHANGE_EDITOR_GRAPHICS_API_CANCEL, CHANGE_EDITOR_GRAPHICS_API_DONT_SAVE_CANCEL);

            switch (dialogComplex)
            {
                case 0: //Save and Restart
                    doRestart = true;
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                        if (saved == false)
                        {
                            doRestart = false;
                        }
                    }

                    break;
                case 1: //Cancel
                    break;
                case 2: //Discard Changes and Restart
                    doRestart = true;
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        ClearSceneDirtiness(dirtyScenes[i]);
                    }

                    break;
            }

            return doRestart;
        }

        public static class Lumin
        {
            public static string GetInternalCertificatePath()
            {
                var foundScript = TypeUtility.FindTypeByPartialName("UnityEditor.PlayerSettings+Lumin");


                if (foundScript != null)
                {
                    var sdkAPILevelProperty = foundScript.GetProperty("certificatePath", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    if (sdkAPILevelProperty != null)
                    {
                        return (string) sdkAPILevelProperty.GetValue(foundScript, null);
                    }

                    Debug.LogError("Could not find Property: [certificatePath] in script [UnityEditor.PlayerSettings+Lumin]");
                }
                else
                {
                    Debug.LogError("Could not find Type: [UnityEditor.PlayerSettings+Lumin]");
                }

                return "NULL";
            }

            public static void SetInternalCertificatePath(string certPath)
            {
                var foundScript = TypeUtility.FindTypeByPartialName("UnityEditor.PlayerSettings+Lumin");


                if (foundScript != null)
                {
                    var sdkAPILevelProperty = foundScript.GetProperty("certificatePath", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    if (sdkAPILevelProperty != null)
                    {
                        sdkAPILevelProperty.SetValue(foundScript, certPath);
                    }
                    else
                    {
                        Debug.LogError("Could not find Property: [certificatePath] in script [UnityEditor.PlayerSettings+Lumin]");
                    }
                }
            }
        }
    }
}
