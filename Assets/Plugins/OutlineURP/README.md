# OutlineURP

Modularny plugin outline dla URP (Unity 6 / URP 17), oparty o `ScriptableRendererFeature`.

## Funkcje

- Stany: `Hover` i `Selected` (priorytet: `Selected > Hover`)
- Grupy: `Enemy`, `Pickup`, `Custom1`, `Custom2`
- Tryb zaslaniania:
  - `RespectDepth` - outline nie przechodzi przez sciany
  - `XRay` - outline widoczny przez sciany
- API do sterowania z kodu przez `OutlineController`
- Dziala w:
  - Render Graph (Unity 6 / URP 17)
  - Compatibility Mode

## Struktura pluginu

- `Assets/Plugins/OutlineURP/Runtime`
  - runtime API (`OutlineController`, `OutlineTarget`, `OutlineProfile`)
- `Assets/Plugins/OutlineURP/RenderFeature`
  - `OutlineRendererFeature`
- `Assets/Plugins/OutlineURP/Shaders`
  - shader maski i compositu
- `Assets/Plugins/OutlineURP/Runtime/Testing`
  - skrypty demo do szybkiego testu

## Instalacja w projekcie

1. Upewnij sie, ze projekt uzywa URP.
2. Otworz aktywny `Universal Renderer` (np. `Assets/Settings/PC_Renderer.asset`).
3. Dodaj `Outline Renderer Feature`.
4. Utworz profil:
   - `Create > Outline URP > Outline Profile`
5. Przypisz profil do `Outline Renderer Feature`.
6. Sprawdz shadery w feature:
   - `Mask Shader = Hidden/OutlineURP/Mask`
   - `Composite Shader = Hidden/OutlineURP/Composite`

## Jak dodac outline do obiektu

1. Dodaj `OutlineTarget` na root obiektu (enemy/pickup).
2. Ustaw `Group` (`Enemy` albo `Pickup`).
3. Zostaw `Include Children = true`, jesli renderer jest w dzieciach.
4. Steruj stanem:
   - `SetHovered(true/false)`
   - `SetSelected(true/false)`
   - albo globalnie przez `OutlineController`

Przy klikaniu myszka obiekt musi miec collider.

## Hover vs Selected

- `Hover`: stan chwilowy (np. pod kursorem)
- `Selected`: stan docelowy (wybrany target)
- Gdy obiekt ma oba stany naraz, renderuje sie styl `Selected`.

## Przypadek: enemy ma outline tylko jako target

To jest domyslna konfiguracja demo:

- `OutlineDemoInteractor`:
  - `Hover Enemies = false`
  - `Select Enemies = true`

Efekt: enemy dostaje outline tylko gdy jest zaznaczony.

## Szybki test (demo)

Najprostszy setup:

1. W scenie utworz obiekt `GameManager` (albo `OutlineDemo`).
2. Dodaj komponenty:
   - `OutlineDemoConfigurator`
   - `OutlineDemoInteractor`
   - `OutlineDemoHud`
3. W `OutlineDemoHud` ustaw pole `Interactor` na ten sam obiekt (`GameManager`).
4. W `OutlineDemoConfigurator`:
   - przypisz `Enemy Roots` (opcjonalnie),
   - albo zostaw puste i ustaw warstwe enemy na `Enemy` (auto-discovery).
5. Uruchom Play.

Sterowanie:

- `LMB`: select pod kursorem
- `Tab`: nastepny enemy
- `X`: `RespectDepth <-> XRay`
- `Esc`: wyczysc selected
- `C`: wyczysc wszystkie stany

## API (skrot)

```csharp
OutlineController.SetHovered(OutlineTarget target, bool hovered);
OutlineController.SetSelected(OutlineTarget target, bool selected);
OutlineController.SetHovered(GameObject root, bool hovered);
OutlineController.SetSelected(GameObject root, bool selected);
OutlineController.SetExclusiveSelected(OutlineTarget target);
OutlineController.SetOcclusionMode(OutlineOcclusionMode mode);
OutlineController.ClearOcclusionModeOverride();
OutlineController.ClearAllStates();
```

## Debug i troubleshooting

### 1) Brak outline

Sprawdz w HUD:

- `Targets in scene` powinno byc `> 0`
- `Active entries` powinno byc `> 0`
- `Mask RG record/exec` i `Composite RG record/exec` powinny rosnac

Jesli `Active entries = 0`:

- obiekt nie ma aktywnego stanu (`Hover`/`Selected`)
- obiekt nie ma `OutlineTarget`
- obiekt nie ma rendererow

Jesli passy dzialaja, ale dalej nic nie widac:

- upewnij sie, ze:
  - `Debug Show Mask = false`
  - `Debug Force Fullscreen Tint = false`

### 2) Sprawdzenie czy pass dziala

W `Outline Renderer Feature` wlacz:

- `Debug Force Fullscreen Tint` - powinien pojawic sie tint fullscreen

Jesli tint dziala, render pass jest poprawnie podpiety.

### 3) Sprawdzenie maski

Wlacz:

- `Debug Show Mask`

Powinienes zobaczyc maske obiektu. Jesli jej nie ma, problem jest w targetach/stanie.

### 4) Outline przez sciany

- Domyslnie: `RespectDepth`
- Zmien runtime:
  - `OutlineController.SetOcclusionMode(OutlineOcclusionMode.XRay)`

## Eksport jako unitypackage

1. Zaznacz `Assets/Plugins/OutlineURP`.
2. PPM -> `Export Package...`
3. Zostaw `Include dependencies`.
4. (Opcjonalnie) dodaj:
   - `Assets/Settings/OutlineProfile.asset`
   - scene demo

