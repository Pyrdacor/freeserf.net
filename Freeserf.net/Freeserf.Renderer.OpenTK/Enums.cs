using System;
using System.Collections.Generic;
using System.Text;

namespace Freeserf.Renderer.OpenTK
{
    public enum SizingPolicy
    {
        FitRatio,
        FitWindow,
        FitRatioKeepOrientation,
        FitWindowKeepOrientation,
        FitRatioForcePortrait,
        FitRatioForceLandscape,
        FitWindowForcePortrait,
        FitWindowForceLandscape
    }

    public enum DeviceType
    {
        Desktop,
        MobilePortrait,
        MobileLandscape
    }

    public enum Orientation
    {
        Default = -1,
        PortraitTopDown,
        PortraitBottomUp,
        LandscapeLeftRight,
        LandscapeRightLeft
    }

    public enum OrientationPolicy
    {
        Fixed,
        Support180DegreeRotation
    }

    internal enum Rotation
    {
        None,
        Deg90,
        Deg180,
        Deg270
    }
}
