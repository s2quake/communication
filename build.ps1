param(
    [string]$OutputPath = "bin",
    [string]$Framework = "netcoreapp3.1",
    [string]$KeyPath = "",
    [string]$LogPath = "",
    [switch]$Force
)

$solutionPath = "./communication.sln"
$propPaths = (
    "./JSSoft.Library/Directory.Build.props",
    "./JSSoft.Library.Commands/Directory.Build.props",
    "./JSSoft.Communication/Directory.Build.props"
)

if (!(Test-Path $OutputPath)) {
    New-Item $OutputPath -ItemType Directory
}
$OutputPath = Resolve-Path $OutputPath
$location = Get-Location
$buildFile = "./.vscode/build.ps1"
try {
    Set-Location $PSScriptRoot
    $propPaths = $propPaths | ForEach-Object { Resolve-Path $_ }
    $solutionPath = Resolve-Path $solutionPath
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/s2quake/build/master/build.ps1" -OutFile $buildFile
    $buildFile = Resolve-Path $buildFile
    & $buildFile $solutionPath $propPaths -Publish -KeyPath $KeyPath -Sign -OutputPath $OutputPath -Framework $Framework -LogPath $LogPath -Force:$Force
}
finally {
    Remove-Item $buildFile
    Set-Location $location
}
