using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Carriage))]
public class CarriageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Rebuild Carriage Data"))
        {
            Carriage carriage = (Carriage)target;
            carriage.RebuildCarriageData();
        }

        base.OnInspectorGUI();
    }
}
