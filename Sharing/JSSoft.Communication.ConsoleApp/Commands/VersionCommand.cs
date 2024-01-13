using System.ComponentModel.Composition;
using JSSoft.Commands;

namespace JSSoft.Communication.Commands;

[Export(typeof(ICommand))]
[Export(typeof(VersionCommand))]
sealed class VersionCommand : VersionCommandBase
{
}
