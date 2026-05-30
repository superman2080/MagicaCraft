using UnityEditor;
using UnityEngine;
using TMPro;
using System.Linq;

public class ApplyFontAssetTool: EditorWindow
{
    private TMP_FontAsset fontAsset;


    private void OnGUI()
    {
        /*        targetTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Texture", targetTexture, typeof(Texture2D), allowSceneObjects: false)
        */
        fontAsset = EditorGUILayout.ObjectField("Apply Font", fontAsset, typeof(TMP_FontAsset), allowSceneObjects: false) as TMP_FontAsset;
        if (GUILayout.Button("Apply"))
        {
            var fonts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            fonts.ToList().ForEach(f => f.font = fontAsset);
        }
    }

    [MenuItem("Tool/ApplyFonts")]
    public static void Window()
    {
        GetWindow<ApplyFontAssetTool>();

    }
}
