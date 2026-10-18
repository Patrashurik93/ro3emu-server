# Upstream integration — 2026-10-18

## Revision and rollback

Both project branches integrate upstream `6befb4b2001fee6e4d6e5c2d8ca4f9416e774a11`, pinned at the beginning of this update. This adds 44 commits (including merge commits) after the previously integrated `32b9b9d20aadafc7cb7c400816f52e3efbe6e360`. History is preserved by a merge, without rebase or force-push.

Windows backup ref `backup/pre-upstream-20261018` preserves Original `db5bace545dfe1028c5f9540c4c160f39ab6efe6` and Classic `8e46564e30e27fe774fa5a7bce3e222431b87f46`.

## Attack-fix review

**Keep both fixes. They solve different bugs.**

- Upstream `c40dfee` makes an active skill with `_iIfAutoAtk=2` continue using the career's normal attack. Previously Blade Smash / Sonic Slash themselves repeated, bypassing their cooldowns.
- Our `NormalAttackCadence` preserves the normal-attack action interval independently of a cancelled animation, scoped to the role and weak session key. Upstream's fix does not provide this guard and does not supersede it. It does not grant active skills a normal-attack cooldown.
- The merged tree retains upstream's loop-skill choice in both PvE and event-PvP paths, plus our manual-cast admission and shared normal-attack start guard.
- Existing wire regressions `NormalAttackInterruptDoesNotResetCadenceAndAttackResumesAfterIt` and `AutoNormalAttackSkillIsFollowedByTheNormalAttackNotRepeatedPastItsCooldown` exercise the combined implementation.

The current-career PC wheel fallback and role-scoped reversible `!unlockall` remain. Skill slot occupation/deduplication, plan changes, and structural guards are included in regression checks.

## Superseded local fixes

Four conflict resolutions deliberately take the new upstream versions:

- `ActivityClock.cs`: upstream centralizes civil-date conversion in the realm-zone `SeasonClock.StartTime`. This supersedes our earlier inline conversion.
- `DatabaseBackup.cs`: upstream runs stale-partial cleanup in `StartAsync` under `backupGate`, before the background backup starts. Remove our old duplicate cleanup in `ExecuteAsync`; retain the upstream restore journal/recovery improvements.
- `DailyWindowsTests.cs`: upstream tests cover realm/host timezone separation and dynamic schedule data. Our former test mutated the clock timezone after host construction and no longer matches the centralized season-clock contract.
- `TimedEventTests.cs`: the old fixed treasure fight fixture has been replaced by upstream Rift rounds, party admission and settlement tests. Do not restore that obsolete scenario during conflict resolution.

Other earlier cadence-aware fixture adjustments are retained by the automatic merge.

## Verification

The following checks completed on Windows independently in each repository:

| Check | Original | Classic |
| --- | --- | --- |
| Selected server/game/client/backup regressions | 132 passed, 0 failed, 0 skipped | 132 passed, 0 failed, 0 skipped |
| Patcher | 19 passed, 0 failed, 0 skipped | 19 passed, 0 failed, 0 skipped |
| Setup, including isolated full export from the installed client | 85 passed, 0 failed, 2 skipped | 85 passed, 0 failed, 2 skipped |
| Release builds: Server, ClientHooks, ClientTools | 3/3 successful | 3/3 successful |

The two Setup skips require additional `RO3_GENESIS_CLIENT` / signed-manifest fixture inputs; they are not claimed as tested. The game export test uses `RO3_CLIENT_DIR` and did run. This is a bounded regression set, not a claim that every client-data test or live gameplay has passed. The two repositories' `src`, `tests` and `tools` Git trees were identical after resolution.

The reproducible server selection is in `UPSTREAM_INTEGRATION_2026-10-18.filter.txt`. Run `dotnet test tests/Ro3Emu.Tests -c Release --filter <contents-of-filter-file>` with at most four xUnit collection threads. Tests use separate temporary state/DB directories, not the live player's database. Source and client-tools compilation use each project's own installed client DLLs. Warnings are recorded in logs, not treated as passed errors. Windows logs, TRX and runsettings are retained outside Git in `C:\Shurik\RO3_Backups\update-20261018`.

## Deployment boundaries

This updates and builds the two development repositories. It does not switch the older admin-owned deployment, Desktop launchers, live database or installed game DLLs. Classic endpoint/port isolation is still required before a separate gameplay launch. No official client repair/update or migration of the live DB is performed.

The known Thief Double Attack implementation gap and `_iSkillHit=10000` evasion bypass remain outside this update; merging this upstream revision does not claim to fix them. No new Classic gameplay design or pass-policy changes are introduced by us. Upstream's own hooks/prebuilt tools are retained; game binaries, private exported data, keys and DBs are not added.
