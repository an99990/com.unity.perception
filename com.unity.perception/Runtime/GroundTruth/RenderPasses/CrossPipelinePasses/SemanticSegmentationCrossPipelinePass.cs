﻿using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Custom Pass which renders labeled images where each object labeled with a Labeling component is drawn with the
    /// value specified by the given LabelingConfiguration.
    /// </summary>
    class SemanticSegmentationCrossPipelinePass : GroundTruthCrossPipelinePass
    {
        const string k_ShaderName = "Perception/SemanticSegmentation";
        static readonly int k_LabelingId = Shader.PropertyToID("LabelingId");

        static int s_LastFrameExecuted = -1;

        SemanticSegmentationLabelConfig m_LabelConfig;

        // NOTICE: Serialize the shader so that the shader asset is included in player builds when the SemanticSegmentationPass is used.
        // Currently commented out and shaders moved to Resources folder due to serialization crashes when it is enabled.
        // See https://fogbugz.unity3d.com/f/cases/1187378/
        // [SerializeField]
        Shader m_ClassLabelingShader;
        Material m_OverrideMaterial;
        LayerMask m_LayerMask;

        public SemanticSegmentationCrossPipelinePass(Camera targetCamera, SemanticSegmentationLabelConfig labelConfig,
            LayerMask layerMask) : base(targetCamera)
        {
            m_LabelConfig = labelConfig;
            m_LayerMask = layerMask;
        }

        public override void Setup()
        {
            base.Setup();
            m_ClassLabelingShader = Shader.Find(k_ShaderName);

            //var shaderVariantCollection = new ShaderVariantCollection();

            // if (shaderVariantCollection != null)
            // {
            //     shaderVariantCollection.Add(
            //         new ShaderVariantCollection.ShaderVariant(m_ClassLabelingShader, PassType.ScriptableRenderPipeline));
            // }

            m_OverrideMaterial = new Material(m_ClassLabelingShader);
            //shaderVariantCollection.WarmUp();
        }

        protected override void ExecutePass(
            ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
        {
            if (s_LastFrameExecuted == Time.frameCount)
                return;

            s_LastFrameExecuted = Time.frameCount;
            var renderList = CreateRendererListDesc(camera, cullingResult, "FirstPass", 0, m_OverrideMaterial, m_LayerMask);
            cmd.ClearRenderTarget(true, true, m_LabelConfig.skyColor);
            DrawRendererList(renderContext, cmd, RendererList.Create(renderList));
        }

        public override void SetupMaterialProperties(
            MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            var entry = new SemanticSegmentationLabelEntry();
            var found = false;

            foreach (var l in m_LabelConfig.labelEntries)
            {
                if (labeling.labels.Contains(l.label))
                {
                    entry = l;
                    found = true;
                    break;
                }
            }

            // Set the labeling ID so that it can be accessed in ClassSemanticSegmentationPass.shader
            mpb.SetVector(k_LabelingId, found ? entry.color : Color.black);

            Texture mainTex = null;
            var mainTexSt = new Vector4(1, 1, 0, 0);

            if (renderer && renderer.material)
            {
                if (renderer.material.HasProperty("_MainTex"))
                {
                    var mat = renderer.material;
                    mainTex = mat.GetTexture("_MainTex");
                    mainTexSt.x = mat.mainTextureScale.x;
                    mainTexSt.y = mat.mainTextureScale.y;
                    mainTexSt.z = mat.mainTextureOffset.x;
                    mainTexSt.w = mat.mainTextureOffset.y;

                    // if (maintex)
                    //     mpb.SetTexture("_MainTex", renderer.material.GetTexture("_MainTex"));
                }
                mpb.SetColor("_BaseColor", renderer.material.color);
            }

            var segBeh = renderer? renderer.gameObject.GetComponent<SemanticSegmentationBehaviour>() : null;
            if (segBeh)
            {
                if (segBeh.useSegmentationMask)
                {
                    mpb.SetFloat("_TextureIsSegmentationMask", 1);
                    if (segBeh.useMainTextureAsSegmask)
                    {
                        if (mainTex == null)
                            Debug.LogError("No texture found on object");
                        else
                        {
                            mpb.SetTexture("_MainTex", mainTex);
                        }
                    }
                    else
                        mpb.SetTexture("_MainTex", segBeh.segmentationMask);

                    mpb.SetVector("_MainTex_ST", mainTexSt);
                }
                //mpb.SetFloat("_TransparencyThreshold", segBeh.opacityThreshold);
            }

        }

        public override void ClearMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            mpb.SetVector(k_LabelingId, Color.black);
        }
    }
}
