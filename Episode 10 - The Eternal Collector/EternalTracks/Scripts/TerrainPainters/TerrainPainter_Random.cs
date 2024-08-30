using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RandomPaintingConfig
{
    [SerializeField] Gradient Colours;
    [SerializeField] Vector2 NoiseScale;
    [SerializeField] float Threshold = 0f;

    public bool Paint(float y, float x, ref Vector4 outputColour)
    {
        float noise = Mathf.PerlinNoise(x * NoiseScale.x, y * NoiseScale.y);
        if (noise < Threshold)
            return false;

        outputColour = Colours.Evaluate(noise);
        return true;
    }
}

public class TerrainPainter_Random : BaseTerrainPainter
{
    [SerializeField] List<RandomPaintingConfig> Configs;

    public override void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight, 
                                 Vector3[] vertices, Vector3[] normals, Color[] vertexColours)
    {
        // paint the vertices
        for (int row = 0; row < vertsPerSide; row++)
        {
            float rowProgress = gridLocation.x + (float)row / (vertsPerSide - 1);

            for (int col = 0; col < vertsPerSide; col++)
            {
                float colProgress = gridLocation.y + (float)col / (vertsPerSide - 1);
                int index = row * vertsPerSide + col;

                // determine the colour
                Vector4 colour = Vector4.zero;
                Vector4 outputColour = Vector4.zero;
                int numAppliedColours = 0;
                for (int configIndex = 0; configIndex < Configs.Count; ++configIndex)
                {
                    if (Configs[configIndex].Paint(rowProgress, colProgress, ref outputColour))
                    {
                        colour += outputColour;
                        ++numAppliedColours;
                    }
                }

                // no colours applied - do not colour
                if (numAppliedColours == 0)
                    continue;

                // average the colours and remove opacity
                colour /= numAppliedColours;
                colour.w = 1f;

                vertexColours[index] = Color.Lerp(vertexColours[index], colour, Intensity);
            }
        }
    }
}
