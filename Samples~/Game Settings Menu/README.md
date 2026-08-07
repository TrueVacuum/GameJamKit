# Game Settings Menu Sample

Open `GameSettingsMenu.unity` and enter Play Mode to try the complete settings menu.

The sample includes:

- display-mode and resolution selection;
- optional immediate application of display changes;
- 16:9 camera and uGUI aspect-ratio preservation;
- master, music, and sound-effects volume settings;
- an optional mute toggle, hidden in the sample hierarchy by default;
- an Audio Mixer with Music and SFX groups;
- audio settings and playback profiles;
- CSV-driven English and Simplified Chinese localization;
- a shared Apply and Reset workflow.

The scene is intended as a working reference. Copy or adapt the objects and assets needed by
your own project. Menu UI references are optional, and the menu components can locate their
corresponding manager automatically when no manager is assigned.

## Fonts

This sample intentionally does not include third-party font files. Depending on the TMP font in
your project, Simplified Chinese may appear as square missing-glyph characters. Assign suitable
fonts in `DefaultLocalization`, or add the required fonts to the TMP font asset's fallback list.
The editor and development builds log a deduplicated warning when localized text contains missing
characters.
