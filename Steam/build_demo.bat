rm ../Lemma/bin/x86/Release/*.xml
..\..\steamsdk\tools\ContentBuilder\builder\steamcmd.exe +login %1 %2 +run_app_build %~dp0%app_build_372580.vdf +quit
