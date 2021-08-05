using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityObject = UnityEngine.Object;
using System;

#if SRPCORE_IS_INSTALLED_FOR_UTS
namespace Unity.Rendering.Toon
{
    /// <summary>
    /// A volume component that holds settings for the Toon Ev Adjustment Curve.
    /// </summary>
    [Serializable, VolumeComponentMenu("Toon/EV Adjustment Curve")]
    public sealed class ToonEvAdjustmentCurve : VolumeComponent
    {
        // flags
        bool m_initialized = false;
        bool m_srpCallbackInitialized = false;

        const int kAdjustmentCurvePrecision = 128;
#if false
        const string kLightAdjustmentPropName = "_UTS_LightAdjustment";
#endif
        const string kExposureAdjustmentPorpName = "_ToonEvAdjustmentCurve";
        const string kExposureArrayPropName = "_ToonEvAdjustmentValueArray";
        const string kExposureMinPropName   = "_ToonEvAdjustmentValueMin";
        const string kExposureMaxPropName   = "_ToonEvAdjustmentValueMax";

#if false
        [SerializeField]
        public bool m_LightAdjustment = false;
#endif
        [SerializeField]
        internal bool m_ExposureAdjustmnt = false;
        [SerializeField]
        internal AnimationCurve m_AnimationCurve = AnimationCurve.Linear(-10f, -10f, 16f, 16f);
        [SerializeField]
        internal float[] m_ExposureArray;
        [SerializeField]
        internal float m_Max, m_Min;
        [SerializeField]
        internal bool m_DebugUI;

#if UNITY_EDITOR
#pragma warning restore CS0414
        bool m_isCompiling = false;
#endif
        /// <summary>
        /// Specifies the method that Toon Shader uses to adjust the EV.
        /// This parameter is only used when <see cref="ToonEvAdjustmentCurve.adjustmentMode"/> is set.
        /// </summary>
        [Tooltip("Specifies the method that Toon Shader uses to adjust the EV.")]
        public ToonEVAdjustmentModeParamater adjustmentMode = new ToonEVAdjustmentModeParamater(ToonEVAdjustmentMode.NoAdjustment);

        /// <summary>
        /// Specifies a curve that remaps the Toon exposure on the x-axis to the EV you want on the y-axis.
        /// This parameter is only used when <see cref="ToonEvAdjustmentCurve.adjustmentMode"/> is set.
        /// </summary>
        [Tooltip("Specifies a curve that remaps the Toon EV on the x-axis to the EV you want on the y-axis.")]
        public AnimationCurveParameter curveMap = new AnimationCurveParameter(AnimationCurve.Linear(-10f, -10f, 16f, 16f)); // TODO: Use TextureCurve instead?



        void Update()
        {

            Initialize();



            // Fail safe in case the curve is deleted / has 0 point
            var curve = m_AnimationCurve;
            

            if (curve == null || curve.length == 0)
            {
                m_Min = 0f;
                m_Max = 0f;

                for (int i = 0; i < kAdjustmentCurvePrecision; i++)
                    m_ExposureArray[i] = 0.0f;
            }
            else
            {
                m_Min = curve[0].time;
                m_Max = curve[curve.length - 1].time;
                float step = (m_Max - m_Min) / (kAdjustmentCurvePrecision - 1f);

                for (int i = 0; i < kAdjustmentCurvePrecision; i++)
                    m_ExposureArray[i] = curve.Evaluate(m_Min + step * i);
            }


#if UNITY_EDITOR
            // handle script recompile
            if (EditorApplication.isCompiling && !m_isCompiling)
            {
                // on compile begin
                m_isCompiling = true;
                Release();
            }
            else if (!EditorApplication.isCompiling && m_isCompiling)
            {
                // on compile end
                m_isCompiling = false;
            }
#endif
            Shader.SetGlobalFloatArray(kExposureArrayPropName, m_ExposureArray);
            Shader.SetGlobalFloat(kExposureMinPropName, m_Min);
            Shader.SetGlobalFloat(kExposureMaxPropName, m_Max);
            Shader.SetGlobalInt(kExposureAdjustmentPorpName, m_ExposureAdjustmnt ? 1 : 0);
#if false
            Shader.SetGlobalInt(kLightAdjustmentPropName, m_LightAdjustment ? 1 : 0);
#endif


        }

        void EnableSrpCallbacks()
        {

            if (!m_srpCallbackInitialized)
            {
                m_srpCallbackInitialized = true;
            }
        }
        void DisableSrpCallbacks()
        {
            if (m_srpCallbackInitialized)
            {
                m_srpCallbackInitialized = false;
            }
        }

        void OnEnable()
        {
            base.OnEnable();

            Initialize();

            EnableSrpCallbacks();

        }

        void OnDisable()
        {
            DisableSrpCallbacks();

            Release();
            base.OnDisable();
        }

        void Initialize()
        {
            if (m_initialized)
            {
                return;
            }
#if UNITY_EDITOR
            // initializing renderer can interfere GI baking. so wait until it is completed.

            if (EditorApplication.isCompiling)
                return;
#endif

            if (m_ExposureArray == null)
            {
                m_ExposureArray = new float[kAdjustmentCurvePrecision];
            }
            m_initialized = true;
        }


        void Release()
        {
            if (m_initialized)
            {
            }

            m_initialized = false;
            Shader.SetGlobalInt(kExposureAdjustmentPorpName, 0);
        }
    }

    /// <summary>
    /// Methods that HDRP uses to change the exposure when the Camera moves from dark to light and vice versa.
    /// </summary>
    /// <seealso cref="Exposure.adjustmentMode"/>
    public enum ToonEVAdjustmentMode
    {
        /// <summary>
        /// No Adjustment
        /// </summary>
        NoAdjustment,

        /// <summary>
        /// The EV changes correspond with the curve.
        /// </summary>
        CurveAdjustment
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ToonEVAdjustmentMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class ToonEVAdjustmentModeParamater : VolumeParameter<ToonEVAdjustmentMode>
    {
        /// <summary>
        /// Creates a new <see cref="ToonEVAdjustmentModeParamater"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ToonEVAdjustmentModeParamater(ToonEVAdjustmentMode value, bool overrideState = false) : base(value, overrideState) { }
    }

}
#endif