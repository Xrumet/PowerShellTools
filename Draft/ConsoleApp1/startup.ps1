

write-host "Hello my friend"
write-host "lets rock&roll"



$PSES_BUNDLE_PATH = "D:\PowerShellEditorServices"
$SESSION_TEMP_PATH  = "D:\pslog"

. $PSES_BUNDLE_PATH/PowerShellEditorServices/Start-EditorServices.ps1 `
	-BundledModulesPath $PSES_BUNDLE_PATH `
	-LogPath $SESSION_TEMP_PATH/logs.log `
	-SessionDetailsPath $SESSION_TEMP_PATH/session.json `
	-FeatureFlags @() `
	-AdditionalModules @() `
	-HostName 'My Client' `
	-HostProfileId 'myclient' `
	-HostVersion 1.0.0 `
	-LogLevel Diagnostic

	#-LogLevel Normal


	write-host "That's it?'"