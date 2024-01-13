using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[Export(typeof(HelpCommand))]
sealed class HelpCommand : HelpCommandBase
{
}
