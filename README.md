# Game Jam Kit

A reusable Unity Package Manager package containing utilities commonly used in game jams.

## Compatibility

- Unity 6.3 LTS or newer

## Installation

1. Open Unity Package Manager.
2. Select **Install package from git URL**.
3. Enter:

   `https://github.com/TrueVacuum/GameJamKit.git`

## Color palettes

1. Create a palette asset from **Assets > Create > Game Jam Kit > Color Palette**.
2. Add semantic color keys such as `Background`, `Primary`, `Text`, and `Danger`.
3. Add a **Color Palette Controller** component to a scene object and assign the palette.
4. Add a palette binder to a SpriteRenderer, Unity UI Graphic, or TextMesh Pro object.
5. Select the color key that the component should use.

Enable **Override Alpha** on an individual binder when it should use the palette color with
a different transparency. The **Alpha** slider only replaces the color's alpha channel;
its RGB values still come from the palette.

Palette keys are case-sensitive. Prefer semantic names that describe a color's role instead
of its appearance, so a key such as `Danger` can change from red to orange in another palette.

### Runtime switching and overrides

```csharp
using GameJamKit.Palettes;
using UnityEngine;

public sealed class PaletteExample : MonoBehaviour
{
    [SerializeField] private ColorPaletteController paletteController;
    [SerializeField] private ColorPalette alternatePalette;

    public void UseAlternatePalette()
    {
        paletteController.SetPalette(alternatePalette);
    }

    public void HighlightDanger()
    {
        paletteController.SetOverride("Danger", Color.yellow);
    }

    public void ClearDangerHighlight()
    {
        paletteController.RemoveOverride("Danger");
    }
}
```

## Display settings

1. Create a profile from **Assets > Create > Game Jam Kit > Display Settings Profile**.
2. Configure the default mode, minimum resolution, aspect-ratio filter, and optional
   windowed presets.
3. Add a **Display Settings Manager** to the scene and assign the profile.
4. Use `GetAvailableResolutions` to populate a custom menu, or add a
   **Display Settings Menu** and connect two TMP dropdowns plus Apply and Reset buttons.

Enable **Apply Immediately** on the menu when display-mode and resolution changes should be
applied and saved as soon as a dropdown changes. Leave it disabled to keep changes pending
until the player presses Apply.

The resolution list is built from the current display's supported resolutions. Duplicate
width/height entries and resolutions below the profile minimum are removed. Windowed mode
also includes the extra presets configured in the profile. By default, resolutions above
the current desktop size are hidden so GPU-driver virtual super resolutions do not appear;
disable **Limit To Desktop Resolution** in the profile if those modes should remain available.

```csharp
using GameJamKit.Display;
using UnityEngine;

public sealed class DisplayExample : MonoBehaviour
{
    [SerializeField] private DisplaySettingsManager displaySettings;

    public void UseWindowed720p()
    {
        displaySettings.Apply(new DisplaySettingsData(
            1280,
            720,
            FullScreenMode.Windowed));
    }
}
```

### Preserving the content aspect ratio

Enable **Preserve Content Aspect Ratio** in the display profile and set a target such as
`16 x 9`. Add an **Aspect Ratio Controller** to the scene, then assign the display settings
manager and the main camera. The camera viewport updates whenever the window size changes.

For uGUI, add a full-stretch **Aspect Ratio Letterbox** graphic to a Screen Space Overlay
canvas. Assign the controller and optionally a UI content root. The component draws the
required bars and restricts the content root to the visible viewport. Keep the letterbox
object outside the content root so it remains full-screen. When the root Canvas Scaler uses
**Scale With Screen Size** and **Match Width Or Height**, the letterbox automatically switches
between width matching and height matching as the window crosses the target aspect ratio.
The same constrained layout is previewed in Edit Mode using the current Game View dimensions.

## Audio settings

1. Create an Audio Mixer with exposed float parameters for the master, music, and sound-effects
   group volumes.
2. Create a profile from **Assets > Create > Game Jam Kit > Audio Settings Profile**.
3. Assign the mixer and enter the exposed parameter names. The defaults are `MasterVolume`,
   `MusicVolume`, and `SfxVolume`.
4. Add an **Audio Settings Manager** to the scene and assign the profile.
5. Connect an **Audio Settings Menu** to up to three sliders, a mute toggle, and optional Apply
   and Reset buttons.

Slider values use a linear `0-1` range and are converted to decibels before being sent to the
Audio Mixer. Missing UI controls are allowed, so a project can expose only the settings it needs.
Enable **Apply Immediately** when slider and toggle changes should be applied and saved without
an Apply button.

### Audio playback

Create an **Audio Playback Profile**, assign the Music and SFX mixer groups, then add an
**Audio Service** to a dedicated scene object. The service can optionally persist between scenes.

```csharp
using GameJamKit.Audio;
using UnityEngine;

public sealed class AudioExample : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip click;

    private void Start()
    {
        AudioService.Instance.PlayMusic(music);
    }

    public void PlayClick()
    {
        AudioService.Instance.PlaySfx(click);
    }
}
```

`PlayMusic` crossfades between two internal music sources. `PlaySfx` uses a reusable source
pool, while `PlaySfxAtPoint` configures a pooled source for 3D playback at a world position.
Clips are passed directly by the caller; the service does not require Resources or Addressables.

## Samples

Import **Game Settings Menu** from the package details in Unity Package Manager. The sample
contains a runnable scene, display and audio profiles, an Audio Mixer, and a palette asset.
Open `GameSettingsMenu.unity` to see the display, aspect-ratio, volume, and audio-service setup
working together. Imported samples are copied into the project's `Assets/Samples` folder and
can be modified without changing the installed package.
