﻿#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BroadcastCommand.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    public class PromoteDemoteEventArgs : CharacterUpdateEventArgs
    {
        public bool IsPromote { get; set; }

        public string TargetChannel { get; set; }

        public string TargetChannelId { get; set; }

        public override string ToString()
        {
            if (TargetChannel == null)
                return "has been " + (IsPromote ? "promoted to" : "demoted from") + " global moderator.";

            return "has been " + (IsPromote ? "promoted to" : "demoted from") + " channel moderator in "
                    + TargetChannel + ".";
        }
    }
}

namespace slimCat.Services
{
    using Models;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {
        private void PromoteOrDemote(string character, bool isPromote, string channelId = null)
        {
            var target = CharacterManager.Find(character);

            string title = null;
            if (channelId != null)
            {
                var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);
                if (channel != null)
                {
                    title = channel.Title;
                    if (isPromote)
                        channel.CharacterManager.Add(character, ListKind.Moderator);
                    else
                        channel.CharacterManager.Remove(character, ListKind.Moderator);
                }
            }


            if (target == null) return;

            var updateArgs = new PromoteDemoteEventArgs
            {
                TargetChannelId = channelId,
                TargetChannel = title,
                IsPromote = isPromote,
            };

            Events.NewCharacterUpdate(target, updateArgs);
        }

        private void OperatorPromoteCommand(IDictionary<string, object> command)
        {
            var target = command.Get(Constants.Arguments.Character);
            string channelId = null;

            if (command.ContainsKey(Constants.Arguments.Channel))
                channelId = command.Get(Constants.Arguments.Channel);

            PromoteOrDemote(target, true, channelId);
        }

        private void OperatorDemoteCommand(IDictionary<string, object> command)
        {
            var target = command.Get(Constants.Arguments.Character);
            string channelId = null;

            if (command.ContainsKey(Constants.Arguments.Channel))
                channelId = command.Get(Constants.Arguments.Channel);

            PromoteOrDemote(target, false, channelId);
        }

    }
}