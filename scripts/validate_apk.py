#!/usr/bin/env python3
"""Fail-closed packaging gates for the ARM64 HMI APK; never a device-run test.

Requires official Android SDK apksigner and zipalign. Tools run against the APK
in this invocation; saved logs are deliberately not accepted as verification.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import subprocess
import sys
import zipfile

PAGE_SIZE = 16 * 1024
ASSEMBLIES = ("HmiGame.dll", "Hmi.Core.dll", "Hmi.Persistence.dll")
# Exact loadable runtimes in the pinned Godot 4.6.3 .NET ARM64 export.
# Optional libmono-component-* libraries cannot replace the Mono VM.
NATIVE_RUNTIMES = ("libgodot_android.so", "libmonosgen-2.0.so")


class ValidationError(Exception):
    """The artifact did not establish every requested packaging gate."""


def require(condition: bool, message: str) -> None:
    # Do not use assert: these checks must also run under python -O.
    if not condition:
        raise ValidationError(message)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def normalize_certificate(value: str) -> str:
    value = value.strip().lower().replace(":", "")
    require(re.fullmatch(r"[0-9a-f]{64}", value) is not None,
            "Expected signer certificate must be a SHA-256 fingerprint")
    return value


def check_elf(data: bytes, name: str) -> list[int]:
    require(len(data) >= 64 and data[:6] == b"\x7fELF\x02\x01",
            f"{name}: expected a complete little-endian ELF64 header")
    require(data[6] == 1 and struct.unpack_from("<I", data, 20)[0] == 1
            and struct.unpack_from("<H", data, 16)[0] == 3
            and struct.unpack_from("<H", data, 52)[0] == 64,
            f"{name}: expected current-version ET_DYN with a 64-byte ELF header")
    require(struct.unpack_from("<H", data, 18)[0] == 183,
            f"{name}: expected AArch64 machine type")
    offset = struct.unpack_from("<Q", data, 32)[0]
    size, count = struct.unpack_from("<HH", data, 54)
    require(size == 56 and 0 < count < 65535 and offset >= 64
            and offset + size * count <= len(data),
            f"{name}: invalid or truncated program-header table")
    alignments = []
    for index in range(count):
        kind, _, file_offset, virtual, _, file_size, memory_size, alignment = (
            struct.unpack_from("<IIQQQQQQ", data, offset + index * size))
        if kind != 1:  # PT_LOAD
            continue
        require(alignment >= PAGE_SIZE and alignment & (alignment - 1) == 0,
                f"{name}: PT_LOAD alignment {alignment} is not 16 KiB compatible")
        require(file_offset % alignment == virtual % alignment,
                f"{name}: incongruent PT_LOAD offset and virtual address")
        require(memory_size <= (1 << 64) - 1 - virtual,
                f"{name}: PT_LOAD virtual address range overflows ELF64")
        require(file_size <= memory_size and file_offset + file_size <= len(data),
                f"{name}: invalid or truncated PT_LOAD segment")
        alignments.append(alignment)
    require(bool(alignments), f"{name}: missing PT_LOAD segments")
    return alignments


def check_payload(apk: Path) -> dict:
    with zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
        require(len(names) == len(set(names)), "Duplicate APK ZIP entries")
        require(archive.testzip() is None, "APK ZIP CRC verification failed")
        libraries = [n for n in names if n.startswith("lib/") and n.endswith(".so")]
        require(bool(libraries), "Missing native runtime libraries")
        require(all(n.startswith("lib/arm64-v8a/") and n.count("/") == 2
                    for n in libraries), "Expected only ARM64 native libraries")
        for runtime in NATIVE_RUNTIMES:
            require("lib/arm64-v8a/" + runtime in libraries,
                    f"Missing required native runtime: {runtime}")
        elf = {name: check_elf(archive.read(name), name) for name in sorted(libraries)}
        assemblies = []
        for name in ASSEMBLIES:
            matches = [n for n in names if n.startswith("assets/")
                       and n.endswith("/" + name)]
            require(len(matches) == 1 and archive.getinfo(matches[0]).file_size > 0,
                    f"Expected one nonempty {name} in APK assets")
            assemblies.append(matches[0])
        require("AndroidManifest.xml" in names and "classes.dex" in names,
                "Missing Android manifest or primary DEX payload")
        require(not any(n.lower().endswith((".jks", ".keystore")) for n in names),
                "Signing keystore must not be packaged")
    return {"zip_crc": "passed", "elf_load_alignments": elf,
            "required_managed_assemblies": assemblies}


def run_tool(executable: str, arguments: list[str], label: str, timeout: float) -> str:
    try:
        result = subprocess.run([executable, *arguments], stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True,
                                encoding="utf-8", errors="replace", timeout=timeout,
                                check=False)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise ValidationError(f"{label} could not complete: {error}") from error
    require(result.returncode == 0,
            f"{label} failed with exit {result.returncode}: {result.stdout.strip()}")
    return result.stdout


def check_signature(output: str, expected_certificate: str) -> str:
    require(re.search(r"(?mi)^\s*DOES NOT VERIFY\b", output) is None,
            "apksigner reported that the APK does not verify")
    require(re.search(r"(?m)^Verifies\s*$", output) is not None,
            "Missing apksigner verification success marker")
    v2 = re.findall(r"(?m)^Verified using v2 scheme \(APK Signature Scheme v2\): (true|false)\s*$", output)
    require(v2 == ["true"], "APK v2 signature verification must be explicitly true")
    counts = re.findall(r"(?m)^Number of signers: (\d+)\s*$", output)
    require(counts == ["1"], "Expected exactly one verified signer")
    certificates = re.findall(r"(?m)^Signer #(\d+) certificate SHA-256 digest: ([0-9a-fA-F]{64})\s*$", output)
    require(len(certificates) == 1 and certificates[0][0] == "1"
            and certificates[0][1].lower() == expected_certificate,
            "Verified signer does not match the expected certificate")
    return certificates[0][1].lower()


def check_zipalign(output: str) -> None:
    require(re.search(r"(?mi)^\s*Verification\s+FAILED\b", output) is None,
            "zipalign reported failed alignment")
    require(re.findall(r"(?m)^(Verification successful)\s*$", output)
            == ["Verification successful"], "Missing zipalign verification success marker")


def validate(apk: Path, apksigner: str, zipalign: str,
             expected_certificate: str, timeout: float = 120) -> dict:
    require(timeout > 0 and timeout < float("inf"), "Tool timeout must be finite and positive")
    certificate = normalize_certificate(expected_certificate)
    apk = apk.resolve(strict=True)
    require(apk.is_file(), "APK path must be a regular file")
    before = sha256(apk)
    payload = check_payload(apk)
    signature_output = run_tool(apksigner, ["verify", "--verbose", "--print-certs", str(apk)],
                                "apksigner", timeout)
    signer = check_signature(signature_output, certificate)
    alignment_output = run_tool(zipalign, ["-c", "-P", "16", "-v", "4", str(apk)],
                                "zipalign", timeout)
    check_zipalign(alignment_output)
    require(sha256(apk) == before, "APK changed during verification; discard this result")
    return {"status": "passed", "apk_name": apk.name, "bytes": apk.stat().st_size,
            "sha256": before, "signer_certificate_sha256": signer,
            "apk_v2_signature": "passed", "zip_alignment_16k": "passed", **payload,
            "not_checked": ["device_install_or_execution", "package_metadata",
                            "source_or_publish_byte_match", "gameplay_or_performance"]}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", type=Path)
    parser.add_argument("--apksigner", required=True, help="Official Android SDK apksigner executable")
    parser.add_argument("--zipalign", required=True, help="Official Android SDK zipalign executable")
    parser.add_argument("--expected-certificate", required=True, help="Trusted expected signer SHA-256")
    parser.add_argument("--tool-timeout", type=float, default=120)
    args = parser.parse_args(argv)
    try:
        report = validate(args.apk, args.apksigner, args.zipalign,
                          args.expected_certificate, args.tool_timeout)
    except (ValidationError, OSError, ValueError, zipfile.BadZipFile, RuntimeError) as error:
        print(f"FAIL APK packaging validation: {error}", file=sys.stderr)
        return 1
    print(json.dumps(report, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
