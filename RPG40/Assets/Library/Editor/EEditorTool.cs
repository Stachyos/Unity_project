using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Editor
{
    public class EEditorTool
    {
        [MenuItem("Assets/Replace Tool/Replace Font/CustomFont", false, priority = -10)]
        [MenuItem("GameObject/Replace Tool/Replace Font/CustomFont", false, priority = -10)]
        public static void ReplaceTmpFont_ChillBitmap_16px()
        {
            Internal_ReplaceTmpFont("Assets/Addressable/Font/AlibabaPuHuiTi-3-55-Regular SDF.asset");
        }
        
        private static void Internal_ReplaceTmpFont(string path)
        {
            var gameObject = Selection.objects.First() as GameObject;

            if (!gameObject)
            {
                return;
            }

            var tmpText = gameObject.GetComponentsInChildren<TMP_Text>(true);

            if (tmpText != null && tmpText.Length > 0)
            {
                TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fontAsset == null)
                {
                    return;
                }

                foreach (var text in tmpText)
                {
                    Undo.RecordObject(text, "ReplaceTmpFont");
                    text.font = fontAsset;
                    EditorUtility.SetDirty(text);
                }
            }

        }
    }
}