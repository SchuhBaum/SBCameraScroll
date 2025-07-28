
. ".\variables.ps1";

#

dotnet build "$dir\sourcecode" -c $config -v:detailed
if ($LASTEXITCODE) {
    exit $LASTEXITCODE
}

function copy_file($file_name) {
    $src = @(Get-ChildItem -Path "$dir\sourcecode\bin\$config" -Recurse -Filter $file_name | Select-Object -ExpandProperty FullName)[0]
    if (-not ($src -eq $null)) {
        $dst = Join-Path -Path "$dir\$mod_name\plugins" -ChildPath $file_name

        # Write-Host $src
        # Write-Host $dst

        New-Item -ItemType Directory -Path (Split-Path $dst) -Force | Out-Null
        Move-Item -Path $src -Destination $dst -Force
    }
}

copy_file($dll_name)
copy_file($pdb_name)

copy_file("profiler.dll")
copy_file("profiler.pdb")

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

