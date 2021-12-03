// Copyright (c) 2010-2017 Anders Gustafsson, Cureos AB.
// All rights reserved. Any unauthorised reproduction of this 
// material will constitute an infringement of copyright.

namespace FellowOakDicom.Imaging
{
    /// <summary>
    /// Unity3D based image manager implementation.
    /// </summary>
    public class UnityImageManager : IImageManager
    {

        #region METHODS

        /// <summary>
        /// Create <see cref="IImage"/> object using the current implementation.
        /// </summary>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <returns><see cref="IImage"/> object using the current implementation.</returns>
        public IImage CreateImage(int width, int height)
        {
            return new UnityImage(width, height);
        }

        #endregion
    }
}
