param(
    [string]$OutputPath = "",
    [string]$Framework = "net8.0",
    [string]$KeyPath = "",
    [string]$LogPath = ""
)

$solutionPath = Join-Path $PSScriptRoot "communication.sln" -Resolve
$buildFile = Join-Path $PSScriptRoot "build" "build.ps1" -Resolve
& $buildFile $solutionPath -Publish -KeyPath $KeyPath -Sign -OutputPath $OutputPath -Framework $Framework -LogPath $LogPath
