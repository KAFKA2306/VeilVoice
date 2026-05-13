# VEILVOICE

# ZERO-TRUST EXECUTABLE DELIVERY CONTRACT

# VERSION 3.0

本契約は、
「VeilVoice」
の完成定義、検証定義、証拠定義、禁止事項、納入条件を規定する。

本契約は：

* 善意
* 自己申告
* コードレビュー
* スクリーンショット説明
* LLM説明

を信用しない。

本契約は：

「偽装耐性を持つ機械検証」

のみを信用する。

---

# SECTION 0

# CORE PHILOSOPHY

## PRINCIPLE-001

LLMは信用されない。

## PRINCIPLE-002

開発者自己申告は信用されない。

## PRINCIPLE-003

artifactを生成する主体は、
PASS判定権限を持たない。

## PRINCIPLE-004

PASS判定主体は、
artifactを書き換えられない。

## PRINCIPLE-005

「見た目上それっぽい」は無効。

## PRINCIPLE-006

自己申告manifestは禁止。

## PRINCIPLE-007

Mock/Fake/Stub/Simulationは、
本番系へ存在した時点でFAIL。

---

# SECTION 1

# PRODUCT DEFINITION

VeilVoice:
Beatrice inference engine を使用し、
Windows 11 上で
リアルタイム音声変換を行う
ローカルnative voice conversion application。

---

# SECTION 2

# MANDATORY ENGINE

許可：

* Beatrice
* Beatrice JVS Corpus Edition
* Beatrice v2

禁止：

* RVC substitution
* OpenVoice substitution
* cloud inference
* remote inference
* API fallback
* DSP-only fake conversion
* prerecorded playback

---

# SECTION 3

# TARGET ENVIRONMENT

OS:

* Windows 11 24H2 x64

Input:

* FIFINE USB microphone

Output:

* VeilVoiceOut

Apps:

* Discord Stable
* VRChat
* OBS Studio

---

# SECTION 4

# ZERO TRUST RULES

## RULE-001

artifact生成とPASS判定を分離する。

## RULE-002

artifact生成者は、
PASSを書き込めない。

## RULE-003

PASS判定者は、
artifact変更権限を持たない。

## RULE-004

すべてのartifactは：

* SHA256
* timestamp
* machine_id
* git_commit
* os_version

必須。

## RULE-005

hash不一致時、
即FAIL。

---

# SECTION 5

# FORBIDDEN IMPLEMENTATIONS

以下が存在した時点でFAIL：

* MockVeilVoiceProvider
* FakeInferenceProvider
* DummyInferenceProvider
* SimulationProvider
* modules.Add(...)
* manual manifest editing
* hardcoded PASS
* hardcoded module detection

---

# SECTION 6

# BINARY INTEGRITY GUARANTEE

## TEST-BINARY-001

Requirement:
本番バイナリにMock/Fake/Stub存在禁止。

Test Method:

```text id="pn1o6u"
binary_inspector.exe --scan forbidden_symbols
```

Forbidden Symbols:

* Mock
* Fake
* Stub
* Dummy
* Simulation

Artifacts:

* binary_symbol_report.json
* binary_hashes.sha256

PASS:

forbidden symbol count == 0

FAIL:

count > 0

---

# SECTION 7

# REAL ENGINE EXECUTION GUARANTEE

## TEST-ENGINE-001

Requirement:
Beatrice ONNX/Torch 推論実行。

禁止：
自己申告manifest。

Test Method:

```text id="wjlwmv"
acceptance_runner.exe --test real_inference
```

Artifacts:

* model_hash_manifest.json
* inference_session_log.txt
* tensor_input_dump.bin
* tensor_output_dump.bin
* inference_timing.csv
* processed_output.wav

PASS条件：

* ONNX session created
* model hash valid
* tensor output exists
* realtime inference executed

FAIL:

* model missing
* fake output
* static prerecorded output
* inference bypass

UNVERIFIED:

* session creation failed

---

# SECTION 8

# MODULE DETECTION GUARANTEE

## TEST-MODULE-001

Requirement:
loaded_modules.txt はOS列挙結果のみ。

禁止：
手動生成。

Test Method:

Windows API:

* EnumProcessModules
* ETW
* Process Explorer compatible dump

Artifacts:

* loaded_modules_raw.txt
* module_enumerator_log.txt

PASS:

* actual loaded modules present
* enumeration API verified

FAIL:

* manually generated file
* modules.Add detected

---

# SECTION 9

# OFFLINE GUARANTEE

## TEST-OFFLINE-001

Requirement:
完全offline動作。

Artifacts:

* firewall_trace.json
* network_access_log.txt
* offline_boot_log.txt

PASS:

* inference operational
* no outbound dependency

FAIL:

* cloud auth
* remote inference

---

# SECTION 10

# CPU REALTIME GUARANTEE

## TEST-CPU-001

Environment:
Ryzen 7 5800X

Requirement:
GPU無し realtime。

Artifacts:

* realtime_factor.json
* cpu_usage.csv

PASS:

* realtime_factor >= 1.0
* avg_cpu <= 30%

FAIL:

* GPU mandatory

---

# SECTION 11

# VIRTUAL AUDIO GUARANTEE

## TEST-VAUDIO-001

Requirement:
独自VeilVoiceOut endpoint。

禁止：

* VoiceMeeter dependency
* VB-CABLE dependency

Artifacts:

* endpoint_provider.json
* endpoint_guid_report.json
* driver_stack_dump.txt

PASS:

* endpoint stable
* GUID persistent

FAIL:

* external dependency mandatory

---

# SECTION 12

# AUDIO GRAPH GUARANTEE

## TEST-GRAPH-001

Requirement:

```text id="h92fpv"
Input PCM
→ Beatrice
→ VeilVoiceOut
```

のみ成立。

Artifacts:

* audio_graph.json
* pipeline_trace.log

FAIL:

* raw passthrough
* bypass route

---

# SECTION 13

# RAW VOICE LEAK PREVENTION

## TEST-PRIVACY-001

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

---

# SECTION 14

# DISCORD GUARANTEE

## TEST-DISCORD-001

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

# SECTION 15

# VRCHAT GUARANTEE

## TEST-VRCHAT-001

Artifacts:

* vrchat_capture.png
* vrchat_audio_log.txt

FAIL:

* mic unavailable

---

# SECTION 16

# LONGRUN STABILITY

## TEST-STABILITY-001

Requirement:
3時間動作。

Artifacts:

* memory_metrics.csv
* dropout_log.txt

FAIL:

* freeze
* leak
* unrecovered dropout

---

# SECTION 17

# CRASH SAFETY

## TEST-CRASH-001

Artifacts:

* crash_dump.dmp
* endpoint_post_crash.json

FAIL:

* zombie endpoint
* audio service corruption

---

# SECTION 18

# DRIVER SIGNING

## TEST-DRIVER-001

Artifacts:

* signtool_verify.txt

FAIL:

* unsigned driver
* test mode dependency

---

# SECTION 19

# CONFIG PERSISTENCE

## TEST-CONFIG-001

Artifacts:

* config_before.json
* config_after.json

FAIL:

* settings reset

---

# SECTION 20

# UNINSTALL SAFETY

## TEST-UNINSTALL-001

Artifacts:

* leftover_files.txt
* registry_diff.json

FAIL:

* orphan driver
* orphan endpoint

---

# SECTION 21

# ACCEPTANCE AUTHORITY

完成判定主体：

* acceptance_runner.exe
* CI pipeline
* binary inspector
* human artifact review

LLMは禁止：

* PASS宣言
* artifact生成
* manifest記述
* completion宣言

---

# SECTION 22

# DELIVERY BLOCKERS

以下成立時、
納入禁止：

* FAIL >= 1
* UNVERIFIED >= 1
* hash mismatch
* reproducibility failure
* missing artifact

---

# SECTION 23

# FINAL DELIVERY PACKAGE

納入物：

* VeilVoiceInstaller.exe
* acceptance_runner.exe
* binary_inspector.exe
* acceptance_report.html
* all artifacts
* all hashes
* all logs
* CI bundle
* reproducibility manifest
* model hash manifest

---

# SECTION 24

# DEFINITION OF COMPLETE

以下すべて成立時のみ：

* FAIL == 0
* UNVERIFIED == 0
* Beatrice verified
* realtime verified
* offline verified
* Discord verified
* VRChat verified
* raw leak absent
* artifact integrity valid
* binary integrity valid

その時のみ：

「VeilVoice 完成」

と定義する。
