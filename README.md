# VEILVOICE

# FINAL ZERO-TRUST DELIVERY CONTRACT

# VERSION 5.0

本契約は、
VeilVoice の：

* 完成定義
* runtime定義
* Beatrice integration定義
* provenance定義
* virtual audio定義
* realtime stability定義
* security定義
* anti-fake verification定義

を規定する。

本契約は：

* 善意
* 自己申告
* completion claim
* “動くはず”
* “production ready”

を信用しない。

本契約は：

```text id="v3mf0h"
reproducible runtime evidence
```

のみを信用する。

---

# SECTION 0

# CORE PRINCIPLES

## PRINCIPLE-001

LLMは信用されない。

## PRINCIPLE-002

artifact単体は信用されない。

## PRINCIPLE-003

artifact provenance mandatory。

## PRINCIPLE-004

PASS判定とartifact生成を分離。

## PRINCIPLE-005

FAILを隠す行為は禁止。

## PRINCIPLE-006

UNVERIFIEDをPASSへ昇格禁止。

## PRINCIPLE-007

runtime未実行状態での
completion claim禁止。

---

# SECTION 1

# PRODUCT DEFINITION

VeilVoice:
Beatrice engine を利用し、
Windows 11 上で realtime voice conversion を行う
local native application。

---

# SECTION 2

# ENGINE REQUIREMENTS

## ALLOWED ENGINE

許可：

* Beatrice
* Beatrice JVS Corpus Edition
* Beatrice v2

禁止：

* RVC substitution
* OpenVoice substitution
* cloud inference
* prerecorded output
* compatibility simulation
* fake inference

---

# SECTION 3

# MODEL FORMAT REQUIREMENTS

## ALLOWED MODEL FORMATS

許可：

* official .bin
* official .toml
* official VST3 runtime
* official native runtime
* officially compatible ONNX backend

---

## NOT REQUIRED

`.onnx mandatory` を禁止。

---

# SECTION 4

# BACKEND DECLARATION

## REQUIRED FIELD

```json id="fd3sq7"
{
  "backend_type": "official_vst3"
}
```

---

## ALLOWED BACKENDS

* official_vst3
* official_native
* onnx_backend

---

## FORBIDDEN

* hidden backend
* spoofed backend
* undeclared dependency

---

# SECTION 5

# REAL EXECUTION REQUIREMENTS

## TEST-REAL-001

Requirement:
真正 runtime execution mandatory。

真正 execution 定義：

```text id="l4h8v6"
Input Audio
→ Official Beatrice Runtime
→ Official Model
→ Tensor Generation
→ Audio Generation
→ VeilVoiceOut
```

が単一 execution trace に存在。

---

## REQUIRED ARTIFACTS

* raw_input.wav
* runtime_execution_id.txt
* backend_runtime_trace.json
* tensor_input_dump.bin
* tensor_output_dump.bin
* processed_output.wav
* inference_timing.csv
* audio_callback_trace.log

---

## PASS条件

以下全成立：

1.

official runtime loaded。

2.

official model loaded。

3.

real tensor output generated。

4.

real generated audio exists。

5.

execution_id一致。

6.

callback starvation absent。

---

## FAIL条件

* simulation mode
* compatibility-only mode
* prerecorded output
* fake tensor
* bypass inference
* silent fallback

---

# SECTION 6

# PROVENANCE CHAIN

## TEST-PROVENANCE-001

Requirement:
全artifact provenance mandatory。

---

## REQUIRED METADATA

* SHA256
* execution_id
* timestamp
* process_id
* thread_id
* git_commit
* machine_id

---

## PASS条件

以下trace成立：

```text id="a3j9de"
raw_input.wav
→ tensor_input_dump.bin
→ runtime execution
→ tensor_output_dump.bin
→ processed_output.wav
→ VeilVoiceOut
```

---

## FAIL条件

* provenance mismatch
* stale artifact reuse
* execution_id collision
* orphan artifact
* replayed wav

---

# SECTION 7

# FORBIDDEN SYMBOL DETECTION

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

## FAIL条件

forbidden symbol detected。

---

# SECTION 8

# SOURCE INTEGRITY

## TEST-SOURCE-001

禁止：

```text id="78sh98"
if (MOCK)
if (VALIDATION)
if (SIMULATION)
```

---

## FAIL条件

validation branch exists。

---

# SECTION 9

# VIRTUAL AUDIO REQUIREMENTS

## TEST-VAUDIO-001

Requirement:
VeilVoiceOut endpoint mandatory。

---

## REQUIRED DISCLOSURE

* backend identity
* dependency identity
* routing topology

---

## REQUIRED ARTIFACTS

* endpoint_provider.json
* driver_stack_dump.txt
* endpoint_guid_report.json
* routing_topology.json

---

## FAIL条件

* hidden dependency
* backend spoofing
* unstable endpoint GUID
* fake endpoint provider

---

# SECTION 10

# REALTIME AUDIO STABILITY

## TEST-REALTIME-001

Requirement:
realtime callback stability。

---

## REQUIRED

* callback timing logs
* underrun logs
* overrun logs
* starvation metrics

---

## FAIL条件

* callback starvation
* realtime collapse
* unstable callback timing

---

# SECTION 11

# HOTSWAP VALIDATION

## TEST-HOTSWAP-001

Requirement:
real runtime hotswap。

---

## PASS条件

* official runtime maintained
* real model switch succeeds
* no graph corruption

---

## FAIL条件

* mock swap
* simulation swap
* callback deadlock

---

# SECTION 12

# AUDIO GRAPH VALIDATION

## TEST-GRAPH-001

Requirement:

```text id="jdfq3x"
Input PCM
→ Beatrice Runtime
→ VeilVoiceOut
```

のみ成立。

---

## FAIL条件

* raw passthrough
* bypass graph
* fallback raw route
* parallel raw output

---

# SECTION 13

# AUDIO AUTHENTICITY

## TEST-AUDIO-001

Requirement:
generated audio authenticity。

---

## REQUIRED VALIDATION

* waveform delta
* spectral difference
* runtime timing correlation
* tensor/audio consistency

---

## FAIL条件

* prerecorded waveform
* static waveform reuse
* silence substitution

---

# SECTION 14

# DISCORD / VRCHAT

## TEST-DISCORD-001

Requirement:
Discord stable operation。

---

## TEST-VRCHAT-001

Requirement:
VRChat stable operation。

---

## REQUIRED

* reconnect stability
* sample rate negotiation
* device persistence

---

# SECTION 15

# HOTPLUG

## TEST-HOTPLUG-001

Requirement:
USB reconnect <= 5s。

---

## FAIL条件

* reconnect failure
* endpoint loss
* callback freeze

---

# SECTION 16

# LONGRUN STABILITY

## TEST-STABILITY-001

Requirement:
3時間 continuous runtime。

---

## REQUIRED VALIDATION

* memory growth
* callback stability
* graph consistency
* no corruption

---

## FAIL条件

* memory leak
* graph corruption
* audio corruption
* deadlock

---

# SECTION 17

# CRASH SAFETY

## TEST-CRASH-001

Requirement:
crash isolation。

---

## FAIL条件

* zombie endpoint
* corrupted audio service
* persistent graph corruption

---

# SECTION 18

# SECURITY VALIDATION

## TEST-SECURITY-001

Requirement:
security hardening。

---

## REQUIRED TESTS

* malformed manifest
* path traversal
* symlink attack
* DLL injection
* runtime replacement
* backend spoofing

---

## FAIL条件

* unsigned runtime replacement
* manifest injection
* arbitrary DLL load

---

# SECTION 19

# HASH RULES

全artifact：

SHA256 mandatory。

---

# SECTION 20

# PASS AUTHORITY

PASS可能主体：

* acceptance_runner.exe
* provenance_verifier.exe
* CI pipeline
* binary inspector

LLMは禁止：

* PASS宣言
* completion宣言
* artifact生成
* self-validation

---

# SECTION 21

# DELIVERY BLOCKERS

以下成立時：

```text id="xq0m1v"
DELIVERY BLOCKED
```

* FAIL >= 1
* UNVERIFIED >= 1
* provenance mismatch
* hidden backend
* mock detected
* runtime trace broken
* security violation
* realtime instability

---

# SECTION 22

# FINAL DELIVERY PACKAGE

必須：

* VeilVoiceInstaller.exe
* acceptance_runner.exe
* provenance_verifier.exe
* binary_inspector.exe
* acceptance_report.html
* provenance_graph.json
* all hashes
* all traces
* all execution IDs
* routing topology
* security audit logs

---

# SECTION 23

# DEFINITION OF COMPLETE

以下すべて成立時のみ：

* FAIL == 0
* UNVERIFIED == 0
* official runtime verified
* official model verified
* real audio generation verified
* provenance valid
* callback stability verified
* security verified
* Discord verified
* VRChat verified
* no hidden backend
* no bypass route
* no replayed artifacts

その時のみ：

```text id="7lm2cp"
VeilVoice 完成
```

と定義する。
