﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using tweetz.core.Commands;
using tweetz.core.Views.UserProfileBlock;
using tweetz.core.Models;
using twitter.core.Models;

namespace tweetz.core.Services
{
    public static class FlowContentService
    {
        public static IEnumerable<Inline> FlowContentInlines(TwitterStatus twitterStatus)
        {
            if (twitterStatus is null)
            {
                yield break;
            }

            var nodes = FlowContentNodes(twitterStatus);

            foreach (var node in nodes)
            {
                switch (node.FlowContentNodeType)
                {
                    case FlowContentNodeType.Text:
                        yield return Run(node.Text);
                        break;

                    case FlowContentNodeType.Url:
                        yield return Link(node.Text);
                        break;

                    case FlowContentNodeType.Mention:
                        yield return Mention(node.Text);
                        break;

                    case FlowContentNodeType.HashTag:
                        yield return Hashtag(node.Text);
                        break;

                    case FlowContentNodeType.Media:
                        // Media is handled else where
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static Run Run(string text)
        {
            return new Run(ConvertXmlEntities(text));
        }

        private static InlineUIContainer Link(string link)
        {
            const int maxDisplayLength = 150;

            var hyperlink = new Hyperlink(new Run(link))
            {
                CommandParameter = link,
                ToolTip = link
            };

            hyperlink.Click += (_, __) => OpenLinkCommand.Command.Execute(link, null);
            hyperlink.ToolTipOpening += LongUrlService.HyperlinkToolTipOpeningHandler;

            var textblock = new TextBlock(hyperlink)
            {
                MaxWidth = maxDisplayLength,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var container = new InlineUIContainer(textblock);
            return container;
        }

        private static Hyperlink Mention(string text)
        {
            var tooltip = new ToolTip();
            var userProfile = new UserProfileBlock();

            tooltip.Content = userProfile;
            tooltip.Style = GetToolTipStyle();
            userProfile.Tag = text;
            var link = $"https://twitter.com/{text}";

            var hyperlink = new Hyperlink(new Run("@" + text))
            {
                CommandParameter = link,
                ToolTip = tooltip
            };

            hyperlink.Click += (_, __) => OpenLinkCommand.Command.Execute(link, null);
            return hyperlink;
        }

        private static Style? userProfileToolTipStyle;

        private static Style GetToolTipStyle()
        {
            return userProfileToolTipStyle ??= (Style)Application.Current.FindResource("ToolTipStyle");
        }

        private static Hyperlink Hashtag(string text)
        {
            var tag = "#" + text;
            var hyperlink = new Hyperlink(new Run(tag))
            {
                CommandParameter = tag
            };

            hyperlink.Click += (_, __) => SearchCommand.Command.Execute(tag, null);
            return hyperlink;
        }

        private static string ConvertXmlEntities(string text)
        {
            return text
                .Replace("&lt;", "<", StringComparison.Ordinal)
                .Replace("&gt;", ">", StringComparison.Ordinal)
                .Replace("&quot;", "\"", StringComparison.Ordinal)
                .Replace("&apos;", "'", StringComparison.Ordinal)
                .Replace("&amp;", "&", StringComparison.Ordinal);
        }

        private static IEnumerable<FlowContentNode> FlowContentNodes(TwitterStatus twitterStatus)
        {
            var start = 0;
            var twitterString = new TwitterString(twitterStatus.FullText ?? twitterStatus.Text ?? string.Empty);

            foreach (var item in FlowControlItems(twitterStatus.Entities ?? new Entities()))
            {
                if (item.Start >= start)
                {
                    var len = item.Start - start;
                    var text = twitterString.Substring(start, len);
                    yield return new FlowContentNode(FlowContentNodeType.Text, text);
                }

                yield return new FlowContentNode(item.FlowContentNodeType, item.Text);
                start = item.End;
            }

            yield return new FlowContentNode(FlowContentNodeType.Text, twitterString.Substring(start));
        }

        private static IEnumerable<FlowContentItem> FlowControlItems(Entities entities)
        {
            var urls = entities.Urls
                 ?.Select(url => new FlowContentItem
                 (
                     nodeType: FlowContentNodeType.Url,
                     text: url.ExpandedUrl,
                     start: url.Indices[0],
                     end: url.Indices[1]
                 ))
                 ?? Enumerable.Empty<FlowContentItem>();

            var mentions = entities.Mentions
                ?.Select(mention => new FlowContentItem
                (
                    nodeType: FlowContentNodeType.Mention,
                    text: mention.ScreenName,
                    start: mention.Indices[0],
                    end: mention.Indices[1]
                ))
                ?? Enumerable.Empty<FlowContentItem>();

            var hashTags = entities.HashTags
                ?.Select(hashtag => new FlowContentItem
                (
                    nodeType: FlowContentNodeType.HashTag,
                    text: hashtag.Text,
                    start: hashtag.Indices[0],
                    end: hashtag.Indices[1]
                ))
                ?? Enumerable.Empty<FlowContentItem>();

            var media = entities.Media
                ?.Select(media => new FlowContentItem
                (
                    nodeType: FlowContentNodeType.Media,
                    text: media.Url,
                    start: media.Indices[0],
                    end: media.Indices[1]
                ))
                ?? Enumerable.Empty<FlowContentItem>();

            return urls
                .Concat(mentions)
                .Concat(hashTags)
                .Concat(media)
                .OrderBy(o => o.Start);
        }
    }
}