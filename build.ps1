param(
    [string]$OutputPath = "",
    [string]$Framework = "net8.0",
    [string]$KeyPath = "",
    [string]$LogPath = "",
    [switch]$Pack
)

$solutionPath = Join-Path $PSScriptRoot "communication.sln" -Resolve
$buildFile = Join-Path $PSScriptRoot ".build" "build.ps1" -Resolve
if ($Pack) {
    & $buildFile $solutionPath -Pack -KeyPath "$KeyPath" -Sign -OutputPath "$OutputPath" -LogPath "$LogPath"
}
else {
    & $buildFile $solutionPath -Publish -KeyPath $KeyPath -Sign -OutputPath $OutputPath -Framework $Framework -LogPath $LogPath
}
