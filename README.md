This is an offshoot of this mod I seen on nexus mods https://www.nexusmods.com/kingdomheartsfinalmix/mods/231. which is an alternative launcher that helps get past the infinite loading screen when selecting one of the games from the 1.5 + 2.5 game select screen, the original mod seems to be made specifically for AMD but I've found that this works for Intel APUs as I was having this same issue when trying to play it through my MSI Claw 8AI+, so hopefully this also works for AMD as it's not something I've tested as I don't have AMD parts.

The mod when I initially downloaded it didn't work, seemed to be hardcoded to look for specific folder paths and such, so with the access to the .cs file I decided to open it up, translate it to English and make what I think are improvements and simplifications with the help of Claude AI.

I have only tested this with the Epic Games Store version so it may not work with the Steam version.

# Instructions
1) Rename the "KINGDOM HEARTS HD 1.5+2.5 Launcher.exe" within the installation folder to something like "KINGDOM HEARTS HD 1.5+2.5 Launcher.exe.bak" as a backup
2) Download the exe in this repo
3) Drop it into the installation folder
4) Run Kingdom Hearts 1.5 + 2.5 HD ReMix from the Epic Games Launcher

Now when you run the game from the Epic Launcher it will open the alternative launcher. It has xinput capabilities so should read inputs from game controllers okay, it's worked well for me so far. 

# Known issues:
- Occasionally when starting the game it might tell you to log into Epic, I've found just running it again it'll work, not sure why it does this and it doesn't bother me enough to try and fix it, I think it's some kind of race condition.

I ran the exe through VirusTotal as well just to try and give some peace of mind to people, there were 66 runs and 1 flagged it as Malware, but that looks to be a false positive. The scan can be found here - https://www.virustotal.com/gui/file/9194d15d9ab5616b925f561ab33b3497c7389f55511d0d468cd3aabf123a28f7

<img width="737" height="580" alt="image" src="https://github.com/user-attachments/assets/453b82e4-d398-4baf-8537-48198d9d5d33" />
