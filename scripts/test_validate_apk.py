#!/usr/bin/env python3
"""Synthetic verifier-policy tests; fixtures are not signed Android applications."""
import contextlib
import io
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import warnings
import zipfile

import validate_apk as gate

CERT = "ab" * 32
SIGNATURE = ("Verifies\nVerified using v2 scheme (APK Signature Scheme v2): true\n"
             "Number of signers: 1\nSigner #1 certificate SHA-256 digest: " + CERT + "\n")
ALIGNMENT = "Verifying alignment...\nVerification successful\n"


def elf(alignment=16384, machine=183, kind=1, offset=0, virtual=0):
    data = bytearray(120)
    data[:7] = b"\x7fELF\x02\x01\x01"
    struct.pack_into("<H", data, 16, 3)
    struct.pack_into("<I", data, 20, 1)
    struct.pack_into("<H", data, 52, 64)
    struct.pack_into("<H", data, 18, machine)
    struct.pack_into("<Q", data, 32, 64)
    struct.pack_into("<HH", data, 54, 56, 1)
    struct.pack_into("<IIQQQQQQ", data, 64, kind, 5, offset, virtual, 0, 120, 120, alignment)
    return data


class PackagingGateTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="hmi-apk-gates-")
        self.addCleanup(self.temp.cleanup)
        self.apk = Path(self.temp.name) / "synthetic.apk"
        self.write_apk()

    def write_apk(self, native=None, omit=(), extra=None):
        entries = {"lib/arm64-v8a/libgodot.so": bytes(elf()),
                   "lib/arm64-v8a/libmono.so": bytes(elf()),
                   "AndroidManifest.xml": b"synthetic manifest", "classes.dex": b"synthetic dex"}
        entries.update({"assets/managed/" + n: b"synthetic assembly" for n in gate.ASSEMBLIES})
        if native is not None:
            entries["lib/arm64-v8a/libgodot.so"] = bytes(native)
        entries.update(extra or {})
        with zipfile.ZipFile(self.apk, "w", compression=zipfile.ZIP_STORED) as archive:
            for name, data in entries.items():
                if name not in omit:
                    archive.writestr(name, data)

    def validate(self):
        return gate.validate(self.apk, "apksigner", "zipalign", CERT)

    def result(self, text, code=0):
        return subprocess.CompletedProcess([], code, stdout=text)

    def test_positive_policy_with_stubbed_official_tools(self):
        with patch.object(gate.subprocess, "run", side_effect=[self.result(SIGNATURE), self.result(ALIGNMENT)]) as run:
            report = self.validate()
        self.assertEqual(report["status"], "passed")
        self.assertEqual(report["sha256"], gate.sha256(self.apk))
        self.assertEqual(report["signer_certificate_sha256"], CERT)
        self.assertIn("device_install_or_execution", report["not_checked"])
        self.assertEqual(run.call_args_list[0].args[0],
                         ["apksigner", "verify", "--verbose", "--print-certs", str(self.apk)])
        self.assertEqual(run.call_args_list[1].args[0],
                         ["zipalign", "-c", "-P", "16", "-v", "4", str(self.apk)])
        self.assertFalse(run.call_args_list[0].kwargs.get("shell", False))

    def test_accepts_64k_compatible_elf(self):
        self.assertEqual(gate.check_elf(elf(65536), "sample"), [65536])

    def test_rejects_4k_elf_before_running_tools(self):
        self.write_apk(elf(4096))
        with patch.object(gate.subprocess, "run") as run, self.assertRaisesRegex(gate.ValidationError, "alignment 4096"):
            self.validate()
        run.assert_not_called()

    def test_rejects_non_power_of_two_alignment(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_elf(elf(24576), "sample")

    def test_rejects_offset_virtual_mismatch(self):
        with self.assertRaisesRegex(gate.ValidationError, "incongruent"):
            gate.check_elf(elf(virtual=4096), "sample")

    def test_rejects_wrong_machine(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_elf(elf(machine=62), "sample")

    def test_rejects_missing_load_segment(self):
        with self.assertRaisesRegex(gate.ValidationError, "missing PT_LOAD"):
            gate.check_elf(elf(kind=2), "sample")

    def test_rejects_truncated_headers(self):
        for size in [0, 5, 63, 80, 119]:
            with self.subTest(size=size), self.assertRaises(gate.ValidationError):
                gate.check_elf(elf()[:size], "sample")

    def test_rejects_invalid_header_fields(self):
        for offset, fmt, value in [(6, "B", 0), (16, "H", 2), (20, "I", 0), (52, "H", 0)]:
            data = elf()
            struct.pack_into("<" + fmt, data, offset, value)
            with self.subTest(offset=offset), self.assertRaises(gate.ValidationError):
                gate.check_elf(data, "sample")

    def test_rejects_virtual_address_overflow(self):
        data = elf(virtual=(1 << 64) - 16384)
        struct.pack_into("<Q", data, 64 + 40, 32768)
        with self.assertRaisesRegex(gate.ValidationError, "overflows"):
            gate.check_elf(data, "sample")

    def test_rejects_bad_program_header_size(self):
        data = elf()
        struct.pack_into("<H", data, 54, 48)
        with self.assertRaises(gate.ValidationError):
            gate.check_elf(data, "sample")

    def test_rejects_out_of_bounds_segment(self):
        data = elf()
        struct.pack_into("<QQ", data, 64 + 32, 121, 121)
        with self.assertRaises(gate.ValidationError):
            gate.check_elf(data, "sample")

    def test_rejects_file_segment_larger_than_memory(self):
        data = elf()
        struct.pack_into("<Q", data, 64 + 40, 119)
        with self.assertRaises(gate.ValidationError):
            gate.check_elf(data, "sample")

    def test_rejects_non_arm64_library(self):
        self.write_apk(extra={"lib/x86_64/extra.so": bytes(elf())})
        with self.assertRaisesRegex(gate.ValidationError, "only ARM64"):
            gate.check_payload(self.apk)

    def test_rejects_missing_runtime(self):
        self.write_apk(omit=["lib/arm64-v8a/libmono.so"])
        with self.assertRaisesRegex(gate.ValidationError, "Mono"):
            gate.check_payload(self.apk)

    def test_rejects_missing_assembly(self):
        self.write_apk(omit=["assets/managed/Hmi.Core.dll"])
        with self.assertRaisesRegex(gate.ValidationError, "Hmi.Core.dll"):
            gate.check_payload(self.apk)

    def test_rejects_empty_assembly(self):
        self.write_apk(extra={"assets/managed/HmiGame.dll": b""})
        with self.assertRaises(gate.ValidationError):
            gate.check_payload(self.apk)

    def test_rejects_ambiguous_assembly(self):
        self.write_apk(extra={"assets/other/HmiGame.dll": b"unexpected"})
        with self.assertRaises(gate.ValidationError):
            gate.check_payload(self.apk)

    def test_rejects_missing_android_payload(self):
        for name in ["AndroidManifest.xml", "classes.dex"]:
            self.write_apk(omit=[name])
            with self.subTest(name=name), self.assertRaises(gate.ValidationError):
                gate.check_payload(self.apk)

    def test_rejects_keystore(self):
        self.write_apk(extra={"assets/debug.KEYSTORE": b"not a real key"})
        with self.assertRaisesRegex(gate.ValidationError, "keystore"):
            gate.check_payload(self.apk)

    def test_rejects_duplicate_zip_names(self):
        with warnings.catch_warnings(), zipfile.ZipFile(self.apk, "a") as archive:
            warnings.simplefilter("ignore", UserWarning)
            archive.writestr("classes.dex", b"duplicate")
        with self.assertRaisesRegex(gate.ValidationError, "Duplicate"):
            gate.check_payload(self.apk)

    def test_rejects_crc_corruption(self):
        data = self.apk.read_bytes().replace(b"synthetic dex", b"corrupted dex", 1)
        self.apk.write_bytes(data)
        with self.assertRaises((gate.ValidationError, zipfile.BadZipFile)):
            gate.check_payload(self.apk)

    def test_signature_rejects_false_v2_even_with_matching_certificate(self):
        with self.assertRaisesRegex(gate.ValidationError, "explicitly true"):
            gate.check_signature(SIGNATURE.replace(": true", ": false"), CERT)

    def test_signature_requires_success_marker(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_signature(SIGNATURE.replace("Verifies\n", ""), CERT)

    def test_signature_rejects_explicit_failure_even_with_success_text(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_signature(SIGNATURE + "DOES NOT VERIFY\n", CERT)

    def test_signature_rejects_wrong_certificate(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_signature(SIGNATURE.replace(CERT, "cd" * 32), CERT)

    def test_signature_rejects_multiple_signers(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_signature(SIGNATURE.replace("signers: 1", "signers: 2"), CERT)

    def test_signature_rejects_conflicting_v2_lines(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_signature(SIGNATURE + "Verified using v2 scheme (APK Signature Scheme v2): false\n", CERT)

    def test_signature_rejects_duplicate_certificate_lines(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_signature(SIGNATURE + "Signer #1 certificate SHA-256 digest: " + CERT + "\n", CERT)

    def test_zipalign_rejects_failure(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_zipalign("Verification FAILED\n")

    def test_zipalign_rejects_conflicting_success_and_failure(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_zipalign(ALIGNMENT + "Verification FAILED\n")

    def test_zipalign_rejects_empty_output(self):
        with self.assertRaises(gate.ValidationError):
            gate.check_zipalign("")

    def test_nonzero_exit_overrides_success_text(self):
        for results in [[self.result(SIGNATURE, 1)], [self.result(SIGNATURE), self.result(ALIGNMENT, 1)]]:
            with self.subTest(results=len(results)), patch.object(gate.subprocess, "run", side_effect=results), self.assertRaisesRegex(gate.ValidationError, "failed with exit 1"):
                self.validate()

    def test_missing_tool_is_failure(self):
        with patch.object(gate.subprocess, "run", side_effect=FileNotFoundError("unavailable")), self.assertRaises(gate.ValidationError):
            self.validate()

    def test_timeout_is_failure(self):
        with patch.object(gate.subprocess, "run", side_effect=subprocess.TimeoutExpired("tool", 1)), self.assertRaises(gate.ValidationError):
            self.validate()

    def test_artifact_mutation_invalidates_results(self):
        def mutate(*args, **kwargs):
            with self.apk.open("ab") as stream:
                stream.write(b"changed")
            return self.result(SIGNATURE if args[0][0] == "apksigner" else ALIGNMENT)
        with patch.object(gate.subprocess, "run", side_effect=mutate), self.assertRaisesRegex(gate.ValidationError, "changed during"):
            self.validate()

    def test_fingerprint_validation(self):
        self.assertEqual(gate.normalize_certificate(":".join(["AB"] * 32)), CERT)
        for value in ["", "ab" * 31, "zz" * 32]:
            with self.subTest(value=value), self.assertRaises(gate.ValidationError):
                gate.normalize_certificate(value)

    def test_timeout_validation(self):
        for value in [-1, 0, float("inf"), float("nan")]:
            with self.subTest(value=value), self.assertRaises(gate.ValidationError):
                gate.validate(self.apk, "apksigner", "zipalign", CERT, value)

    def test_cli_failure_emits_no_success_json(self):
        self.write_apk(elf(4096))
        stdout, stderr = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            code = gate.main([str(self.apk), "--apksigner", "unused", "--zipalign", "unused", "--expected-certificate", CERT])
        self.assertEqual(code, 1)
        self.assertEqual(stdout.getvalue(), "")
        self.assertIn("alignment 4096", stderr.getvalue())

    def test_optimized_python_cannot_disable_gates(self):
        self.write_apk(elf(4096))
        result = subprocess.run([sys.executable, "-O", str(Path(gate.__file__)), str(self.apk),
                                 "--apksigner", "unused", "--zipalign", "unused", "--expected-certificate", CERT],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 1)
        self.assertEqual(result.stdout, "")
        self.assertIn("alignment 4096", result.stderr)


if __name__ == "__main__":
    unittest.main(verbosity=2)
