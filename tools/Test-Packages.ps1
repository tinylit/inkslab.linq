[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,
    [Parameter(Mandatory = $true)]
    [string] $Version
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$frameworks = [ordered] @{
    'netstandard2.1' = '.NETStandard2.1'
    'net6.0' = 'net6.0'
    'net8.0' = 'net8.0'
    'net10.0' = 'net10.0'
}
$packageNames = 'Inkslab.Transactions', 'Inkslab.Linq', 'Inkslab.Linq.MySql', 'Inkslab.Linq.SqlServer', 'Inkslab.Linq.PostgreSQL'
$documents = @('README.md', 'LICENSE', 'inkslab-mini.jpg', 'docs/多目标编译与性能.md') +
    @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot '..') -Filter 'README.*.md' -File | ForEach-Object { $_.Name })

foreach ($name in $packageNames) {
    $packagePath = Join-Path $PackageDirectory "$name.$Version.nupkg"
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $packagePath).Path)
    try {
        $entries = @($archive.Entries.FullName)
        foreach ($framework in $frameworks.Keys) {
            foreach ($extension in @('dll', 'xml')) {
                if ($entries -notcontains "lib/$framework/$name.$extension") {
                    throw "$packagePath missing lib/$framework/$name.$extension"
                }
            }
        }
        foreach ($document in $documents) {
            if ($entries -notcontains $document) {
                throw "$packagePath missing $document"
            }
        }
        $nuspec = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
        if ($nuspec.Count -ne 1) { throw "$packagePath must contain exactly one nuspec." }
        $reader = [IO.StreamReader]::new($nuspec[0].Open())
        try { [xml] $metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($metadata.package.metadata.id -ne $name -or $metadata.package.metadata.version -ne $Version) {
            throw "Wrong package identity or version in $packagePath"
        }
        $groups = @($metadata.package.metadata.dependencies.group)
        if ($groups.Count -ne $frameworks.Count) {
            throw "$packagePath must contain $($frameworks.Count) dependency groups."
        }
        foreach ($framework in $frameworks.Values) {
            $group = @($groups | Where-Object { $_.targetFramework -eq $framework })
            if ($group.Count -ne 1) { throw "$packagePath missing dependency group $framework" }
            foreach ($dependency in @($group[0].dependency)) {
                if ($dependency -and $packageNames -contains $dependency.id) {
                    if ($dependency.version -ne $Version -and $dependency.version -ne "[$Version, )") {
                        throw "$packagePath has an inconsistent dependency: $($dependency.id) $($dependency.version)"
                    }
                }
            }
        }
        Write-Host "${name}: identity, version, four DLL/XML targets, dependency groups and package documents verified."
    }
    finally { $archive.Dispose() }
}
