
$dir = $PSScriptRoot
$ErrorActionPreference = "Stop"

$config = "Release"
dotnet build "$dir\sourcecode" -c $config
if ($LASTEXITCODE) {
    exit $LASTEXITCODE
}

$mod_name = "SBCameraScroll"
$dll_name = $mod_name + ".dll"
$pdb_name = $mod_name + ".pdb"

function copy_file($file_name) {
    $src = Get-ChildItem -Path "$dir\sourcecode\bin\$config" -Recurse -Filter $file_name | Select-Object -ExpandProperty FullName
    $dst = Join-Path -Path "$dir\$mod_name\plugins" -ChildPath $file_name

    # Write-Host $src
    # Write-Host $dst

    New-Item -ItemType Directory -Path (Split-Path $dst) -Force | Out-Null
    Move-Item -Path $src -Destination $dst -Force
}

copy_file($dll_name)
copy_file($pdb_name)

$asset_bundles_path = "$dir\$mod_name\assetbundles"
$delete_file_paths = @(
    "$asset_bundles_path\assetbundles",
    "$asset_bundles_path\assetbundles.meta",
    "$asset_bundles_path\assetbundles.manifest",
    "$asset_bundles_path\assetbundles.manifest.meta",
    "$asset_bundles_path\modded_shaders.meta",
    "$asset_bundles_path\modded_shaders.manifest",
    "$asset_bundles_path\modded_shaders.manifest.meta"
)

foreach ($file_path in $delete_file_paths) {
    if (Test-Path $file_path) {
        Remove-Item -Path "$file_path" -Force
    }
}

