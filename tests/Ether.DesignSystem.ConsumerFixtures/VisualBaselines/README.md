# Consumer visual baselines

This directory contains the reviewed, source-controlled PNG golden images for the
13 consumer controls in Light and Dark themes:

`controls/<control-id>-light.png` and `controls/<control-id>-dark.png`.

The default consumer-fixture run decodes these PNGs inside the WinUI fixture and
compares them with settled `RenderTargetBitmap` captures. It never writes here.
Regenerate the images only after a deliberate UI review:

```powershell
& 'C:\Users\yiqizhong\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoProfile -File .\scripts\Verify-ConsumerFixtures.ps1 -UpdateVisualBaselines
```

The explicit switch copies the accepted Light/Dark control captures here, leaving
all changed PNGs visible in `git diff` for code review. Whole-page Light, Dark,
and High Contrast screenshots remain runtime evidence only: window-level layout
and OS High Contrast state make them unsuitable as narrow per-control golden
baselines.
