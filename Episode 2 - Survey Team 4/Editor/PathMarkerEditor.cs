using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Injaia
{
    [CustomEditor(typeof(PathMarker))]
    public class PathMarkerEditor : Editor
    {
        void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.DrawDefaultInspector();

            if (GUILayout.Button("Update SO"))
            {
                PathSO path = ((PathMarker)target).TargetPath;

                Undo.RegisterCompleteObjectUndo(path, "Synchronised path");
                EditorUtility.SetDirty(path);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}