# Quality Colors - RimWorld 1.6 Compatibility

## Changes Made for 1.6 Support

### 1. Updated About.xml
- Added "1.6" to the `supportedVersions` list in the main About.xml file

### 2. Created 1.6 Version Structure
- Copied the 1.5 source code to create a 1.6 version
- Updated the project file to reference RimWorld 1.6 assemblies

### 3. Project File Updates
- Updated `QualityColors.csproj` to use direct references to your actual RimWorld 1.6.4503 assemblies
- Updated `OutputPath` to compile directly into `1.6/Assemblies/` folder
- Replaced NuGet package references with direct DLL references:
  - `Assembly-CSharp.dll` from `RimWorldWin64_Data\Managed\`
  - `UnityEngine.CoreModule.dll`
  - `UnityEngine.IMGUIModule.dll`
  - `UnityEngine.TextRenderingModule.dll`
  - `0Harmony.dll` from the Harmony mod

### 4. AssemblyInfo.cs
- Ensured no BurstCompiler references are present (as recommended in the 1.6 documentation to prevent Visual Studio crashes)

## Compatibility Assessment

Based on the RimWorld 1.6 documentation, this mod should work without major issues because:

1. **No Breaking Changes**: The mod doesn't use any of the major breaking changes mentioned in the 1.6 documentation:
   - No designator system changes
   - No building system changes  
   - No pawn rendering system changes
   - No major UI system changes

2. **Harmony Compatibility**: The mod uses Harmony for patching, which continues to work in 1.6

3. **Quality System**: The quality system (`QualityCategory`, `CompQuality`, `QualityUtility`) remains unchanged

4. **UI Methods**: The patched methods (`TransferableUIUtility.DrawTransferableInfo`, `MainTabWindow_Inspect.GetLabel`) don't appear to be affected

## Potential Issues to Monitor

1. **Reflection Access**: The mod uses reflection to access private fields:
   - `GenLabel.labelDictionary`
   - `InspectPaneUtility.truncatedLabelsCached`
   
   While these aren't mentioned as changed in the documentation, internal implementation details could have changed.

2. **Testing Required**: The mod should be thoroughly tested in RimWorld 1.6 to ensure:
   - Quality coloring works in trade windows
   - Inspect pane coloring functions properly
   - Settings menu works correctly
   - No console errors appear

## Build Instructions

To compile the 1.6 version:

1. Open `1.6/Source/QualityColors/QualityColors.sln` in Visual Studio
2. Build in Release mode
3. The compiled DLL will be placed in `1.6/Assemblies/QualityColors.dll`

## Assembly References

The project now uses direct references to your actual RimWorld installation:
- **RimWorld Version**: 1.6.4503 rev209
- **Assembly-CSharp**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll`
- **UnityEngine Modules**: From the same Managed folder
- **Harmony**: From your Steam Workshop Harmony mod installation

## Notes

- The mod structure follows the same pattern as previous versions
- All source code is identical to the 1.5 version except for the project file references
- The mod should be backward compatible with 1.5 if needed
- The 1.6 version is completely self-contained in its own folder
- Uses your actual RimWorld installation instead of NuGet packages for better compatibility 