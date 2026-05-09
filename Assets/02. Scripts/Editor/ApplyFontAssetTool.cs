using UnityEditor;
using UnityEngine;
using TMPro;

public class ApplyFontAssetTool: EditorWindow
{
    private TMP_FontAsset fontAsset;


    private void OnGUI()
    {
        /*        targetTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Texture", targetTexture, typeof(Texture2D), allowSceneObjects: false)
        */

        fontAsset = EditorGUILayout.ObjectField("asd", fontAsset, typeof(TMP_FontAsset), allowSceneObjects: false) as TMP_FontAsset;
        if (GUILayout.Button("Apply"))
        {
            var fonts = FindSceneObjectsOfType(typeof(TMP_FontAsset));

        }
    }

    [MenuItem("Tool/ApplyFonts")]
    public static void Window()
    {
        GetWindow<ApplyFontAssetTool>();

    }
}
