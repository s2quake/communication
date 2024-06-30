#!/usr/local/bin/pwsh
param (
    [string]$OutputPath,
    [string]$KeyPath,
    [string]$NugetApiKey
)

$commitMessage = "$(git log -1 --pretty=%B)"
$pattern = "(?<=Merge pull request #)(\d+)"
if (!($commitMessage -match $pattern)) {
    Write-Error "Commit message does not contain a pull request number."
}

Remove-Item -Force -Recurse pack -ErrorAction SilentlyContinue  

$commitSHA = "$(git log -1 --pretty=%H)"
.github/scripts/pack.ps1 `
    -OutputPath $OutputPath `
    -PullRequestNumber $matches[1] `
    -KeyPath $KeyPath `
    -CommitSHA $commitSHA

Get-ChildItem -Path $OutputPath -Filter "*.nupkg" | ForEach-Object {
    dotnet nuget push `
        $_ `
        --skip-duplicate `
        --api-key $NugetApiKey `
        --source https://api.nuget.org/v3/index.json
}
