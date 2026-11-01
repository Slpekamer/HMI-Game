# Runtime notices

These notices are distributed with exported packages, in addition to the renamed Noto subset's `content/fonts/LICENSE-Noto.txt`. They do not change the license or ownership of original HMI-Game code/art.

- Godot4.6.3.NET: `Godot-LICENSE.txt` and `Godot-THIRD-PARTY.json` were extracted from the actual editor's `Engine.get_license_text()`, `Engine.get_copyright_info()` and `Engine.get_license_info()` APIs. Official guidance: https://docs.godotengine.org/en/stable/about/complying_with_licenses.html and https://godotengine.org/license/.
- .NET/Mono Android10.0.14: verbatim `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT` from the official installed `Microsoft.NETCore.App.Runtime.Mono.android-arm64/10.0.14` pack.
- .NET Linux10.0.14: verbatim notices from official `microsoft.netcore.app.runtime.linux-x64/10.0.14` package.
- .NET Android bindings36.1.2: verbatim license from the official `Microsoft.Android.Runtime.36.android/36.1.2` pack.
- Noto font copyright and full OFL text are preserved in the existing font license file. The exported subset retains its original copyright metadata; a link alone is not substituted for the supplied license text.

If changing engine/runtime/target dependencies, refresh notices from the actual official packages and verify their bytes in the exported artifact. This is an engineering attribution record, not a blanket legal certification of future assets/dependencies.
