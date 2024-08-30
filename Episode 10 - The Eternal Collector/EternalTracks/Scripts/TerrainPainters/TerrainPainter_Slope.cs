using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SlopePaintingRule
{
    [SerializeField] float MinAngle;
    [SerializeField] float MaxAngle;
    [SerializeField] Gradient Colours;

    float CosMinAngle;
    float CosMaxAngle;

    public void Prepare()
    {
        CosMinAngle = Mathf.Cos(MaxAngle * Mathf.Deg2Rad);
        CosMaxAngle = Mathf.Cos(MinAngle * Mathf.Deg2Rad);
    }

    public bool Paint(Vector3 position, Vector3 normal, ref Vector4 outputColour)
    {
        if (normal.y < CosMinAngle || normal.y > CosMaxAngle)
            return false;

        outputColour = Colours.Evaluate(Mathf.InverseLerp(CosMinAngle, CosMaxAngle, normal.y));
        return true;
    }
}

public class TerrainPainter_Slope : BaseTerrainPainter
{
    [SerializeField] List<SlopePaintingRule> Rules;

    public override void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight, 
                                 Vector3[] vertices, Vector3[] normals, Color[] vertexColours)
    {
        // prepare the rules
        foreach (var rule in Rules)
            rule.Prepare();

        // paint the vertices
        for (int row = 0; row < vertsPerSide; row++)
        {
            for (int col = 0; col < vertsPerSide; col++)
            {
                int index = row * vertsPerSide + col;

                // determine the colour
                Vector4 colour = Vector4.zero;
                Vector4 outputColour = Vector4.zero;
                int numAppliedRules = 0;
                for (int ruleIndex = 0; ruleIndex < Rules.Count; ++ruleIndex)
                {
                    if (Rules[ruleIndex].Paint(vertices[index], normals[index], ref outputColour))
                    {
                        colour += outputColour;
                        ++numAppliedRules;
                    }
                }

                // no rules met - do not colour
                if (numAppliedRules == 0)
                    continue;

                // average the colours and remove opacity
                colour /= numAppliedRules;
                colour.w = 1f;

                vertexColours[index] = Color.Lerp(vertexColours[index], colour, Intensity);
            }
        }
    }
}
