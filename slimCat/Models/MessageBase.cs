﻿#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageBase.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    #region Usings

    using System;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     The message base.
    /// </summary>
    public abstract class MessageBase : SysProp, IDisposable
    {
        #region Fields

        private readonly DateTimeOffset posted;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageBase" /> class.
        /// </summary>
        protected MessageBase()
        {
            posted = DateTimeOffset.Now;
        }

        protected MessageBase(DateTimeOffset posted)
        {
            this.posted = posted;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the posted time.
        /// </summary>
        public DateTimeOffset PostedTime
        {
            get { return posted; }
        }

        /// <summary>
        ///     Gets the time stamp.
        /// </summary>
        public string TimeStamp
        {
            get 
            {
                return !ApplicationSettings.UseCustomTimeStamp
                    ? posted.ToTimeStamp()
                    : posted.ToString(ApplicationSettings.CustomTimeStamp);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Methods

        protected abstract override void Dispose(bool isManaged);

        #endregion
    }
}