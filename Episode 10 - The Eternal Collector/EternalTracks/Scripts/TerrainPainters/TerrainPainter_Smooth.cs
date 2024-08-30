using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainPainter_Smooth : BaseTerrainPainter
{
    [SerializeField] int SmoothingKernelSize = 1;

    [SerializeField] bool AdaptiveKernel = false;
    [SerializeField] int MinAdaptiveKernelSize = 1;
    [SerializeField] int MaxAdaptiveKernelSize = 5;

    public override void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight,
                                 Vector3[] vertices, Vector3[] normals, Color[] vertexColours)
    {
        Vector4[] smoothedColours = new Vector4[vertices.Length];

        // generate the smoothed heights
        for (int row = 0; row < vertsPerSide; row++)
        {
            for (int col = 0; col < vertsPerSide; col++)
            {
                int outputIndex = row * vertsPerSide + col;

                smoothedColours[outputIndex] = Vector4.zero;
                int numSamples = 0;

                int workingKernelSize = SmoothingKernelSize;
                if (AdaptiveKernel)
                {
                    workingKernelSize = Mathf.RoundToInt(Mathf.Lerp(MaxAdaptiveKernelSize,
                                                                    MinAdaptiveKernelSize,
                                                                    vertices[outputIndex].y / maxHeight));
                }

                // sample the data
                for (int rowDelta = -workingKernelSize; rowDelta <= workingKernelSize; ++rowDelta)
                {
                    // invalid point?
                    if ((row + rowDelta) < 0 || (row + rowDelta) >= vertsPerSide)
                        continue;

                    for (int colDelta = -workingKernelSize; colDelta <= workingKernelSize; ++colDelta)
                    {
                        // invalid point?
                        if ((col + colDelta) < 0 || (col + colDelta) >= vertsPerSide)
                            continue;

                        int readIndex = (row + rowDelta) * vertsPerSide + (col + colDelta);
                        smoothedColours[outputIndex].x += vertexColours[readIndex].r;
                        smoothedColours[outputIndex].y += vertexColours[readIndex].g;
                        smoothedColours[outputIndex].z += vertexColours[readIndex].b;
                        smoothedColours[outputIndex].w += vertexColours[readIndex].a;
                        numSamples++;
                    }
                }

                smoothedColours[outputIndex] /= numSamples;
                smoothedColours[outputIndex].w = 1f; // force no transparency
            }
        }

        // apply the smoothing
        for (int row = 0; row < vertsPerSide; row++)
        {
            for (int col = 0; col < vertsPerSide; col++)
            {
                int index = row * vertsPerSide + col;

                vertexColours[index] = Color.Lerp(vertexColours[index], smoothedColours[index], Intensity);
            }
        }
    }
}
