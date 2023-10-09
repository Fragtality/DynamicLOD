# powershell -ExecutionPolicy Unrestricted -file "$(ProjectDir)CopyToMSFS.ps1" $(ConfigurationName)
$buildConfiguration = $args[0]

$projDir = "C:\Users\Fragtality\source\repos\DynamicLOD"
$bindir = $projDir + "\DynamicLOD\bin\Release\net7.0-windows"
$pubDir = $bindir
$destDir = "F:\MSFS2020\DynamicLOD"
$startFile = $true;

$makeZip = $true
$overrideZip = $true
$version = "latest"

if ($buildConfiguration -eq "Release") {
	Write-Host "Stop & Copy Binaries ..."
	Stop-Process -Name DynamicLOD -ErrorAction SilentlyContinue
	Sleep(3)
	Copy-Item -Path ($pubDir + "\*") -Destination $destDir -Recurse -Force -Exclude ("*.config","*.log")
	if ($startFile) {
		Write-Host "Starting Binary ..."
		Start-Process -FilePath ($destDir + "\DynamicLOD.exe") -WorkingDirectory $destDir
	}

		if ($makeZip) {
		Write-Host "Build Archive."
		$releaseDir = "C:\Users\Fragtality\source\repos\DynamicLOD\DynamicLOD\Releases"
		$pluginDir = "DynamicLOD"
		Copy-Item -Path ($bindir + "\DynamicLOD.exe") -Destination ($releaseDir + "\" + $pluginDir) -Recurse -Force
		Copy-Item -Path ($bindir + "\DynamicLOD.dll.config") -Destination ($releaseDir + "\" + $pluginDir) -Recurse -Force
		Copy-Item -Path ($bindir + "\*.dll") -Destination ($releaseDir + "\" + $pluginDir) -Recurse -Force
		Copy-Item -Path ($bindir + "\*.json") -Destination ($releaseDir + "\" + $pluginDir) -Recurse -Force
		$workDir = ($releaseDir + "\" + $pluginDir)
		$zipFile = ("DynamicLOD-" + $version + ".zip")

		if ($overrideZip -or -not(Test-Path -Path ($releaseDir + "\" + $zipFile))) {
			Write-Host "Zipping Binaries ..."
			& "C:\Program Files\7-Zip\7z.exe" a -tzip ($releaseDir + "\" + $zipFile) $workDir | Out-Null
			Write-Host "Creating Release File for $version ..."
			if ($version -eq "latest") {
				Write-Host "Copy latest File .."
				Copy-Item -Path ($releaseDir + "\" + $zipFile)  -Destination "C:\Users\Fragtality\source\repos\DynamicLOD\"
			}
		}
	}
}

exit 0
