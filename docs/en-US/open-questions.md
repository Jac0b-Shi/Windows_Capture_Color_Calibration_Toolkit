# Open Technical Questions

This file tracks unverified Windows, WGC, HDR, WinUI, D3D11, and DXGI behavior. Do not present these items as facts before verification.

| ID | Question | Status | Evidence or next step |
| --- | --- | --- | --- |
| WGC-HDR-001 | What conversion does WGC apply when BGRA8 is used on an HDR desktop? | Unverified | Later use Microsoft official documentation, official samples, and project experiments. |
| WGC-HDR-002 | How can the encoding and color space of an FP16 WGC surface be determined reliably? | Unverified | FP16 is not implemented in phase one. |
| WGC-HDR-003 | Is XAML solid-color fill stable enough as a declared chart source? | Unverified | Verify during the Milestone 3 renderer slice. |
| WGC-HDR-004 | Does Windows HDR SDR content brightness affect the chart window and captured values? | Unverified | Requires manual experiments. |
| WGC-HDR-005 | What color space do Display Picker or external color pickers report? | Unverified | Later record tool and system settings. |
| WGC-HDR-006 | Do window capture and monitor capture produce different color values? | Unverified | Later WGC single-frame experiments. |
| WGC-HDR-007 | Do multi-monitor, ICC, and Advanced Color configurations change the mapping? | Unverified | Later collect metadata on the diagnostics page. |
| WGC-HDR-008 | Does FP16 preserve more useful inverse-mapping information than BGRA8? | Unverified | Milestone 6 technical spike. |
| WGC-HDR-009 | What is the earliest stable Windows version that supports the required WGC HDR path? | Unverified | Do not use the current Insider/development machine version as the minimum. |
| WINUI-001 | Is packaged or unpackaged WinUI 3 better for the first release? | Unverified | Phase one uses a buildable shell; release packaging is a later decision. |
| WINUI-002 | Is a Direct3D renderer required to control chart output encoding? | Unverified | Decide after XAML renderer verification. |

