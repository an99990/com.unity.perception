using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Unity.Collections;

using UnityEngine;
using UnityEngine.Profiling;

using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
[Serializable]
[AddRandomizerMenu("Perception/My Light Randomizer")]
public class MyLightRandomizer : Randomizer
{
    int m_FrameInIteration;
    List<MyLightRandomizerTag> m_SwitcherTagsInIteration;
    protected override void OnIterationStart()
    {
        m_FrameInIteration = 0;
        m_SwitcherTagsInIteration = tagManager.Query<MyLightRandomizerTag>().ToList();
    }
 
    protected override void OnUpdate()
    {

        foreach (var tag in m_SwitcherTagsInIteration)
        {
            tag.gameObject.SetActive(false);

        }

        if (m_FrameInIteration < m_SwitcherTagsInIteration.Count)
        {
            m_SwitcherTagsInIteration[m_FrameInIteration].gameObject.SetActive(true);
        }
 
        m_FrameInIteration++;
    }

}
