using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PerlinOctave
{
    public float Amplitude = 1f;
    public float Frequency = 1f;

    public PerlinOctave(float _Amplitude, float _Frequency)
    {
        Amplitude = _Amplitude;
        Frequency = _Frequency;
    }
}

public class HeightModifier_PerlinNoise : BaseHeightModifier
{
    [SerializeField] Vector2 BaseScale = new Vector2(8f, 8f);
    [SerializeField] List<PerlinOctave> Octaves;

    public override void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight, Vector3[] vertices)
    {
        // if there are no octaves then add in a default one
        if (Octaves.Count == 0)
            Octaves.Add(new PerlinOctave(1f, 1f));

        // apply each octave
        for (int octave = 0; octave < Octaves.Count; octave++)
        {
            var currentOctave = Octaves[octave];
            Vector2 currentScale = BaseScale * currentOctave.Frequency;

            for (int row = 0; row < vertsPerSide; row++)
            {
                float rowProgress = (gridLocation.x + (float)row / (vertsPerSide - 1)) * currentScale.x;

                for (int col = 0; col < vertsPerSide; col++)
                {
                    float colProgress = (gridLocation.y + (float)col / (vertsPerSide - 1)) * currentScale.y;
                    int index = row * vertsPerSide + col;

                    float height = maxHeight * Mathf.PerlinNoise(rowProgress, colProgress);

                    vertices[index].y += Intensity * currentOctave.Amplitude * height;
                }
            }
        }
    }
}
