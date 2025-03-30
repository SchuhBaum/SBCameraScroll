
$dir = $PSScriptRoot
$ErrorActionPreference = "Stop"

$config = "Release"
dotnet build (Join-Path -Path $dir -ChildPath "sourcecode") -c $config

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

