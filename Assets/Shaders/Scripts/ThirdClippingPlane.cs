using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class ThirdClippingPlane : ClippingPlane
{
    /// <inheritdoc />
    protected override string Keyword
    {
        get { return "_CLIPPING_PLANE3"; }
    }

    /// <inheritdoc />
    protected override string ClippingSideProperty
    {
        get { return "_ClipPlaneSide3"; }
    }

    /// <inheritdoc />
    protected override void Initialize()
    {
        base.Initialize();
        clipPlaneID = Shader.PropertyToID("_ClipPlane3");
    }
}

