using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Problem", menuName = "Databases/Problem Entry", order = 1)]
public class ProblemDefinition : ScriptableObject
{
    public class Configuration
    {
        public readonly List<int> Values = new List<int>();
        public readonly List<bool> PlusMinusOperators = new List<bool>();
        public readonly string LHSText;
        public readonly int RHSValue;
        public readonly float SolveTime;
        public readonly bool IsTutorial;

        public Configuration(List<int> _values, List<bool> _plusMinusOperators, float _solveTime, ProblemDefinition definition)
        {
            Values = _values;
            PlusMinusOperators = _plusMinusOperators;
            SolveTime = _solveTime;

            LHSText = definition.GetLHSText(Values, PlusMinusOperators);
            RHSValue = definition.GetRHSValue(Values, PlusMinusOperators);

            IsTutorial = false;
        }
    }

    public enum Tier
    {
        Tier1,  //  A + B            = ?
        Tier2,  //  A ± B            = ?
        Tier3,  //  A + B  + C       = ?
        Tier4,  //  A ± B  ± C       = ?
        Tier5,  //  A × B            = ?
        Tier6,  // (A ± B) ×  C      = ?
        Tier7,  // (A × B) ±  C      = ?
        Tier8   // (A ± B) × (C ± D) = ?
    }

    public Tier Complexity;
    public float SolveTimeMultiplier = 1.0f;

    public GameObject ProblemUIPrefab;

    public int GetRHSValue(List<int> values, List<bool> PlusMinusOperators)
    {
        switch(Complexity)
        {
            case Tier.Tier1:
                return values[0] + values[1];
            case Tier.Tier2:
                return values[0] + ((PlusMinusOperators[0] ? 1 : -1) * values[1]);
            case Tier.Tier3:
                return values[0] + values[1] + values[2];
            case Tier.Tier4:
                return values[0] + ((PlusMinusOperators[0] ? 1 : -1) * values[1]) + ((PlusMinusOperators[1] ? 1 : -1) * values[2]);
            case Tier.Tier5:
                return values[0] * values[1];
            case Tier.Tier6:
                return (values[0] + ((PlusMinusOperators[0] ? 1 : -1) * values[1])) * values[2];
            case Tier.Tier7:
                return (values[0] * values[1]) + ((PlusMinusOperators[0] ? 1 : -1) * values[2]);
            case Tier.Tier8:
                return (values[0] + ((PlusMinusOperators[0] ? 1 : -1) * values[1])) *
                       (values[2] + ((PlusMinusOperators[1] ? 1 : -1) * values[3]));
        }

        return int.MaxValue;
    }

    public string GetLHSText(List<int> values, List<bool> PlusMinusOperators)
    {
        switch(Complexity)
        {
            case Tier.Tier1:
                return values[0].ToString() + " + " + values[1].ToString();
            case Tier.Tier2:
                return values[0].ToString() + (PlusMinusOperators[0] ? " + " : " - ") + values[1].ToString();
            case Tier.Tier3:
                return values[0].ToString() + " + " + values[1].ToString() + " + " + values[2].ToString();
            case Tier.Tier4:
                return values[0].ToString() + (PlusMinusOperators[0] ? " + " : " - ") + values[1].ToString() +
                                              (PlusMinusOperators[1] ? " + " : " - ") + values[2].ToString();
            case Tier.Tier5:
                return values[0].ToString() + " × " + values[1].ToString();
            case Tier.Tier6:
                return "(" + values[0].ToString() + (PlusMinusOperators[0] ? " + " : " - ") + values[1].ToString() + ")" + " × " + values[2].ToString();
            case Tier.Tier7:
                return "(" + values[0].ToString() + " × " + values[1].ToString() + ")" + (PlusMinusOperators[0] ? " + " : " - ") + values[2].ToString();
            case Tier.Tier8:
                return "(" + values[0].ToString() + (PlusMinusOperators[0] ? " + " : " - ") + values[1].ToString() + ")" + " × " +
                       "(" + values[2].ToString() + (PlusMinusOperators[1] ? " + " : " - ") + values[3].ToString() + ")";
        }

        return "Unknown problem tier";
    }

    public int NumValues
    {
        get
        {
            if (Complexity == Tier.Tier1 || Complexity == Tier.Tier2 || Complexity == Tier.Tier5)
                return 2;
            else if (Complexity == Tier.Tier3 || Complexity == Tier.Tier4 || Complexity == Tier.Tier6 || Complexity == Tier.Tier7)
                return 3;
            else if (Complexity == Tier.Tier8)
                return 4;

            return -1;
        }
    }

    public int NumPlusMinusOperators
    {
        get
        {
            if (Complexity == Tier.Tier2 || Complexity == Tier.Tier6 || Complexity == Tier.Tier7)
                return 1;
            else if (Complexity == Tier.Tier4 || Complexity == Tier.Tier8)
                return 2;
            else if (Complexity == Tier.Tier1 || Complexity == Tier.Tier3 || Complexity == Tier.Tier5)
                return 0;

            return 0;
        }
    }
}
