This is an offshoot of this mod I seen on nexus mods https://www.nexusmods.com/kingdomheartsfinalmix/mods/231. which is an alternative launcher that helps get past the infinite loading screen when selecting one of the games from the 1.5 + 2.5 game select screen, the original mod seems to be made specifically with AMD in mind but I've found that this works for Intel APUs as I was having this same issue when trying to play it through my MSI Claw 8AI+, so hopefully this also works for AMD as it's not something I've tested as I don't have AMD parts.

I have also looked to apply the same kind of thing for Kingdom Hearts 2.8 Final Chapter Prologue as I was having the same issues with Dream Drop Distance and 0.2 Birth by Sleep. The mod when I initially downloaded it didn't work, seemed to be hardcoded to look for specific folder paths and such, so with the access to the .cs file I decided to open it up, translate it to English and make what I think are improvements and simplifications with the help of Claude AI.

I have only tested this with the Epic Games Store version so it may not work with the Steam version.

# Kingdom Hearts 1.5 + 2.5 Instructions
1) Rename the "KINGDOM HEARTS HD 1.5+2.5 Launcher.exe" within the installation folder to something like "KINGDOM HEARTS HD 1.5+2.5 Launcher OG.exe" as a backup
2) Download the zip folder in releases in this repo
3) Drop it the Kingdom Hearts 1.5+2.5 Launcher.exe into the installation folder
4) Run Kingdom Hearts HD 1.5 + 2.5 ReMix from the Epic Games Launcher

Now when you run the game from the Epic Launcher it will open the alternative launcher. It has xinput capabilities so should read inputs from game controllers okay, it's worked well for me so far. 

# Kingdom Hearts 2.8 Final Chapter Prologue Instructions
1) Rename the "KINGDOM HEARTS HD 2.8 Launcher.exe" to something like "KINGDOM HEARTS HD 2.8 Launcher OG.exe" as a backup for your original launcher
2) Download the zip folder in releases
3) Open up the zip and copy the "KINGDOM HEARTS HD 2.8 Launcher.exe" and drop it into the installation folder
4) Run Kingdom Hearts HD 2.8 Final Chapter Prologue through the Epic Games Launcher

It will have the same kind of launcher style as the 1.5+2.5 launcher, if you want to launch Kingdom Hearts X Back Cover you will need to tell the alternative launcher where the original one is as there is no dedicated exe file for Kingdom Hearts X Back Cover so it just opens up the official launcher where you can open that movies up from there.

| Kingdom Hearts 1.5 + 2.5                                                                                                           | Kingdom Hearts 2.8 |
| ---                                                                                                                                | --- |
| <img width="737" height="580" alt="image" src="https://github.com/user-attachments/assets/453b82e4-d398-4baf-8537-48198d9d5d33" /> | <img width="735" height="601" alt="image" src="https://github.com/user-attachments/assets/b43a1d20-9c64-429b-b3ad-cc74a3207829" /> |

# Known issues:
- Kingdom Hearts 1.5 + 2.5
  - Occasionally when starting one of the games it might tell you to log into Epic, I've found just running it again it'll work, not sure why it does this and it doesn't bother me enough to try and fix it, I think it's some kind of race condition.
  - When at the menu if you select "Back" it might take you into the official launcher, it might also just hang in a loading screen, I would suggest using the "Quit Game" option or Alt+f4.
- Kingdom Hearts 2.8
  - When at the menu if you select "Back" it might take you into the official launcher, it might also just hang in a loading screen, I would suggest using the "Quit Game" option or Alt+f4.

I ran the exe through VirusTotal as well just to try and give some peace of mind to people, there were 66 runs and 1 flagged it as Malware, but that looks to be a false positive. The scan can be found here 
- KH1.5+2.5 - https://www.virustotal.com/gui/file/9194d15d9ab5616b925f561ab33b3497c7389f55511d0d468cd3aabf123a28f7
- KH2.8 - https://www.virustotal.com/gui/file/6d7a4735c29d00f3a0b2fb5a6f02a0b3a4b01bea3363f0b18dd978b6ade29fa2
