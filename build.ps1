param(
	[int]$buildNumber = 0,
	[bool]$skipRestore = $false
	)

Import-Module .\scripts\psake.psm1
Invoke-Psake .\scripts\default.ps1 default -framework "4.0" -properties @{ buildNumber=$buildNumber }
Remove-Module psake