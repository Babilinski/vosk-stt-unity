/* Copyright (C) 2021 Adrian Babilinski
* You may use, distribute and modify this code under the
* terms of the MIT License
*/


using System;

namespace MagicLeapSetupTool.Editor.Utilities
{
  using System.IO;
  using UnityEditor;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;

  public class DefineSymbolsUtility
  {
   
    /// <summary>
    /// Check if the current define symbols contain a definition
    /// </summary>
    public static bool ContainsDefineSymbol(string symbol)
    {
      string definesString =
        PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

      return definesString.Contains(symbol);
    }





    /// <summary>
    /// Remove a define from the scripting define symbols for every build target.
    /// </summary>
    /// <param name="define"></param>
    public static void RemoveDefineSymbol(string define)
    {
      foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
      {
        if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup))
          continue;

        string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

        if (defineSymbols.Contains(define))
        {
          defineSymbols = defineSymbols.Replace(string.Format("{0};", define), "");
          defineSymbols = defineSymbols.Replace(define, "");

          PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
        }
      }
    }

    static bool IsObsolete(BuildTargetGroup group)
    {
      var attrs = typeof(BuildTargetGroup).GetField(group.ToString()).GetCustomAttributes(typeof(ObsoleteAttribute), false);
      return attrs.Length > 0;
    }

    /// <summary>
    /// Add define symbol as soon as Unity gets done compiling.
    /// </summary>
    public static void AddDefineSymbol(string define)
    {
      foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
      {
        if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup))
          continue;

        string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

        if (!defineSymbols.Contains(define))
        {
          if (defineSymbols.Length < 1)
            defineSymbols = define;
          else if (defineSymbols.EndsWith(";"))
            defineSymbols = string.Format("{0}{1}", defineSymbols, define);
          else
            defineSymbols = string.Format("{0};{1}", defineSymbols, define);

          PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
        }
      }
    }



 

    /// <summary>
    /// path to project folder;
    /// </summary>
    public static string AbsolutePath
    {
      get { return "../" + Application.dataPath; }
    }

    /// <summary>
    /// Determines whether the specified file exists relative to the root project
    /// </summary>
    /// <returns></returns>
    public static bool FilePathExists(string path)
    {
      return File.Exists(AbsolutePath + path);
    }
    /// <summary>
    /// Find Wild Card File Path
    /// </summary>
    public static bool FilePathExistsWildCard(string path, string searchPattern)
    {
      return Directory.EnumerateFiles(path, searchPattern).Any();
    }
    /// <summary>
    /// Find Wild Card Directory Path
    /// </summary>
    public static bool DirectoryPathExistsWildCard(string path, string searchPattern)
    {
      return Directory.EnumerateDirectories(path, searchPattern).Any();
    }

    /// <summary>
    /// Checks if a Type exists in the project by name.
    /// </summary>
    /// <param name="contains"> full or partial name</param>
    /// <param name="doesNotContain">filter</param>
    /// <returns></returns>
    public static bool TypeExists(string contains, string doesNotContain = null)
    {
      System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
    
      foreach (var assembly in assemblies)
      {
        System.Type[] types = assembly.GetTypes();

        foreach (var scriptType in types)
        {
          if (scriptType.FullName != null)
          {
            if (!string.IsNullOrEmpty(doesNotContain) && scriptType.FullName.Contains(doesNotContain))
            {
              continue;
            }
           
            if (scriptType.FullName.Contains(contains))
            {
              return true;
            

            }

            //if (scriptType.FullName != null && (scriptType.FullName.Contains(contains)))
            //{
            //    Debug.Log($"script name: {scriptType.FullName}");
            //}
          }

        }
      }

      return false;




    }

    /// <summary>
    /// Checks if an assembly exists in the project by name.
    /// </summary>
    /// <param name="contains"> full or partial name</param>
    /// <param name="doesNotContain">filter</param>
    /// <returns></returns>
    public static bool AssemblyExists(string contains, string doesNotContain = null)
    {
      System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

      foreach (var assembly in assemblies)
      {
        if (!string.IsNullOrEmpty(doesNotContain) && assembly.FullName.Contains(doesNotContain))
        {
          continue;
        }

        if (assembly.FullName.Contains(contains))
        {
          return true;


        }
      }

      return false;




    }

    public static bool ValidFolder(string path)
    {
      return AssetDatabase.IsValidFolder(path);
    }

  
  }
}