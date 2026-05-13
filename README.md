# VEILVOICE

# BEATRICE-LOCKED EXECUTABLE DELIVERY & ACCEPTANCE CONTRACT

# VERSION 2.0

本契約は、
「Beatrice を使用したローカルリアルタイムAIボイスチェンジャー VeilVoice」
の完成定義、納入条件、検証条件、禁止事項を規定する。

本契約は：

* 仕様書
* 宣伝文
* 設計意図

ではない。

本契約は：

「機械検証可能な完成保証契約」

である。

---

# SECTION 0

# DEFINITIONS

## PRODUCT

VeilVoice:
Beatrice inference engine を利用し、
Windows 11 上でリアルタイム音声変換を行う
ローカルネイティブアプリケーション。

## MANDATORY ENGINE

使用可能 engine：

* Beatrice
* Beatrice JVS Corpus Edition
* Beatrice v2

禁止：

* RVC substitution
* OpenVoice substitution
* so-vits-svc substitution
* cloud inference substitution
* external realtime API substitution

## TARGET PLATFORM

* Windows 11 24H2
* x64

## TARGET INPUT DEVICE

* FIFINE USB microphone

## TARGET OUTPUT DEVICE

* VeilVoiceOut

## TARGET APPLICATIONS

* Discord Stable
* VRChat
* OBS Studio

---

# SECTION 1

# CONTRACT PRINCIPLES

## PRINCIPLE-001

LLM、人間、開発者の発言は、
完成証拠として無効。

## PRINCIPLE-002

以下のみ有効証拠：

* acceptance_runner.exe
* CI results
* generated artifacts
* signed logs
* endpoint enumeration
* wav recordings
* binary hash manifests
* process module dumps

## PRINCIPLE-003

証拠欠落時、
必ず UNVERIFIED。

## PRINCIPLE-004

PASSは：

* reproducible
* hashed
* machine generated

であること。

## PRINCIPLE-005

以下は禁止：

* “ほぼ完成”
* “動くはず”
* “コードレビュー済み”
* “理論上OK”
* “再現できないがPASS”

---

# SECTION 2

# REQUIRED DELIVERY COMPONENTS

納入対象：

* VeilVoiceInstaller.exe
* VeilVoice.exe
* VeilVoice virtual audio driver
* acceptance_runner.exe
* uninstall utility
* crash recovery subsystem
* realtime logging subsystem
* CI verification pipeline
* reproducibility manifest

---

# SECTION 3

# ENGINE IDENTITY GUARANTEE

## TEST-ENGINE-001

Requirement:
VeilVoice が Beatrice engine を使用すること。

Artifacts:

* engine_manifest.json
* loaded_modules.txt
* inference_backend_log.txt
* process_memory_map.txt

PASS:

engine_manifest.json:

```json id="4apxwl"
{
  "engine": "Beatrice",
  "execution_mode": "local_realtime",
  "runtime": "native"
}
```

loaded_modules.txt に：

* beatrice
* onnxruntime
* torch

のいずれか存在。

FAIL:

* RVC detected
* OpenVoice detected
* external inference API detected
* remote inference detected

UNVERIFIED:

* module dump missing
* process inspection failed

---

# SECTION 4

# OFFLINE GUARANTEE

## TEST-OFFLINE-001

Requirement:
インターネット接続無しで動作可能。

Test Method:
NIC disabled environment.

Artifacts:

* offline_boot_log.txt
* network_access_log.txt
* firewall_trace.json

PASS:

* inference operational offline
* no auth dependency
* no cloud endpoint required

FAIL:

* startup blocked
* cloud auth required
* runtime API dependency exists

---

# SECTION 5

# CPU REALTIME GUARANTEE

## TEST-CPU-001

Requirement:
GPU無し realtime inference 可能。

Environment:
Ryzen 7 5800X baseline.

Artifacts:

* inference_timing.csv
* realtime_factor.json
* cpu_usage.csv

PASS:

* realtime_factor >= 1.0
* avg_cpu <= 30%
* no GPU required

FAIL:

* GPU mandatory
* realtime impossible

---

# SECTION 6

# MODEL INTEGRITY GUARANTEE

## TEST-MODEL-001

Requirement:
使用モデル固定。

Artifacts:

* model_hash_manifest.json
* loaded_model_log.txt

PASS:

SHA256一致。

FAIL:

* unknown model
* dynamic redownload
* model replacement

---

# SECTION 7

# BOOTSTRAP GUARANTEE

## TEST-BOOTSTRAP-001

Requirement:
Windows初心者が15分以内導入可能。

禁止：

* VoiceMeeter要求
* VB-CABLE要求
* REAPER要求
* PowerShell要求
* manual routing

Artifacts:

* install_duration.json
* bootstrap_capture.mp4
* endpoint_after_install.json

PASS:

* installer only
* auto routing success
* VeilVoiceOut available

FAIL:

* reboot required
* manual routing required

---

# SECTION 8

# INPUT DEVICE GUARANTEE

## TEST-INPUT-001

Requirement:
FIFINE mic 自動認識。

Artifacts:

* input_endpoint_list.json
* selected_input_device.json

PASS:

selected_input_device.json:

```json id="pyr2p2"
{
  "device_name": "FIFINE ..."
}
```

FAIL:

* unknown mic selected
* manual device selection required

---

# SECTION 9

# OUTPUT DEVICE GUARANTEE

## TEST-VMIC-001

Requirement:
VeilVoiceOut endpoint 生成。

Artifacts:

* virtual_endpoint_list.json
* veilvoiceout_guid.txt
* windows_recording_devices.png

PASS:

endpoint visible.

FAIL:

* endpoint absent
* VB-CABLE visible
* VoiceMeeter visible

---

# SECTION 10

# DISCORD GUARANTEE

## TEST-DISCORD-001

Requirement:
Discord Stable compatible.

Artifacts:

* discord_capture.png
* discord_meter_capture.png

PASS:

* VeilVoiceOut selectable
* meter active

FAIL:

* restart required
* endpoint invisible

---

# SECTION 11

# VRCHAT GUARANTEE

## TEST-VRCHAT-001

Requirement:
VRChat compatible.

Artifacts:

* vrchat_capture.png
* vrchat_audio_log.txt

FAIL:

* mic unavailable
* crash occurs

---

# SECTION 12

# RAW VOICE LEAK PREVENTION

## TEST-PRIVACY-001

Requirement:
生声流出禁止。

Artifacts:

* raw_input.wav
* processed_output.wav
* muted_output.wav
* correlation_metrics.json

PASS:

* processed voice only
* mute outputs silence

FAIL:

* raw mic audible
* bypass routing active

---

# SECTION 13

# LATENCY GUARANTEE

## TEST-LATENCY-001

Requirement:
p95 latency < 150ms

Artifacts:

* latency_metrics.json
* waveform_alignment.png

FAIL:

* p95 >= 150ms

---

# SECTION 14

# LONGRUN STABILITY

## TEST-STABILITY-001

Requirement:
3時間安定動作。

Artifacts:

* cpu_metrics.csv
* memory_metrics.csv
* dropout_log.txt

FAIL:

* freeze
* memory leak
* unrecovered dropout

---

# SECTION 15

# HOTPLUG GUARANTEE

## TEST-HOTPLUG-001

Requirement:
USB抜き差し復旧 <= 5s

Artifacts:

* reconnect_log.txt
* endpoint_before_after.json

FAIL:

* restart required
* endpoint lost

---

# SECTION 16

# CRASH SAFETY

## TEST-CRASH-001

Requirement:
異常終了後もWindows音声環境維持。

Artifacts:

* crash_dump.dmp
* endpoint_post_crash.json

FAIL:

* zombie endpoint
* audio service corruption

---

# SECTION 17

# DRIVER SIGNING GUARANTEE

## TEST-DRIVER-001

Requirement:
Microsoft署名済みdriver。

Artifacts:

* signtool_verify.txt
* driver_signature_report.txt

FAIL:

* unsigned driver
* test signing dependency

---

# SECTION 18

# CONFIG PERSISTENCE

## TEST-CONFIG-001

Requirement:
再起動後設定維持。

Artifacts:

* config_before.json
* config_after.json

FAIL:

* settings reset

---

# SECTION 19

# UNINSTALL SAFETY

## TEST-UNINSTALL-001

Requirement:
完全アンインストール。

Artifacts:

* uninstall_log.txt
* leftover_files.txt
* registry_diff.json

FAIL:

* orphan driver
* orphan service
* zombie endpoint

---

# SECTION 20

# ARTIFACT INTEGRITY

全artifact必須：

* SHA256
* timestamp
* machine_id
* git_commit
* app_version
* os_version

---

# SECTION 21

# ACCEPTANCE AUTHORITY

完成判定主体：

* acceptance_runner.exe
* CI pipeline
* human artifact review

LLMは禁止：

* PASS宣言
* completion宣言
* artifact生成偽装

---

# SECTION 22

# DELIVERY BLOCKERS

以下成立時、
納入禁止：

* FAIL >= 1
* UNVERIFIED >= 1
* missing artifact
* hash mismatch
* unreproducible execution

---

# SECTION 23

# FINAL DELIVERY PACKAGE

最終納入物：

* VeilVoiceInstaller.exe
* acceptance_runner.exe
* acceptance_report.html
* artifacts/*
* logs/*
* hashes.sha256
* CI bundle
* endpoint GUID report
* crash bundle
* reproducibility manifest
* model hash manifest

---

# SECTION 24

# DEFINITION OF COMPLETE

以下成立時のみ：

* FAIL == 0
* UNVERIFIED == 0
* Beatrice verified
* offline verified
* realtime verified
* Discord verified
* VRChat verified
* raw leak absent
* artifact integrity valid

その時のみ：

「VeilVoice 完成」

と定義する。
