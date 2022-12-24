using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Object = UnityEngine.Object;

/// <summary>
/// 
/// This script duplicates a folder with internal dependencies through the right-click context menu.
///
/// The folder must be selected either in a one-column
/// layout, or in the right-hand pane if you are using a two-column layout, due to Unity's quirks:
/// https://forum.unity.com/threads/selection-activeobject-doesnt-return-folders-in-unity4.182418/
/// 
/// Forum Quote: " the default view of the Project Hierarchy was changed from one column to two and apparently when you
/// have it in 2 columns the Selection.activeObject does not function properly. So just click on the context menu
/// dropdown icon the upper right hand corner of the of the Project Hierarchy view and select "One Column Layout".
/// It does work in two-column layout, but only if you select the folder in the right panel.
/// Selecting in the folder/hierarchy panel (left side) doesn't."
/// 
/// </summary>
public class DuplicateFolder : MonoBehaviour
{
    [MenuItem("Assets/Duplicate Folder (with internal dependencies)")]
    
    static void Duplicate()
    {
        // Get the selected object in the Project window
        Object selectedFolder = Selection.activeObject;

        // Get the folder path of the selected object
        string selectedFolderPath = AssetDatabase.GetAssetPath(selectedFolder);
        // Debug.Log("Selected folder path: " + selectedFolderPath);

        try
        {
            // Create a new name for the duplicate directory by adding "(Copy)" to the end of the original name
            string duplicateName = selectedFolder.name + " (Copy)";
            // Debug.Log("Duplicate name: " + duplicateName);
        
            // Create the destination path by replacing the original name with the duplicate name
            string destinationPath = selectedFolderPath.Replace(selectedFolder.name, duplicateName);
            // Debug.Log("Destination path: " + destinationPath);

            CopyDirectory(selectedFolderPath, destinationPath);
        
            // Refresh the AssetDatabase to show the new directory
            AssetDatabase.Refresh();
            
        }

        catch (NullReferenceException)
        {
            Debug.LogError("Try duplicating the folder by selecting it in the right-hand pane if " +
                             "using a two-column Project layout, or switch to the one-column Project layout and " +
                             "re-run the script again.");
        }

    }
    
    // Recursively retrieve the GUID of all the files contained in the .meta of the destination folder
    // generate a correspondence table between the original GUID and the new ones from GUID.Generate
    // replace the original GUID with the new ones in all files
    
    static void CopyDirectory(string sourcePath, string destinationPath)
    {
        CopyDirectoryRecursively(sourcePath, destinationPath);
 
        List<string> metaFiles = GetFilesRecursively(destinationPath, (f) => f.EndsWith(".meta"));
        List<(string originalGuid, string newGuid)> guidTable = new List<(string originalGuid, string newGuid)>();
 
        foreach (string metaFile in metaFiles)
        {
            StreamReader file = new StreamReader(metaFile);
            file.ReadLine();
            string guidLine = file.ReadLine();
            file.Close();
            string originalGuid = guidLine.Substring(6, guidLine.Length - 6);
            Debug.Log("Original guid: " + originalGuid);
            string newGuid = GUID.Generate().ToString().Replace("-", "");
            Debug.Log("New guid: " + newGuid);
            guidTable.Add((originalGuid, newGuid));
        }
 
        List<string> allFiles = GetFilesRecursively(destinationPath);
 
        foreach (string fileToModify in allFiles)
        {
            string content = File.ReadAllText(fileToModify);
 
            foreach (var guidPair in guidTable)
            {
                content = content.Replace(guidPair.originalGuid, guidPair.newGuid);
            }

            if (Path.GetExtension(fileToModify) != ".png")
            {
                File.WriteAllText(fileToModify, content);
            }
            
            Debug.Log("fileToModify: " + fileToModify + " content: " + content);
        }
    }
    
    private static void CopyDirectoryRecursively(string sourceDirName, string destDirName)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        Debug.Log("Source Directory Name: " + sourceDirName);
        
        DirectoryInfo[] dirs = dir.GetDirectories();
 
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
            Debug.Log("Destination Directory Name: " + destDirName);
        }
 
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            Debug.Log("tempPath: " + tempPath);
            file.CopyTo(tempPath, false);
            Debug.Log("file: " + file);
        }
 
        foreach (DirectoryInfo subdir in dirs)
        {
            string tempPath = Path.Combine(destDirName, subdir.Name);
            CopyDirectoryRecursively(subdir.FullName, tempPath);
            Debug.Log("Source Directory Name: " + subdir.FullName);
            Debug.Log("Destination Directory Name: " + tempPath);
        }
    }
        
    private static List<string> GetFilesRecursively(string path, Func<string, bool> criteria = null, List<string> files = null)
    {
        if (files == null)
        {
            files = new List<string>();
        }
 
        files.AddRange(Directory.GetFiles(path).Where(f =>criteria == null || criteria(f)));
 
        foreach (string directory in Directory.GetDirectories(path))
        {
            GetFilesRecursively(directory, criteria, files);
            Debug.Log("Get Files Recursively: " + directory + " " + criteria + " " + files);
        }
 
        return files;
    }
}
