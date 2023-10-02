﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class HapticsEffectAsset : PlayableAsset
{
    public HapticEffect Effect;
    public bool OverrideBlendMode = false;
    [ConditionalField(nameof(OverrideBlendMode))] public HapticEffect.EBlendMode BlendingMode;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<HapticsEffectBehaviour>.Create(graph);

        var hapticsEffectBehaviour = playable.GetBehaviour();
        hapticsEffectBehaviour.Effect = Effect;
        hapticsEffectBehaviour.OverrideBlendMode = OverrideBlendMode;
        hapticsEffectBehaviour.BlendingMode = BlendingMode;

        return playable;
    }
}
