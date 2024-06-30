#!/usr/local/bin/pwsh
param (
    [string]$OutputPath,
    [int]$PullRequestNumber,
    [string]$KeyPath
)

.github/scripts/pack.ps1 `
    -OutputPath $OutputPath `
    -PullRequestNumber $PullRequestNumber `
    -KeyPath $KeyPath
