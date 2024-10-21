
$wd = Get-Location
Set-Location "c:\steam\steamapps\common\rain world\modding\sbcamerascroll\sourcecode"

$config = "Release"
dotnet build -c $config

$modName = "SBCameraScroll"
$dllName = $modName + ".dll"
$pdbName = $modName + ".pdb"

$sourcePath      = Get-ChildItem -Path ("bin\" + $config) -Recurse -Filter $dllName | Select-Object -ExpandProperty FullName
$destinationPath = Join-Path -Path ("..\" + $modName + "\plugins\") -ChildPath $dllName
Move-Item -Path $sourcePath -Destination $destinationPath -Force

$sourcePath      = Get-ChildItem -Path ("bin\" + $config) -Recurse -Filter $pdbName | Select-Object -ExpandProperty FullName
$destinationPath = Join-Path -Path ("..\" + $modName + "\plugins\") -ChildPath $pdbName
Move-Item -Path $sourcePath -Destination $destinationPath -Force

Set-Location $wd

