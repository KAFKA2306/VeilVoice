# CONTRACT UPDATE

# MODEL RESOLUTION & SPEAKER PROFILE SPECIFICATION

# ADDENDUM FOR VEILVOICE CONTRACT v3.1

本追補契約は、
VeilVoice における：

* Beatrice model management
* speaker profile handling
* model path resolution
* compatible speaker verification

を規定する。

---

# SECTION 25

# ENGINE / MODEL SEPARATION

## RULE-ENGINE-001

Engine と Model を分離定義する。

Engine:
推論実行基盤。

Model:
音声人格・speaker profile。

---

## RULE-ENGINE-002

Engine は Beatrice mandatory。

許可：

* Beatrice
* Beatrice JVS Corpus Edition
* Beatrice v2

禁止：

* RVC substitution
* OpenVoice substitution
* external API fallback

---

# SECTION 26

# MODEL RESOLUTION SYSTEM

## TEST-MODEL-RESOLUTION-001

Requirement:
モデルpath固定禁止。

禁止：

```text id="3tb6o7"
models/beatrice_v2.onnx
```

固定依存。

---

## TEST-MODEL-RESOLUTION-002

Requirement:
以下path対応。

PASS条件：

* relative path
* absolute path
* UTF-8 path
* Japanese path
* spaces in path
* external drive path

Artifacts:

* model_resolution_log.txt
* model_path_test.json

FAIL:

* ASCII only
* fixed directory dependency
* startup crash on UTF-8 path

---

# SECTION 27

# MODEL MANIFEST SYSTEM

## TEST-MODEL-MANIFEST-001

Requirement:
全モデルは manifest 管理。

Required Manifest Format:

```json id="cf8d8l"
{
  "model_name": "Tsukuyomi",
  "engine": "Beatrice",
  "model_path": "D:/Models/Tsukuyomi/model.onnx",
  "sample_rate": 48000,
  "speaker_id": "tsukuyomi",
  "sha256": "...",
  "compatible_engine_versions": [
    "Beatrice v2"
  ]
}
```

Artifacts:

* model_manifest.json
* model_hash_manifest.json

FAIL:

* raw path only
* unidentified model
* no hash verification

---

# SECTION 28

# SPEAKER PROFILE SYSTEM

## TEST-SPEAKER-001

Requirement:
speaker profile selectable。

PASS:

* runtime speaker switching possible
* profile metadata visible

Artifacts:

* speaker_profiles.json
* runtime_switch_log.txt

FAIL:

* hardcoded single speaker
* rebuild required for speaker change

---

# SECTION 29

# REFERENCE COMPATIBLE MODEL

## TEST-REFERENCE-001

Requirement:
つくよみちゃん compatible reference model supported。

Definition:

Reference speaker:
Tsukuyomi

Requirement:

* official compatible manifest
* runtime compatibility verified
* inference success verified

Artifacts:

* tsukuyomi_validation_log.txt
* reference_model_hash.json
* processed_tsukuyomi_output.wav

FAIL:

* model incompatible
* runtime crash
* invalid sample rate

---

# SECTION 30

# USER MODEL SUPPORT

## TEST-USERMODEL-001

Requirement:
ユーザーモデル追加可能。

PASS:

* model hot add
* manifest recognition
* runtime load success

Artifacts:

* user_model_scan_log.txt
* runtime_model_load_log.txt

FAIL:

* rebuild required
* app restart mandatory

---

# SECTION 31

# MODEL HASH VERIFICATION

## TEST-HASH-001

Requirement:
モデルSHA256検証 mandatory。

PASS:

* startup hash verification success

Artifacts:

* model_hash_report.json

FAIL:

* hash mismatch ignored
* unsigned model silently accepted

---

# SECTION 32

# ENGINE COMPATIBILITY VALIDATION

## TEST-COMPAT-001

Requirement:
manifest と engine compatibility 検証。

PASS:

* engine version compatible
* runtime validation success

FAIL:

* incompatible runtime accepted
* invalid tensor shape accepted

Artifacts:

* compatibility_validation_log.txt
* tensor_shape_validation.json

---

# SECTION 33

# HOT RELOAD SYSTEM

## TEST-HOTRELOAD-001

Requirement:
speaker profile runtime switching。

PASS:

* runtime model switch success
* no app restart required

Artifacts:

* hotswap_log.txt
* model_transition_metrics.json

FAIL:

* app restart mandatory
* audio graph corruption

---

# SECTION 34

# MODEL SECURITY RULES

## RULE-SECURITY-001

禁止：

* silent remote model download
* hidden model replacement
* unsigned runtime model swap

---

## RULE-SECURITY-002

すべてのモデル変更は：

* UI visible
* logged
* hash recorded

mandatory。

---

# SECTION 35

# UPDATED DEFINITION OF COMPLETE

以下成立時のみ：

* Beatrice verified
* Tsukuyomi reference verified
* user model support verified
* model hash validation verified
* runtime speaker switching verified
* UTF-8 path compatibility verified

成立した場合のみ：

「VeilVoice 完成」

と定義する。
