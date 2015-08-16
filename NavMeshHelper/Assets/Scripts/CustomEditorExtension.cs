using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Class that holds Unity Editor custom methods and extension methods.
/// This class are intended to write facilitators and functionallity that the built in tools dont provide.
/// </summary>
public static class CustomEditorExtension
{
    /// <summary>
    /// Creates a LayerMask field in an editor(EditorWindow, Editor).
    /// Unity is missing it, so there is the need to implement this handmade.
    /// Use example:
    /// private LayerMask layerMask = 0; // this has global scope
    /// 
    /// layerMask = CustomEditorExtension.LayerMaskField("Layer Mask: ", layerMask);
    /// </summary>
    /// <param name="label"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    public static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        List<string> layers = new List<string>();
        List<int> layerNumbers = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (layerName != "")
            {
                layers.Add(layerName);
                layerNumbers.Add(i);
            }
        }
        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if (((1 << layerNumbers[i]) & layerMask.value) > 0)
            {
                maskWithoutEmpty |= (1 << i);
            }
        }
        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
        int mask = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) > 0)
            {
                mask |= (1 << layerNumbers[i]);
            }
        }
        layerMask.value = mask;
        return layerMask;
    }
}