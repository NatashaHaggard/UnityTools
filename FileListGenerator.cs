using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using NestedDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, int>>;

/// <summary>
/// Select a game folder through the Tools menu
/// The script gets a list of assets from the selected folder, and adds them to a dictionary
/// The script then goes through a list of all game assets and gets their guids
/// then compares the guids to the assets' guids in the selected folder, and if they match
/// adds those to a nested dictionary
/// The script then prompts you to save a text file in which it lists all the assets in the
/// selected folder, the asset's guid, and a list of game assets its referenced by
/// </summary>

public class FileListGenerator
{
    // A dictionary in a dictionary: D(selectedAssetGUID,D(searchedAssetGUID,timesReferenced))
    private NestedDictionary _assetsReferencedBy = new NestedDictionary();

    // Add an option to the Tools menu
    [MenuItem("Tools/Generate File List")]

    public static void Main()
    {
        var fileListGenerator = new FileListGenerator();
        fileListGenerator.GenerateFileList();
    }
    
    // Main function - get a folder of assets and pre-build the dictionary
    private void GenerateFileList()
    {
        string[] selectedAssets = SelectAFolder();
        
        PopulateDictionary(selectedAssets);
        
        List<string> allAssets = GetAListOfAllGameAssets();

        // Go through each asset in a list of all game assets and get the guids
        foreach (var asset in allAssets)
        {
            var foundGuids = GetGuids(asset);
            Debug.Log("I found some guids: " + foundGuids.Count);
            
            // Store the guids and the asset they came from in the nested dictionary
            AddFoundGuidsToDictionary(foundGuids, asset);
        }
        PrintOutList();
    }
    private string[] SelectAFolder()
    {
        // Browse to select a folder for which you want to generate a file list
        string selectedFolderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/Bingo", "");
        Debug.Log("Selected folder path: " + selectedFolderPath);
  
        // Returns a list of assets from the selected folder and it's subfolders
        string[] selectedAssets = Directory.GetFiles(selectedFolderPath,"*.*", SearchOption.AllDirectories);
        
        return selectedAssets;
    }
    private void PopulateDictionary(string[] selectedAssets)
    {
        // Go through each file in the selected folder
        foreach (string selectedAsset in selectedAssets)
        {
            // Ignore files if their extension is .meta or .DS_Store
            if (Path.GetExtension(selectedAsset) == ".meta" || Path.GetExtension(selectedAsset) == ".DS_Store")
            {
                continue;
            }

            // Get the file path for the asset
            string pathToSelectedAsset = "Assets" + selectedAsset.Replace
                (Application.dataPath, "").Replace('\\', '/');

            // Get the GUID for the asset
            string selectedAssetGuid = AssetDatabase.AssetPathToGUID(pathToSelectedAsset);

            // Convert GUID back to asset path
            var realAssetPath = AssetDatabase.GUIDToAssetPath(selectedAssetGuid);

            if (string.IsNullOrEmpty(realAssetPath))
            {
                Debug.LogError("No asset found for GUID: " + pathToSelectedAsset);
                continue;
            }

            // Check to see if the assetsReferencedBy dictionary contains the selectedAsset GUID
            // if it doesn't, add it to the dictionary
            if (!_assetsReferencedBy.ContainsKey(selectedAssetGuid))
            {
                _assetsReferencedBy.Add(selectedAssetGuid, new Dictionary<string, int>());
            }
        }
    }
    private void PrintOutList()
    {
        string outputPath = EditorUtility.SaveFilePanel("Save Text File", "", "", "txt");

        using (StreamWriter writer = new StreamWriter(outputPath, true))
            foreach (KeyValuePair<string, Dictionary<string, int>> entry in _assetsReferencedBy)
            {
                // Convert GUID back to asset path
                var guidToAsset = AssetDatabase.GUIDToAssetPath(entry.Key);
            
                // Get a file name with extension for the asset
                var selectedAssetName = Path.GetFileName(guidToAsset);

                writer.WriteLine(selectedAssetName + "   " + "GUID: " + entry.Key + '\n'
                                 + "Path: " + guidToAsset + '\n' 
                                 + "Referenced By: ");
                foreach (KeyValuePair<string, int> entry2 in entry.Value)
                {
                    // Get a file name with extension for the asset
                    var referencedAssetName = Path.GetFileName(entry2.Key);
                    writer.WriteLine('\t' + referencedAssetName);
                }
                writer.WriteLine('\n');
            }
        Debug.Log("File list generated at: " + outputPath);
    }
    private List<string> GetAListOfAllGameAssets()
    {
        // Create a list to hold all of the game's files to search for references in.
        var allPathsToAssetsList = new List<string>();

        // Specify what type of files you want to search for references in and add them to the above list
        // This will search through prefabs, materials, scenes, controllers, vfx graphs, assets
        // ("AddRange" appends these items to the end of the allPathsToAssetsList array)
        var allPrefabs = Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories);
        allPathsToAssetsList.AddRange(allPrefabs);
        var allMaterials = Directory.GetFiles(Application.dataPath, "*.mat", SearchOption.AllDirectories);
        allPathsToAssetsList.AddRange(allMaterials);
        var allScenes = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
        allPathsToAssetsList.AddRange(allScenes);
        var allControllers = Directory.GetFiles(Application.dataPath, "*.controller", SearchOption.AllDirectories);
        allPathsToAssetsList.AddRange(allControllers);
        var allVfxGraphs = Directory.GetFiles(Application.dataPath, "*.vfx", SearchOption.AllDirectories);
        allPathsToAssetsList.AddRange(allVfxGraphs);
        var allAssets = Directory.GetFiles(Application.dataPath, "*.asset", SearchOption.AllDirectories);
        allPathsToAssetsList.AddRange(allAssets);

        return allPathsToAssetsList;
    }
    private List<string> GetGuids(string asset)
    {
        var text = File.ReadAllText(asset);
        var lines = text.Split('\n');
        
        List<string> foundGuids = new List<string>();
            
        foreach (var line in lines)
        {   
            string pattern = "guid:\\s*(\\w{32})";

            if(line.Contains("guid:"))
            {
                MatchCollection matches = Regex.Matches(line, pattern);

                Debug.Log("Matches: ");
                foreach (Match match in matches)
                {
                    string foundGuid = match.Groups[1].ToString();
                    foundGuids.Add(foundGuid);
                }
            }
        }
        return foundGuids;
    }
    
    private void AddFoundGuidsToDictionary(List<string> foundGuids, string searchedAsset)
    {
        foreach (string foundGuid in foundGuids)
        {
            if (_assetsReferencedBy.ContainsKey(foundGuid))
            {
                if (!_assetsReferencedBy[foundGuid].ContainsKey(searchedAsset))
                {
                    _assetsReferencedBy[foundGuid].Add(searchedAsset,0);
                }
                _assetsReferencedBy[foundGuid][searchedAsset]++;
            }
        }
    }
}
