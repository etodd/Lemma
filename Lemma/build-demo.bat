rm bin/x86/Release/Content/Game/Images/*.xnb
rm bin/x86/Release/Content/Game/Models/*.xnb
rm bin/x86/Release/Content/Game/*.map
rm bin/x86/Release/Content/Game/*.xlsx
rm bin/x86/Release/Content/Game/*.dlz
rm bin/x86/Release/Content/Game/Challenge/*.map
rm bin/x86/Release/Content/Wwise/Windows/*.bnk

cp bin/x86/Debug/Content/Game/Images/material-propagation.xnb bin/x86/Release/Content/Game/Images
cp bin/x86/Debug/Content/Game/Images/wave-functions.xnb bin/x86/Release/Content/Game/Images

cp Game/rain.map bin/x86/Release/Content/Game
cp Game/rain.xlsx bin/x86/Release/Content/Game
cp Game/rain.dlz bin/x86/Release/Content/Game

cp Game/dawn.map bin/x86/Release/Content/Game
cp Game/dawn.xlsx bin/x86/Release/Content/Game
cp Game/dawn.dlz bin/x86/Release/Content/Game

cp Game/dawn2.map bin/x86/Release/Content/Game

cp "Game/Challenge/01 - Tunnel.map" bin/x86/Release/Content/Game/Challenge
cp "Game/Challenge/02 - Desert.map" bin/x86/Release/Content/Game/Challenge
cp "Game/Challenge/03 - Twilight.map" bin/x86/Release/Content/Game/Challenge
cp "Game/Challenge/04 - Gauntlet.map" bin/x86/Release/Content/Game/Challenge
cp "Game/Challenge/05 - Rubicon.map" bin/x86/Release/Content/Game/Challenge

cp Content/Wwise/Windows/Dark2.bnk bin/x86/Release/Content/Wwise/Windows
cp Content/Wwise/Windows/Dawn.bnk bin/x86/Release/Content/Wwise/Windows
cp Content/Wwise/Windows/Init.bnk bin/x86/Release/Content/Wwise/Windows
cp Content/Wwise/Windows/Rain.bnk bin/x86/Release/Content/Wwise/Windows
cp Content/Wwise/Windows/SFX_Bank_01.bnk bin/x86/Release/Content/Wwise/Windows
