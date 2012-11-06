; AutoIt script
; http://www.autoitscript.com

Const $AppTitle = "Lemma"
Const $MB_ICONERROR = 16

If RegRead("HKLM\Software\Microsoft\NET Framework Setup\NDP\v4\Client", "Install") <> 1 Then
	MsgBox($MB_ICONERROR, $AppTitle, ".NET 4.0 runtime is required. Hit OK to go to the download website.")
	Exit ShellExecute("http://www.microsoft.com/en-us/download/details.aspx?id=17851")
	Exit 1
EndIf

If RegRead("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\XNA\Framework\v4.0", "Installed") <> 1 Then
	MsgBox($MB_ICONERROR, $AppTitle, "XNA 4.0 runtime is required. Hit OK to go to the download website.")
	Exit ShellExecute("http://www.microsoft.com/en-us/download/details.aspx?id=20914")
EndIf

FileChangeDir("bin")

Exit RunWait("LemmaLauncher.exe")