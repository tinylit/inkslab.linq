[CmdletBinding(PositionalBinding = $false)]
param(
    [bool] $CreatePackages = $true,
    [switch] $SkipRestore
)

$ErrorActionPreference = 'Stop'
$packageOutputFolder = Join-Path $PSScriptRoot '.nupkgs'
$projectsToBuild =
    'Inkslab.Transactions',
    'Inkslab.Linq',
    'Inkslab.Linq.MySql',
    'Inkslab.Linq.SqlServer',
    'Inkslab.Linq.PostgreSQL'

function Invoke-CheckedDotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

Push-Location $PSScriptRoot
try {
    if (-not $SkipRestore) {
        Invoke-CheckedDotnet restore inkslab.linq.sln
    }
    Invoke-CheckedDotnet build inkslab.linq.sln -c Release --no-restore
    if ($CreatePackages) {
        foreach ($project in $projectsToBuild) {
            Invoke-CheckedDotnet pack "src/$project/$project.csproj" -c Release --no-build --no-restore -o $packageOutputFolder
        }
        $packageVersion = & dotnet msbuild 'src/Inkslab.Linq/Inkslab.Linq.csproj' -nologo -getProperty:Version
        if ($LASTEXITCODE -ne 0) { throw 'Cannot determine package version.' }
    }
    Write-Host 'Build Complete.' -ForegroundColor Green
}
finally {
    Pop-Location
}
