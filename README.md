# VEILVOICE

# CRYPTOGRAPHIC ZERO-TRUST EXECUTABLE CONTRACT

# VERSION 4.0

本契約は、
VeilVoice の：

* 完成定義
* 実行定義
* 推論定義
* Virtual Audio 定義
* artifact provenance
* audit chain
* anti-fake verification

を規定する。

本契約は：

「artifact が存在する」

だけでは不十分とする。

本契約は：

```text id="uyx2v7"
artifact provenance
```

すなわち：

「そのartifactが、
真正な runtime execution から生成された」

ことを要求する。

---

# SECTION 0

# CORE PHILOSOPHY

## PRINCIPLE-001

LLMは信用されない。

## PRINCIPLE-002

開発者自己申告は信用されない。

## PRINCIPLE-003

artifact単体は信用されない。

## PRINCIPLE-004

artifact provenance を mandatory とする。

## PRINCIPLE-005

PASS判定主体と、
artifact生成主体を分離する。

## PRINCIPLE-006

Mock/Fake/Stub/Simulation は：

* source
* binary
* runtime
* validation
* compatibility

すべて禁止。

## PRINCIPLE-007

“validation only”
“simulation only”
“compatibility mode”
は禁止。

---

# SECTION 1

# PRODUCT DEFINITION

VeilVoice:
Beatrice engine による、
Windows 11 向け
local realtime voice conversion application。

---

# SECTION 2

# ALLOWED ENGINE

許可：

* Beatrice
* Beatrice JVS Corpus Edition
* Beatrice v2

禁止：

* RVC substitution
* OpenVoice substitution
* cloud inference
* prerecorded output
* DSP fake conversion
* compatibility simulation

---

# SECTION 3

# REAL EXECUTION REQUIREMENT

## TEST-REAL-001

Requirement:
真正な Beatrice inference mandatory。

真正 inference 定義：

Input Audio
↓
Tensor Input
↓
Real ONNX Session
↓
Tensor Output
↓
Generated Audio

が、
単一 execution trace 内で接続されること。

---

## REQUIRED ARTIFACTS

* raw_input.wav
* tensor_input_dump.bin
* onnx_session_trace.json
* tensor_output_dump.bin
* processed_output.wav
* inference_timing.csv
* runtime_execution_id.txt

---

## PASS条件

以下が全成立：

1.

ONNX session 実生成。

2.

real model loaded。

3.

tensor output generated。

4.

processed_output.wav が
同一 execution_id を持つ。

5.

runtime_execution_id が：

* input
* tensor
* output
* wav

全artifactで一致。

---

## FAIL条件

* prerecorded wav
* fake tensor
* simulated inference
* validation-only execution
* empty ONNX session
* bypass graph
* silent fallback
* compatibility-only mode

---

# SECTION 4

# ARTIFACT PROVENANCE CHAIN

## TEST-PROVENANCE-001

Requirement:
全artifact provenance mandatory。

各artifact必須：

* SHA256
* execution_id
* timestamp
* process_id
* machine_id
* git_commit

---

## PASS条件

artifact graph が：

```text id="b2u1pn"
raw_input.wav
→ tensor_input_dump.bin
→ ONNX session
→ tensor_output_dump.bin
→ processed_output.wav
```

として接続可能。

---

## FAIL条件

* orphan artifact
* provenance mismatch
* execution_id mismatch
* hash mismatch

---

# SECTION 5

# FORBIDDEN SYMBOLS

## TEST-BINARY-001

禁止：

* Mock
* Fake
* Stub
* Simulation
* ValidationOnly
* CompatibilityMode
* Dummy
* Bypass

---

## PASS条件

forbidden symbol count == 0

---

## FAIL条件

count > 0

---

# SECTION 6

# SOURCE INTEGRITY

## TEST-SOURCE-001

Requirement:
conditional bypass prohibited。

禁止：

```text id="khw6xv"
if (MOCK)
if (VALIDATION)
if (SIMULATION)
```

---

## FAIL条件

validation branch detected。

---

# SECTION 7

# VIRTUAL AUDIO DEFINITION

## TEST-VAUDIO-001

Requirement:
VeilVoiceOut endpoint mandatory。

---

## OPTION A

独自Virtual Audio Driver。

---

## OPTION B

署名済み third-party backend。

許可：

* Voicemeeter VAIO
* VB-CABLE

ただし mandatory：

* dependency disclosure
* backend identity disclosure
* endpoint provider disclosure

---

## REQUIRED ARTIFACTS

* endpoint_provider.json
* driver_stack_dump.txt
* endpoint_guid_report.json

---

## PASS条件

backend identity 明示。

---

## FAIL条件

* hidden dependency
* backend spoofing
* fake endpoint naming

---

# SECTION 8

# AUDIO GRAPH GUARANTEE

## TEST-GRAPH-001

Requirement:

```text id="6sv2yo"
Input PCM
→ Beatrice
→ VeilVoiceOut
```

のみ成立。

---

## REQUIRED ARTIFACTS

* audio_graph.json
* runtime_pipeline_trace.log

---

## FAIL条件

* raw passthrough
* bypass graph
* parallel raw route
* fallback raw route

---

# SECTION 9

# REAL AUDIO VALIDATION

## TEST-AUDIO-001

Requirement:
processed_output.wav が
真正推論由来。

---

## REQUIRED VALIDATION

* waveform delta
* spectral difference
* tensor-output correlation
* runtime timing consistency

---

## FAIL条件

* identical prerecorded pattern
* static waveform reuse
* silence substitution
* fake generated output

---

# SECTION 10

# DISCORD / VRCHAT

## TEST-DISCORD-001

Artifacts:

* discord_capture.png
* discord_meter_capture.png

---

## TEST-VRCHAT-001

Artifacts:

* vrchat_capture.png
* vrchat_audio_log.txt

---

# SECTION 11

# HOTSWAP VALIDATION

## TEST-HOTSWAP-001

Requirement:
real ONNX session hotswap。

---

## REQUIRED

runtime model switch mandatory。

---

## FAIL条件

* mock provider swap
* simulation-only hotswap
* empty runtime switch

---

# SECTION 12

# OFFLINE GUARANTEE

## TEST-OFFLINE-001

Requirement:
完全offline動作。

FAIL：

* cloud auth
* runtime API dependency

---

# SECTION 13

# LATENCY

## TEST-LATENCY-001

Requirement:
p95 < 150ms。

---

# SECTION 14

# LONGRUN

## TEST-STABILITY-001

Requirement:
3時間 stable。

---

# SECTION 15

# CRASH SAFETY

## TEST-CRASH-001

FAIL：

* zombie endpoint
* audio corruption

---

# SECTION 16

# DRIVER SIGNING

## TEST-DRIVER-001

Requirement:
署名済み。

---

# SECTION 17

# HASH RULES

全artifact：

SHA256 mandatory。

---

# SECTION 18

# PASS AUTHORITY

PASS可能主体：

* acceptance_runner.exe
* provenance verifier
* CI pipeline

LLMは禁止：

* PASS宣言
* completion宣言
* artifact生成
* manifest自己記述

---

# SECTION 19

# DELIVERY BLOCKERS

以下成立時：

```text id="v6j2rk"
DELIVERY BLOCKED
```

* FAIL >= 1
* UNVERIFIED >= 1
* provenance mismatch
* hidden dependency
* mock detected
* execution trace broken

---

# SECTION 20

# FINAL DELIVERY PACKAGE

必須：

* VeilVoiceInstaller.exe
* acceptance_runner.exe
* provenance_verifier.exe
* acceptance_report.html
* provenance_graph.json
* all hashes
* all logs
* all traces
* all execution IDs

---

# SECTION 21

# DEFINITION OF COMPLETE

以下すべて成立時のみ：

* FAIL == 0
* UNVERIFIED == 0
* provenance valid
* real inference verified
* real audio generation verified
* mock absent
* execution chain valid
* backend disclosed
* Discord verified
* VRChat verified

その時のみ：

```text id="z6m2wr"
VeilVoice 完成
```

と定義する。
