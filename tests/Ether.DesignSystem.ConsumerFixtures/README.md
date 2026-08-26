# Ether package consumer fixtures

These fixtures intentionally stay out of `Ether.DesignSystem.slnx`: each references only
`Ether.DesignSystem.Controls` from the version in `artifacts/consumer-fixtures/local-feed`, so adding either to
the ordinary solution would make a normal source build depend on a pre-existing local feed.
Foundation flows through Controls as a package dependency. Its `buildTransitive` asset target
copies the frozen host-root `Fonts/...` and `Assets/...` paths required by the package resources;
it does not alter assembly reference resolution.

- `Unpackaged` is the runtime smoke target.
- `Packaged` is built as an MSIX project; installation and launch are deliberately out
  of L0-B2 scope. It opts out of the transitive font copy because the current MSIX packaging
  targets cannot parse the frozen comma-named font file names; the unpackaged fixture verifies
  their host-root output layout instead.

Run `scripts/Verify-ConsumerFixtures.ps1` from the repository root to build the packages,
validate their contents, restore/build the fixtures, and launch the unpackaged smoke app.
The local default includes that runtime launch. CI may pass `-SkipRuntimeSmoke` only because
GitHub-hosted Windows runners do not provide a reliable WinUI GUI runtime; package and fixture
build assertions still run there. `-SkipSolutionBuild` is available when Debug and Release
solution builds have already run in the caller.
