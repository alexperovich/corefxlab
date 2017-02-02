// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.CommandLine
{
    internal static class HelpTextGenerator
    {
        public static string Generate(HelpInfo helpInfo, int maxWidth)
        {
            HelpPage page;
            if (string.IsNullOrEmpty(helpInfo.ActiveCommandName) && helpInfo.Commands.Any())
            {
                page = GetCommandListHelp(helpInfo);
            }
            else
            {
                page = GetCommandHelp(helpInfo, helpInfo.ActiveCommandName);
            }
            var sb = new StringBuilder();
            sb.WriteHelpPage(page, maxWidth);
            return sb.ToString();
        }

        private struct HelpPage
        {
            public string ApplicationName;
            public IEnumerable<string> SyntaxElements;
            public IReadOnlyList<HelpRow> Rows;
        }

        private struct HelpRow
        {
            public string Header;
            public string Text;
        }

        private static void WriteHelpPage(this StringBuilder sb, HelpPage page, int maxWidth)
        {
            sb.WriteUsage(page.ApplicationName, page.SyntaxElements, maxWidth);

            if (!page.Rows.Any())
                return;

            sb.AppendLine();

            sb.WriteRows(page.Rows, maxWidth);

            sb.AppendLine();
        }

        private static void WriteUsage(this StringBuilder sb, string applicationName, IEnumerable<string> syntaxElements, int maxWidth)
        {
            var usageHeader = string.Format(Strings.HelpUsageOfApplicationFmt, applicationName);
            sb.Append(usageHeader);

            if (syntaxElements.Any())
                sb.Append(@" ");

            var syntaxIndent = usageHeader.Length + 1;
            var syntaxMaxWidth = maxWidth - syntaxIndent;

            sb.WriteWordWrapped(syntaxElements, syntaxIndent, syntaxMaxWidth);
        }

        private static void WriteRows(this StringBuilder sb, IReadOnlyList<HelpRow> rows, int maxWidth)
        {
            const int indent = 4;
            var maxColumnWidth = rows.Select(r => r.Header.Length).Max();
            var helpStartColumn = maxColumnWidth + 2 * indent;

            var maxHelpWidth = maxWidth - helpStartColumn;
            if (maxHelpWidth < 0)
                maxHelpWidth = maxWidth;

            foreach (var row in rows)
            {
                var headerStart = sb.Length;

                sb.Append(' ', indent);
                sb.Append(row.Header);

                var headerLength = sb.Length - headerStart;
                var requiredSpaces = helpStartColumn - headerLength;

                sb.Append(' ', requiredSpaces);

                var words = SplitWords(row.Text);
                sb.WriteWordWrapped(words, helpStartColumn, maxHelpWidth);
            }
        }

        private static void WriteWordWrapped(this StringBuilder sb, IEnumerable<string> words, int indent, int maxidth)
        {
            var helpLines = WordWrapLines(words, maxidth);
            var isFirstHelpLine = true;

            foreach (var helpLine in helpLines)
            {
                if (isFirstHelpLine)
                    isFirstHelpLine = false;
                else
                    sb.Append(' ', indent);

                sb.AppendLine(helpLine);
            }

            if (isFirstHelpLine)
                sb.AppendLine();
        }

        private static HelpPage GetCommandListHelp(HelpInfo info)
        {
            return new HelpPage
            {
                ApplicationName = info.ApplicationName,
                SyntaxElements = GetGlobalSyntax(),
                Rows = GetCommandRows(info).ToArray()
            };
        }

        private static HelpPage GetCommandHelp(HelpInfo info, string commandName)
        {
            var command = info.Commands.FirstOrDefault(c => c.Name == commandName);
            return new HelpPage
            {
                ApplicationName = info.ApplicationName,
                SyntaxElements = GetCommandSyntax((OperationInfo) command ?? info),
                Rows = GetArgumentRows((OperationInfo) command ?? info).ToArray()
            };
        }

        private static IEnumerable<string> GetGlobalSyntax()
        {
            yield return @"<command>";
            yield return @"[<args>]";
        }

        private static IEnumerable<string> GetCommandSyntax(OperationInfo info)
        {
            if (info is CommandInfo command)
                yield return command.Name;

            foreach (var option in info.Options)
                yield return GetOptionSyntax(option);

            if (info.Options.Any() && info.Parameters.Any())
            {
                yield return @"[--]";
            }

            foreach (var parameter in info.Parameters)
                yield return GetParameterSyntax(parameter);
        }

        private static string GetOptionSyntax(OptionInfo option)
        {
            var sb = new StringBuilder();

            sb.Append(@"[");
            sb.Append(option.DisplayName);

            if (!option.IsFlag)
                sb.Append(option.IsValueRequired ? @" <arg>" : @" [arg]");

            if (option.IsList)
                sb.Append(@"...");

            sb.Append(@"]");

            return sb.ToString();
        }

        private static string GetParameterSyntax(ParameterInfo parameter)
        {
            var sb = new StringBuilder();

            sb.Append(parameter.DisplayName);
            if (parameter.IsList)
                sb.Append(@"...");

            return sb.ToString();
        }

        private static IEnumerable<HelpRow> GetCommandRows(HelpInfo info)
        {
            return info.Commands
                .Select(c => new HelpRow {Header = c.Name, Text = c.Help});
        }

        private static IEnumerable<HelpRow> GetArgumentRows(OperationInfo info)
        {
            foreach (var option in info.Options)
                yield return new HelpRow { Header = GetArgumentRowHeader(option), Text = option.Help};

            foreach (var parameter in info.Parameters)
                yield return new HelpRow { Header = GetArgumentRowHeader(parameter), Text = parameter.Help};
        }

        private static string GetArgumentRowHeader(ArgumentInfo parameter)
        {
            var sb = new StringBuilder();

            foreach (var displayName in parameter.DisplayNames)
            {
                if (sb.Length > 0)
                    sb.Append(@", ");

                sb.Append(displayName);
            }

            if (parameter is OptionInfo option && !option.IsFlag)
            {
                sb.Append(option.IsValueRequired ? @" <arg>" : @" [arg]");
            }

            if (parameter.IsList)
                sb.Append(@"...");

            return sb.ToString();
        }

        private static IEnumerable<string> WordWrapLines(IEnumerable<string> tokens, int maxWidth)
        {
            var sb = new StringBuilder();

            foreach (var token in tokens)
            {
                var newLength = sb.Length == 0
                    ? token.Length
                    : sb.Length + 1 + token.Length;

                if (newLength > maxWidth)
                {
                    if (sb.Length == 0)
                    {
                        yield return token;
                        continue;
                    }

                    yield return sb.ToString();
                    sb.Clear();
                }

                if (sb.Length > 0)
                    sb.Append(@" ");

                sb.Append(token);
            }

            if (sb.Length > 0)
                yield return sb.ToString();
        }

        private static IEnumerable<string> SplitWords(string text)
        {
            return string.IsNullOrEmpty(text)
                ? Enumerable.Empty<string>()
                : text.Split(' ');
        }
    }
}