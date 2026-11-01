# Export notice preservation correction

Status: corrected package payloads verified. Original M2 source archive remains immutable and already contains the font license; the defect affected exported binary inclusion, not the source archive.

The exported APK had no notice asset, and the renamed font subset retained only copyright metadata. The supplied OFL text requires preserving copyright and license. Godot's official [license-compliance guidance](https://docs.godotengine.org/en/stable/about/complying_with_licenses.html) also documents engine/dependency notices. Delivery of the old APK was held; it had not reached the user.

Added explicit font and runtime notice inclusion to export filters, plus `content/licenses/` with verbatim official .NET runtime notices and actual Godot license/copyright API output. `scripts/check_notices.py` verifies exact notice hashes, rejects unmanifested/missing notice files, requires inclusion filters, and rejects signing/encryption settings in repository presets. A temporary experimental preset had inherited debug-signing fields; those were removed before any source/document artifact save, and the final static preset is sanitized. No private keystore was included in source or APK.

Fresh checks: core538761 assertions, core/host builds0warnings/errors,10GLB/20SVG validation, clean editor import and managed smoke passed. Independently rebuilt Linux export and actual standalone smoke passed; engine PCK probing matched all9 notice files plus manifest bytes. Android r2 matched all10 notice/manifest files byte-for-byte, APKv2 signature,16KiB ZIP alignment, gameplay/assembly/native payload and zero requested permissions checks passed. See `android-r2-apk-manifest.json` for exact98,214,387byte artifact and SHA256. Android device execution remains unrun.

Only notices, export metadata, documentation and validation utilities changed; C# gameplay/adapter source, gameplay JSON, models, UI assets and font bytes remain identical to the native-tested M2 source. This is an engineering packaging correction, not a blanket legal certification for future dependencies or a device-performance claim.
