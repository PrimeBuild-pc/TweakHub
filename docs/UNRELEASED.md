# TweakHub: next release (not released yet)

Changes merged after v1.6.1. They are kept for a larger release; move them into the release notes when the version is tagged.

- External tools: NVIDIA VBIOS Vix Editor (GPU & Display). The description carries the warnings: edited ROMs fail the original signature and must never be flashed with nvflash, raising power, current or voltage can damage hardware, many fields are experimental, binaries are unsigned. TweakHub only opens the project page.
- Devices → Affinity: "Suggested layout by role" gives graphics, USB, network and audio one physical core each, outside core 0, chosen from the CPU topology (P-cores only on hybrid CPUs; with several L3 caches the one without 3D V-Cache, or the one without CPU 0 when equal; network and audio share a core up to 6 cores). Each device can be unticked in the preview. Only the affinity changes, MSI and priority stay as they are; one backup and one elevation for the whole layout, undone per device with the existing restore. HD Audio controllers are now recognised by their PCI class code. Network RSS is not changed.
