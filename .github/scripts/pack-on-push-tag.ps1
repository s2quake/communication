#!/usr/local/bin/pwsh
param (
    [string]$OutputPath,
    [string]$KeyPath,
    [string]$NugetApiKey,
    [string]$tagName
)

Remove-Item -Force -Recurse pack -ErrorAction SilentlyContinue  

.github/scripts/pack.ps1 `
    -OutputPath $OutputPath `
    -KeyPath $KeyPath `
    -CommitSHA $tagName

$files = Get-ChildItem -Path pack -Filter "*.nupkg" | ForEach-Object {
    dotnet nuget push `
        $_ `
        --api-key $NugetApiKey `
        --source https://api.nuget.org/v3/index.json
    $_.FullName
}

gh release create --generate-notes --latest --title "Release $tagName" $tagName $files
