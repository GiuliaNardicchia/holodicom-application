// Copyright (c) 2010-2017 Anders Gustafsson, Cureos AB.
// All rights reserved. Any unauthorised reproduction of this 
// material will constitute an infringement of copyright.

namespace FellowOakDicom.Imaging
{
    using System.Collections.Generic;

    using FellowOakDicom.Imaging.Render;
    using FellowOakDicom.IO;

    using UnityEngine;

    /// <summary>
    /// Convenience class for non-generic access to <see cref="UnityImage"/> image objects.
    /// </summary>
    public static class UnityImageExtensions
    {
        /// <summary>
        /// Convenience method to access WinForms <see cref="IImage"/> instance as WinForms <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="image"><see cref="IImage"/> object.</param>
        /// <returns><see cref="Texture2D"/> contents of <paramref name="image"/>.</returns>
        public static Texture2D AsTexture2D(this IImage image)
        {
            return image.As<Texture2D>();
        }
    }

    /// <summary>
    /// <see cref="IImage"/> implementation of a Unity <see cref="Texture2D"/>.
    /// </summary>
    public sealed class UnityImage : ImageBase<Texture2D>
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Initializes an instance of the <see cref="UnityImage"/> object.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        public UnityImage(int width, int height)
            : base(width, height, new PinnedIntArray(width * height), null)
        {
        }

        /// <summary>
        /// Initializes an instance of the <see cref="UnityImage"/> object.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="pixels">Pixel array.</param>
        /// <param name="image">Bitmap image.</param>
        public UnityImage(int width, int height, PinnedIntArray pixels, Texture2D image)
            : base(width, height, pixels, image)
        {
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Renders the image given the specified parameters.
        /// </summary>
        /// <param name="components">Number of components.</param>
        /// <param name="flipX">Flip image in X direction?</param>
        /// <param name="flipY">Flip image in Y direction?</param>
        /// <param name="rotation">Image rotation.</param>
        public override void Render(int components, bool flipX, bool flipY, int rotation)
        {
            var w = this.width;
            var h = this.height;

            // Note that image is flipped in Y direction due to Texture2D coordinate system.
            var bytes = ToBytes(ref w, ref h, components, flipX, !flipY, rotation, this.pixels.Data);

            this.image = Create(w, h, bytes);
        }

        /// <summary>
        /// Draw graphics onto existing image.
        /// </summary>
        /// <param name="graphics">Graphics to draw.</param>
        public override void DrawGraphics(IEnumerable<IGraphic> graphics)
        {
            foreach (var graphic in graphics)
            {
                var layer = graphic.RenderImage(null).As<Texture2D>();
                this.image.SetPixels(
                    graphic.ScaledOffsetX,
                    graphic.ScaledOffsetY,
                    graphic.ScaledWidth,
                    graphic.ScaledHeight,
                    layer.GetPixels());
            }

            this.image.Apply();
        }

        /// <summary>
        /// Creates a deep copy of the image.
        /// </summary>
        /// <returns>Deep copy of this image.</returns>
        public override IImage Clone()
        {
            return new UnityImage(
                this.width,
                this.height,
                new PinnedIntArray(this.pixels.Data),
                this.image == null ? null : Create(this.image));
        }

        public static Texture2D Create(int w, int h, byte[] bytes)
        {
            // Switch byte representation from BGRA to RGBA
            var length = bytes.Length;
            for (var k = 0; k < length; k += 4)
            {
                var tmp = bytes[k];
                bytes[k] = bytes[k + 2];
                bytes[k + 2] = tmp;
            }

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(bytes);
            texture.Apply();

            return texture;
        }

        public static Texture2D Create(Texture2D image)
        {
            var texture = new Texture2D(image.width, image.height, TextureFormat.BGRA32, false);
            texture.SetPixels(image.GetPixels());
            texture.Apply();

            return texture;
        }

        #endregion
    }
}
