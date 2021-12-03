using System.Collections.Generic;
using UnityEngine;
using FellowOakDicom.Imaging.Mathematics;
using System;
using FellowOakDicom.Imaging;
using System.Linq;
using static UnityEngine.Mathf;

public static class GeometryExtensions
{

    public static Vector3 ToUnityVector3(this Vector3D self)
    {
        var (x, y, z, _) = self.ToArray().Select(Convert.ToSingle);
        return new Vector3(x, y, z);
    }

    public static Quaternion GetRotation(this FrameGeometry self)
    {
        var xDir = self.DirectionRow;
        var yDir = self.DirectionColumn;
        var zDir = self.DirectionNormal;
        var (m00, m10, m20, _) = xDir.ToArray().Select(Convert.ToSingle);
        var (m01, m11, m21, _) = yDir.ToArray().Select(Convert.ToSingle);
        var (m02, m12, m22, _) = zDir.ToArray().Select(Convert.ToSingle);
        var (zRot, xRot, yRot, _) = new[] {
            Atan2(-m10, m11),
            Asin(m12),
            Atan2(-m02, m22)
        }.Select(x => x * 180.0f / PI);
        return Quaternion.Euler(xRot, yRot, zRot);
    }

    public static double[] GetPixelSpacing(this FrameGeometry self) => new[]
            {
                self.PixelSpacingBetweenColumns,
                self.PixelSpacingBetweenRows
            };

    public static int[] GetFrameSize(this FrameGeometry self) => new[]
            {
                self.FrameSize.X,
                self.FrameSize.Y
            };

    public static Vector3 GetScalingVector(this FrameGeometry self)
    {
        var (scaleX, scaleY, _) =
            self.GetPixelSpacing()
            .Zip(
                self.GetFrameSize(),
                (x, y) => x * y / 1000.0
            )
            .Select(Convert.ToSingle);
        return
            scaleX * Vector3.right +
            scaleY * Vector3.up;
    }

    public static Vector3 ProjectOnRotatedSpace(this FrameGeometry self)
    {
        var imagePosition = self.PointTopLeft.ToVector();
        var (x, y, z, _) = new[]
        {
            imagePosition.DotProduct(self.DirectionRow),
            imagePosition.DotProduct(self.DirectionColumn),
            imagePosition.DotProduct(self.DirectionNormal),
        }
        .Select(x => x / 1000.0)
        .Select(Convert.ToSingle);
        return new Vector3(
            x,
            y,
            z
        );
    }

    public static IEnumerable<float> GetSliceDepths(this IEnumerable<FrameGeometry> geometryInfo)
    {
        return geometryInfo
            .Select(x => x.DirectionNormal.DotProduct(x.PointTopLeft))
            .Select(Convert.ToSingle)
            .Select(x => x / 1000.0f);
    }
}

