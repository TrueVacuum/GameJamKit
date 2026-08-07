# Changelog

All notable changes to this package will be documented in this file.

## [Unreleased]

## [0.3.0] - 2026-08-07

### Added

- Added CSV-driven runtime localization with multiple source-file merging and fallback locales.
- Added saved language selection and optional system-language detection.
- Added per-locale TMP fonts, a default font, and a language-menu font with fallback support.
- Added localized TMP text and a uGUI language-selection menu.
- Added localized display-mode labels to the display settings menu.
- Added a searchable key browser, translation previews, one-click key creation, scene scanning,
  batch binding, and CSV validation editor tools.
- Added a configurable global missing-translation marker.
- Added deduplicated development warnings for missing TMP font characters.
- Added localization parser, merge, fallback, and validation tests.

### Changed

- Expanded the Game Settings Menu sample with English and Simplified Chinese localization.
- Kept the sample font-independent; projects can assign their own TMP fonts and fallbacks.

## [0.2.0] - 2026-08-04

### Changed

- Expanded the package manifest metadata.

### Added

- Added a reusable color palette system with runtime overrides and change notifications.
- Added palette binders for SpriteRenderer, Unity UI, and TextMesh Pro.
- Added optional per-binder alpha override.
- Added editor tooling for palette validation and color-key selection.
- Added reusable display settings profiles, resolution filtering, persistence, and runtime application.
- Added a TMP/uGUI display settings menu component.
- Added optional filtering for driver-provided resolutions above the desktop size.
- Added camera viewport aspect-ratio preservation and a uGUI letterbox/content adapter.
- Added automatic Canvas Scaler matching for constrained aspect-ratio UI.
- Added reusable AudioMixer settings with master, music, sound-effects, and mute controls.
- Added an optional uGUI audio settings menu component.
- Added a lightweight Audio Service with music crossfading and pooled 2D/3D sound effects.
- Added optional immediate-apply behavior to the display and audio settings menus.
- Added safe Edit Mode preview updates for aspect-ratio-constrained uGUI layouts.
- Added an importable Game Settings Menu sample scene with profiles and an Audio Mixer setup.

## [0.1.0] - 2026-07-29

### Added

- Created the initial UPM package structure.
- Added the `GameJamKit.Runtime` assembly definition.
- Added the `GameJamKitInfo` utility.
- Added README documentation.
- Added the MIT License.
