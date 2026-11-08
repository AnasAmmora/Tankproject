# Post-match / rematch checkpoint — 2026-11-08

## Completed
- Server freezes winner/statistics after same-frame combat processing; publishes a ServerTime deadline five seconds later. Living players retain movement/fire; dead players remain locked. Damage, scoring, respawns and new hit impulses stop immediately; recoil is preserved. Existing projectile damage paths use the same server gate.
- Authored MatchCanvas/ResultRow prefabs edited through Unity MCP: reversible Rematch/Cancel Ready, Ready column, host-only Start, status text; serialized references saved. Runtime binds existing UI and instantiates only authored result rows.
- Server-owned frozen roster, readiness, permanent departure cancellation, immediate Back RPC, host-only atomic Start, duplicate/stale round validation, join lock. Accepted Start locks Back; actual disconnect still aborts loading.
- Reuses existing connection, Lobby/Relay session and NGO scene/preparation barrier. GameScene reload creates fresh players/bases/state; preserves identities/teams. Persistent UI suspension avoids duplicates. Immediate load failure restores Results with a new epoch and cleared readiness; stale pre-failure requests are rejected.
- Temporary verification fixture removed from Assets; archived under ignored Temp/RematchVerification/RematchVerification.cs.txt. Editor restored to MainMenuScene, test virtual editor closed. Unrelated assets preserved.

## Tests actually run
- Unity compilation: no errors after final edits. C# diff whitespace check passed (Unity prefab serializer emits conventional empty-string whitespace).
- Pure rules: host readiness required, reversible readiness, nonhost Start rejection, stale round rejection, duplicate Start, Start-before-Back lock, Back-before-Start cancellation, disconnect/reset, reconnect cannot restore cancelled session. Final kill counted and finalized statistics immutable.
- Two real local NGO peers in separate Unity editors, with test identities/local transport (not live authentication/Lobby/Relay allocation). Production scene synchronization/player preparation/ready barrier used. Three completed matches and two successful rematches. Teams unchanged, health100, statistics zero at each fresh start; exactly one MatchHud, GameplayHud and LoadingScreenController per peer. Logs: Temp/RematchVerification/two-cycles/.
- PostMatch: server deadline exactly now+5; living host control=true, dead client=false, respawnAt=-1; damage attempt rejected with health100 unchanged; final kill/death captured; Results not revealed at post entry. Actual host TryFire produced an active cosmetic projectile. Dead player's TryFire was invoked and remained control-blocked; active-count includes host's replicated projectile.
- Ready/unready replicated; client-only mask2 could not start, both-ready mask3 started. Duplicate Start and Back immediately after accepted Start did not interrupt. Third Results client Back cancelled remaining host's readiness and Start; host could exit.
- Forced NGO scene verification rejection: usable Results restored, readiness0, old-generation ready rejected, retry succeeded and clean match started. Logs: load-rejection/. A separate MCP screenshot call later produced editor-recursive-playerloop errors; these are capture-tool errors, not clean gameplay evidence. Successful two-cycles and loading-disconnect logs contain no error events.
- Real client shutdown at rematch-start/loading boundary: host cleaned up to MainMenu, transition=false; no stuck loading. Logs: loading-disconnect/.
- Results layout visually reviewed at1920x1080 using the existing GameScene authored UI with sample rows and forced button visibility for layout only. Screenshot: Temp/RematchVerification/results-layout.png. Early PreviewRenderUtility captures were blank and are not validation.
- Ordinary result-deadline observations were within roughly11ms in each peer's NGO time domain. One host sample was1.647s late while a synchronous MCP capture blocked the editor; timing under uninterrupted production build is not established. Client buffered ServerTime is not a shared exact physical clock.

## Remaining manual verification
- Actual authenticated Lobby/Relay session across builds/devices, four participants, latency/loss, host transport failure and genuine mid-scene-load timeout. Local loading-disconnect and immediate load rejection are verified, not every failure mode.
- Human controls/camera/cursor, post-match wall/player impact appearance/audio, already-flying projectile contacts across the finish boundary, recoil feel and full match timer progression. Server damage gate and post-match launch were exercised, not every cosmetic contact path.
- Responsive UI at other aspect ratios and long player names. Final additional guards (connected sender for Back; pending result retained on transport failure) compiled and pure-state checks rerun; full two-editor suite predates these two small guards.

## Exact next step
Playtest two builds through the existing Lobby/Relay: finish a match, move/fire for five seconds, toggle ready including host, Start twice, complete second match, then test Back and disconnect during the next rematch load. No temporary fixture is active or shipped.
