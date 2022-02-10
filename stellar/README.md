# Catch2 Adapater fork for Stellar Core

This is an *experimental* fork of the
[Catch2 test adapter extension for Visual Studio](https://github.com/JohnnyHendriks/TestAdapter_Catch2)
made to work with stellar-core.

It allows running tests with `stellar-core.exe test <test args>` syntax instead
of the default Catch2 `foo.exe <test args>` syntax. It also resolves some issues
in the original extension that prevented runnning stellar-core tests.

Eventually I may try merging the changes from this fork to the upstream, but
some of them are not trivial as I had to change the original logic (those
probably had some rationale for other projects).

# Installation

1. Download and install the pre-built `VSTestAdapterCatch2.vsix` extension from
   this directory or build one from the fork.

1. In Visual Studio Test Explorer choose `Configure run settings` from the
   settings menu and pick a `runsettings` file to use. Two examples of such
   files are in this directory: one for the normal mode and one for the mode
   with all versions enabled.

1. Click `Run All Tests In View` in the test explorer to trigger the test
   discovery. After that the Test Explorer should be populated with all the
   tests from the stellar-core.

Additional command line arguments can be provided via `<TestRunArgs>` in the
`runsettings` file.

See the original extension
[documentation](https://github.com/JohnnyHendriks/TestAdapter_Catch2) for more
details.

# Known issues

- While Visual Studio claims to automatically parallelize tests as long as they
  have an adapter, I wasn't able to make it run more than one test at a time, so
  the extension seems the most useful to debug singular tests using the nice
  Test Explorer interface.

- The tests are only discovered for the stell-core binary. For xdr.exe a
  separate `runsettings` file would be needed (it shouldn't have
  `DiscoverCommandLine` and `TestCommand` settings).

- The discovery has to be re-triggered when changing the configurations (e.g.
  it's not possible to run a single test in the Release mode before clicking
  'Run All Tests').

- For debugging check the `Tests` output in Visual Studio. Detailed logging may
  be enabled via providing `<Logging>debug</Logging>` in `runsettings`.
