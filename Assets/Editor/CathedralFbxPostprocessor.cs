using UnityEditor;
using UnityEngine;

public class CathedralFbxPostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.Contains("Cathedral.fbx")) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.isReadable = true;
        importer.generateSecondaryUV = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
    }
}
