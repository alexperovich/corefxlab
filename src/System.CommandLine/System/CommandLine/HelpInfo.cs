using System.Linq;
using System.Collections.Generic;

namespace System.CommandLine
{
    public class OperationInfo
    {
        public IReadOnlyList<OptionInfo> Options { get; }
        public IReadOnlyList<ParameterInfo> Parameters { get; }

        protected OperationInfo(ArgumentSyntax syntax, ArgumentCommand command)
        {
            Options = syntax.GetOptions(command)
                .Where(o => !o.IsHidden)
                .Select(o => new OptionInfo(o))
                .ToList();
            Parameters = syntax.GetParameters(command)
                .Where(p => !p.IsHidden)
                .Select(p => new ParameterInfo(p))
                .ToList();
        }
    }

    public class HelpInfo : OperationInfo
    {
        public string ApplicationName { get; }
        public string ActiveCommandName { get; }
        public IReadOnlyList<CommandInfo> Commands { get; }

        internal HelpInfo(ArgumentSyntax syntax)
            : base(syntax, null)
        {
            ApplicationName = syntax.ApplicationName;
            ActiveCommandName = syntax.ActiveCommand?.Name;
            Commands = syntax.Commands
                .Where(c => !c.IsHidden)
                .Select(c => new CommandInfo(syntax, c))
                .ToList();
        }
    }

    public class CommandInfo : OperationInfo
    {
        public string Name { get; }
        public string Help { get; }

        internal CommandInfo(ArgumentSyntax syntax, ArgumentCommand command)
            : base(syntax, command)
        {
            Name = command.Name;
            Help = command.Help;
        }
    }

    public class ArgumentInfo
    {
        public string DisplayName { get; }
        public IReadOnlyList<string> DisplayNames { get; }
        public string Help { get; }
        public bool IsList { get; }

        internal ArgumentInfo(Argument arg)
        {
            DisplayName = arg.GetDisplayName();
            DisplayNames = arg.GetDisplayNames().ToList();
            Help = arg.Help;
            IsList = arg.IsList;
        }
    }

    public class OptionInfo : ArgumentInfo
    {
        public bool IsValueRequired { get; }
        public bool IsFlag { get; }

        internal OptionInfo(Argument option)
            : base(option)
        {
            IsFlag = option.IsFlag;
            IsValueRequired = option.IsRequired;
        }
    }

    public class ParameterInfo : ArgumentInfo
    {
        internal ParameterInfo(Argument parameter)
            : base(parameter)
        {
        }
    }
}