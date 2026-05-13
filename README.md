# VEILVOICE

# EXECUTABLE DELIVERY & ACCEPTANCE CONTRACT

# VERSION 1.0

本契約は、
「AIリアルタイムボイスチェンジャー VeilVoice」
の納入条件、検証条件、完成定義を規定する。

本契約は「努力義務」ではない。

本契約は：

* 実機動作
* 再現可能性
* 自動検証
* 証拠生成
* Windows統合
* 音声安全性

を含む「完成保証契約」である。

---

# SECTION 0

# DEFINITIONS

## PRODUCT

VeilVoice:
リアルタイム音声変換アプリケーション。

## TARGET PLATFORM

* Windows 11 24H2
* x64
* 日本語環境
* 英語環境

## TARGET INPUT DEVICE

* FIFINE USB microphone

## TARGET OUTPUT DEVICE

* VeilVoiceOut
* Windows Recording Endpoint

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

以下のみ有効証拠とする：

* acceptance_runner.exe 出力
* 自動生成artifact
* hash付き成果物
* 実録音wav
* endpoint列挙結果
* JSON logs
* CI results
* crash dumps

## PRINCIPLE-003

PASSは、
機械検証済みartifactが存在する場合のみ成立。

## PRINCIPLE-004

証拠欠落時、
必ず UNVERIFIED とする。

## PRINCIPLE-005

以下は禁止：

* “動くはず”
* “設計上問題ない”
* “コードレビュー済み”
* “実質完成”
* “ほぼOK”
* “再現できないがPASS”

---

# SECTION 2

# REQUIRED DELIVERY COMPONENTS

納入対象：

* VeilVoice application
* signed installer
* virtual audio driver
* acceptance_runner.exe
* CI configuration
* automated verification scripts
* uninstall utility
* crash recovery subsystem
* realtime logging subsystem

---

# SECTION 3

# BOOTSTRAP GUARANTEE

## TEST-BOOTSTRAP-001

Requirement:
Windows初心者が、
15分以内にVC可能状態へ到達できること。

禁止事項：

* VoiceMeeter要求
* VB-CABLE手動設定
* REAPER要求
* PowerShell操作要求
* WDKインストール要求
* registry手編集
* endpoint GUID編集
* Windows既定変更要求

PASS条件：

* installer起動のみで利用開始
* FIFINE mic自動検出
* VeilVoiceOut自動生成
* Discord入力選択可能

Artifacts:

* install_log.txt
* install_duration.json
* bootstrap_capture.mp4
* endpoint_after_install.json

FAIL:

* reboot mandatory
* manual routing required
* PowerShell required

---

# SECTION 4

# INPUT DEVICE GUARANTEE

## TEST-INPUT-001

Requirement:
FIFINE microphone を自動入力として使用。

PASS:

selected_input_device.json:

```json id="8r8y0z"
{
  "device_name": "FIFINE ..."
}
```

Artifacts:

* input_endpoint_list.json
* selected_input_device.json
* initialization_log.txt

FAIL:

* unknown mic selected
* manual selection required

---

# SECTION 5

# VIRTUAL MIC GUARANTEE

## TEST-VMIC-001

Requirement:
VeilVoiceOut endpoint が生成されること。

PASS:

virtual_endpoint_list.json:

```json id="zw8lbm"
{
  "endpoint_name": "VeilVoiceOut"
}
```

Artifacts:

* virtual_endpoint_list.json
* veilvoiceout_guid.txt
* windows_recording_devices.png

FAIL:

* endpoint absent
* VB-CABLE visible
* VoiceMeeter exposed

---

# SECTION 6

# DISCORD GUARANTEE

## TEST-DISCORD-001

Requirement:
Discord Stable にて入力デバイスとして使用可能。

PASS:

* Discord input list contains VeilVoiceOut
* Discord input meter reacts

Artifacts:

* discord_capture.png
* discord_meter_capture.png
* discord_endpoint_log.json

FAIL:

* Discord restart required
* endpoint invisible
* default device dependency

---

# SECTION 7

# VRCHAT GUARANTEE

## TEST-VRCHAT-001

Requirement:
VRChat microphone inputとして利用可能。

Artifacts:

* vrchat_capture.png
* vrchat_audio_log.txt

FAIL:

* no mic detected
* crash occurs

---

# SECTION 8

# RAW VOICE LEAK PREVENTION

## TEST-PRIVACY-001

Requirement:
生声がVCへ流出しない。

PASS:

* processed voice exists
* raw voice absent
* mute outputs silence

Artifacts:

* raw_input.wav
* processed_output.wav
* muted_output.wav
* correlation_metrics.json

FAIL:

* raw mic audible
* bypass routing active

---

# SECTION 9

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

# SECTION 10

# LONGRUN STABILITY

## TEST-STABILITY-001

Requirement:
3時間以上安定動作。

Artifacts:

* cpu_metrics.csv
* memory_metrics.csv
* dropout_log.txt
* audio_crc_log.txt

FAIL:

* freeze
* unrecovered dropout
* audio corruption
* memory leak

---

# SECTION 11

# DEVICE HOTPLUG

## TEST-HOTPLUG-001

Requirement:
USB抜き差し後5秒以内復旧。

Artifacts:

* reconnect_log.txt
* device_change_log.txt

FAIL:

* restart required
* endpoint lost

---

# SECTION 12

# CRASH SAFETY

## TEST-CRASH-001

Requirement:
異常終了後もWindows音声環境を破壊しない。

Artifacts:

* crash_dump.dmp
* endpoint_post_crash.json
* audio_service_state.json

FAIL:

* audio service broken
* zombie endpoint remains

---

# SECTION 13

# RESOURCE GUARANTEE

## TEST-PERF-001

Requirement:
平均CPU使用率30%以下。

Environment:
Ryzen 7 5800X baseline

Artifacts:

* cpu_usage.csv
* inference_timing.csv

FAIL:

* avg_cpu > 30%

---

# SECTION 14

# CONFIG PERSISTENCE

## TEST-CONFIG-001

Requirement:
再起動後も設定保持。

Artifacts:

* config_before.json
* config_after.json

FAIL:

* settings reset

---

# SECTION 15

# UNINSTALL SAFETY

## TEST-UNINSTALL-001

Requirement:
完全アンインストール可能。

Artifacts:

* uninstall_log.txt
* leftover_files.txt
* registry_diff.json

FAIL:

* orphan driver remains
* endpoint zombie remains

---

# SECTION 16

# DRIVER REQUIREMENTS

## DRIVER-001

Requirement:
Virtual audio driver signed.

PASS:

* Microsoft attestation signing valid

Artifacts:

* driver_signature_report.txt
* signtool_verify.txt

FAIL:

* unsigned driver
* test mode dependency

---

# SECTION 17

# ARTIFACT REQUIREMENTS

全テスト成果物は以下必須：

* timestamp
* machine_id
* git_commit
* app_version
* artifacts_hash
* os_version

hash algorithm:
SHA256

---

# SECTION 18

# ACCEPTANCE RUNNER REQUIREMENTS

acceptance_runner.exe は：

* standalone executable
* no Python dependency
* deterministic output
* reproducible execution

であること。

---

# SECTION 19

# FINAL DELIVERY BLOCKERS

以下成立時、
納入禁止：

* FAIL >= 1
* UNVERIFIED >= 1
* artifact missing
* hash mismatch
* non reproducible result

---

# SECTION 20

# FINAL ACCEPTANCE AUTHORITY

完成判定主体：

* acceptance_runner.exe
* CI pipeline
* human artifact review

LLMは：

* 完成判定権限なし
* PASS宣言権限なし
* artifact改変権限なし

---

# SECTION 21

# FINAL DELIVERY PACKAGE

最終納入物：

* VeilVoiceInstaller.exe
* acceptance_runner.exe
* acceptance_report.html
* all logs
* all artifacts
* hashes.sha256
* CI bundle
* crash dump bundle
* reproducibility manifest
* endpoint GUID report
* uninstall verification report

---

# SECTION 22

# DEFINITION OF COMPLETE

以下すべて成立時のみ：

* FAIL == 0
* UNVERIFIED == 0
* artifact integrity valid
* CI reproducible
* reboot persistence valid
* Discord verified
* VRChat verified
* raw voice leak absent

その時のみ：

「VeilVoice 完成」

と定義する。
