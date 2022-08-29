# Asu's Dolpatcher - v1.1.0
A tool to patch Nintendo Wii DOL executables using Riivolution XML files.

# Deprecation note
I'm no longer working on this tool, as a superior version has been made and implemented into my [RiivolutionIsoBuilder](https://github.com/Asu-chan/RiivolutionIsoBuilder). Please use it instead.

# Usage
"Asu's Dolpatcher.exe" \<DOL path\> \<Riivolution XML path\> \<Output DOL path\> [options]

"Asu's Dolpatcher.exe" [options]

"Asu's Dolpatcher.exe"

(Note: In the 2nd and 3rd cases, you will be asked for the file paths.)

# Options
--silent                           -\> Prevents from displaying any console outputs apart from the necessary ones

--always-create-sections           -\> Always create a new section if a target pointer is outside of the DOL range.

--never-create-sections            -\> Never create a new section if a target pointer is outside of the DOL range.

--binary-files-dir \<path\>      -\> Set the default directory for binary files

--only-patches "Patch1;Patch2;..." -\> Makes it so only the given patches will be used to patch the DOL.

--region <P/E/J/K/W>               -\> Uses the said region for files that requires a region name.

# Support
Need any help? Feel free to contact me on discord: <b>Asu-chan#2929</b>
