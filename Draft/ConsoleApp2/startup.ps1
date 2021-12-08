

$PSES_BUNDLE_PATH = "D:\PowerShellEditorServices"
$SESSION_TEMP_PATH  = "D:\pslog"

. $PSES_BUNDLE_PATH/PowerShellEditorServices/Start-EditorServices.ps1 `
	-BundledModulesPath $PSES_BUNDLE_PATH `
	-LogPath "$SESSION_TEMP_PATH/logs.log" `
	-SessionDetailsPath "$SESSION_TEMP_PATH/session.json" `
	-FeatureFlags @() `
	-AdditionalModules @() `
	-HostName 'My Client' `
	-HostProfileId 'myclient' `
	-HostVersion 1.0.0 `
	-LogLevel Verbose
	
	#-EnableConsoleRepl `
	#-LanguageServicePipeName "mytestlsppipe" `
	#-DebugServicePipeName "mytestlsppipedebug" `
	#-SplitInOutPipes `

	#-Stdio `

	#public enum PsesLogLevel
    #{
    #    Diagnostic = 0,
    #    Verbose = 1,
    #    Normal = 2,
    #    Warning = 3,
    #    Error = 4,
    #}
	#-LogLevel Verbose
	#-LogLevel Verbose
	#-LogLevel Normal

#$PSES_BUNDLE_PATH/PowerShellEditorServices/Start-EditorServices.ps1 
#	-BundledModulesPath $PSES_BUNDLE_PATH 
#	-LogPath $SESSION_TEMP_PATH/logs.log 
#	-SessionDetailsPath $SESSION_TEMP_PATH/session.json 
#	-FeatureFlags @() -AdditionalModules @() -HostName 'My Client' -HostProfileId 'myclient' -HostVersion 1.0.0 -Stdio -LogLevel Normal"
