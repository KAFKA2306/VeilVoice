# VeilVoice

# Beatrice Virtual Mic - Executable Acceptance Contract v1.0

## Contract Rules

本ドキュメントは「作業メモ」ではない。
本ドキュメントは「納入判定契約」である。

LLM、人間、開発者の発言は証拠にならない。
証拠として有効なのは以下のみ：

* acceptance_runner が生成した成果物
* 自動保存ログ
* 録音ファイル
* スクリーンショット
* JSON結果
* ハッシュ付きartifact
* Git commit
* 実行環境情報

禁止事項：

* 「たぶん動く」
* 「コード上問題ない」
* 「設計的にはOK」
* 「再現できないがPASS」
* 手動PASS宣言
* スクリーンショット手差し替え
* ログ編集

状態定義：

PASS:

* required artifacts complete
* automatic judge passed
* hash valid

FAIL:

* evidence exists and condition failed

UNVERIFIED:

* evidence missing
* automation incomplete
* environment invalid
* runner crashed

すべてのテスト結果は以下を必須とする：

* timestamp
* machine_id
* os_version
* app_version
* git_commit
* artifacts_hash
* test_duration_ms

---

# SECTION 1 - INSTALLATION

## TEST-INSTALL-001

Requirement:
Windows 11クリーン環境で15分以内に利用開始できること。

Environment:

* Windows 11 24H2
* no Python
* no VoiceMeeter
* no REAPER
* no VB-CABLE preinstalled

Test Method:
acceptance_runner.exe --test clean_install

Artifacts:

* install_log.txt
* installer_exit_code.txt
* install_duration.json
* installed_programs.json
* device_manager_capture.png

PASS:

* install_duration <= 15min
* virtual mic appears
* reboot not required
* no manual driver patch

FAIL:

* manual WDK install required
* unsigned driver blocking
* Python install required
* PATH editing required

UNVERIFIED:

* missing artifacts
* installer aborted

---

# SECTION 2 - VIRTUAL MIC DETECTION

## TEST-VMIC-001

Requirement:
仮想マイクがWindows録音デバイスとして認識されること。

Test Method:
acceptance_runner.exe --test endpoint_visibility

Artifacts:

* endpoint_list.json
* endpoint_guid.txt
* sound_settings_capture.png
* audio_service_log.txt

PASS:

* endpoint visible
* GUID stable across reboot
* endpoint enabled

FAIL:

* endpoint hidden
* GUID changes
* duplicate endpoints generated

UNVERIFIED:

* endpoint enumeration failed

---

# SECTION 3 - DISCORD COMPATIBILITY

## TEST-DISCORD-001

Requirement:
Discord入力デバイスとして使用可能。

Environment:

* Discord Stable latest

Test Method:
acceptance_runner.exe --test discord_input_visibility

Artifacts:

* discord_capture.png
* endpoint_match.json
* discord_audio_meter_capture.png

PASS:

* device selectable
* meter reacts
* no reboot required

FAIL:

* default device switching required
* Discord restart required
* device invisible

UNVERIFIED:

* Discord automation unavailable

---

# SECTION 4 - VRCHAT COMPATIBILITY

## TEST-VRCHAT-001

Requirement:
VRChatマイク入力として認識される。

Test Method:
acceptance_runner.exe --test vrchat_input_visibility

Artifacts:

* vrchat_capture.png
* vrchat_log.txt
* endpoint_map.json

PASS:

* selectable
* voice transmitted

FAIL:

* mic unavailable
* crash occurs

UNVERIFIED:

* VRChat unavailable

---

# SECTION 5 - RAW VOICE LEAK PREVENTION

## TEST-PRIVACY-001

Requirement:
生声がVCへ流出しない。

Test Method:
acceptance_runner.exe --test raw_voice_leak

Artifacts:

* raw_input.wav
* processed_output.wav
* mute_output.wav
* correlation_metrics.json

PASS:

* mute state outputs silence
* processed voice only
* raw correlation below threshold

FAIL:

* raw voice detectable
* fallback routes raw mic

UNVERIFIED:

* audio comparison failed

---

# SECTION 6 - LATENCY

## TEST-LATENCY-001

Requirement:
往復遅延150ms未満。

Test Method:
acceptance_runner.exe --test latency

Artifacts:

* latency_metrics.json
* waveform_alignment.png
* realtime_log.txt

PASS:

* p95 latency < 150ms

FAIL:

* p95 latency >= 150ms

UNVERIFIED:

* sync failure

---

# SECTION 7 - LONG RUN STABILITY

## TEST-STABILITY-001

Requirement:
3時間連続運用可能。

Test Method:
acceptance_runner.exe --test longrun --duration 3h

Artifacts:

* cpu_metrics.csv
* memory_metrics.csv
* dropout_log.txt
* audio_crc_log.txt

PASS:

* no crash
* no unrecovered dropout
* memory growth within threshold

FAIL:

* freeze
* audio corruption
* memory leak

UNVERIFIED:

* interrupted run

---

# SECTION 8 - DEVICE HOTPLUG

## TEST-HOTPLUG-001

Requirement:
USB抜き差しで復旧する。

Test Method:
acceptance_runner.exe --test hotplug

Artifacts:

* device_change_log.txt
* reconnect_log.txt
* endpoint_before_after.json

PASS:

* auto reconnect success
* audio restored <= 5s

FAIL:

* app restart required
* permanent disconnect

UNVERIFIED:

* hotplug event missing

---

# SECTION 9 - CRASH RECOVERY

## TEST-RECOVERY-001

Requirement:
異常終了後もWindows音声環境を壊さない。

Test Method:
acceptance_runner.exe --test forced_crash

Artifacts:

* crash_dump.dmp
* recovery_log.txt
* endpoint_post_crash.json

PASS:

* Windows audio operational
* no phantom endpoint
* restart possible

FAIL:

* audio service corruption
* endpoint zombie remains

UNVERIFIED:

* crash injection failed

---

# SECTION 10 - RESOURCE USAGE

## TEST-PERF-001

Requirement:
CPU常用率30%以下。

Environment:

* Ryzen 7 5800X baseline

Test Method:
acceptance_runner.exe --test perf

Artifacts:

* cpu_usage.csv
* gpu_usage.csv
* inference_timing.csv

PASS:

* avg_cpu <= 30%

FAIL:

* avg_cpu > 30%

UNVERIFIED:

* metrics unavailable

---

# SECTION 11 - CONFIGURATION

## TEST-CONFIG-001

Requirement:
再起動後も設定保持。

Test Method:
acceptance_runner.exe --test config_persistence

Artifacts:

* config_before.json
* config_after.json
* startup_log.txt

PASS:

* configs identical

FAIL:

* settings reset

UNVERIFIED:

* config unreadable

---

# SECTION 12 - UNINSTALL

## TEST-UNINSTALL-001

Requirement:
完全アンインストール可能。

Test Method:
acceptance_runner.exe --test uninstall

Artifacts:

* uninstall_log.txt
* registry_diff.json
* endpoint_post_uninstall.json
* leftover_files.txt

PASS:

* no active endpoint
* no orphan service
* no startup task

FAIL:

* zombie driver remains
* orphan registry remains

UNVERIFIED:

* uninstall interrupted

---

# FINAL ACCEPTANCE RULES

製品納入条件：

* FAIL = 0
* UNVERIFIED = 0
* PASS rate = 100%
* artifact hash valid
* all logs reproducible

LLMは禁止：

* PASS宣言
* 主観評価
* “ほぼ完成”
* “実質OK”

最終納入物：

* acceptance_report.html
* results/*.json
* artifacts/*
* logs/*
* hashes.sha256
* git_commit.txt
* build_manifest.json

完成判定主体：

* acceptance_runner.exe
* CI pipeline
* human reviewer

LLMは完成判定主体ではない。
