# Phase 7 SketchUp licensing research

Accessed 2026-07-21. Sources are limited to the official SketchUp Help Center.

SketchUp subscription uses a Trimble ID and named-user assignment. SketchUp documents periodic online validation approximately every 28 days and mentions `%AppData%\SketchUp\SketchUp <year>\login_session.dat` for troubleshooting. WinLic checks only profile/session existence and optional filesystem timestamp. It never opens, hashes, copies, logs, or reports the file/path. File presence or age cannot prove entitlement, sign-in, device authorization, or expiry.

Classic/perpetual licenses require user name, serial and authorization code. SketchUp stopped selling new Classic licenses on 2020-11-04, but existing version-specific authorizations may remain. No documented safe local status API was found. Artifact presence therefore remains `Unknown`.

Classic network licensing uses pooled seats. Official deployment documentation describes `activation_info.txt`, which contains serial and authorization code, and network checkout for limited offline use. WinLic checks only known-file existence under ProgramData. It never reads contents, server identity, port, seat count, checkout data, serial or authorization code; it does not contact a server.

Trial and education/lab state have no sufficiently authoritative local read-only status source in scope: **NEEDS VERIFICATION**. Offline timestamps are not entitlement expiration. Explicit signed-out/not-licensed evidence would require a documented local source not found in this research.

**NO OFFICIAL READ-ONLY LOCAL LICENSE-STATUS CLI FOUND.** WinLic creates no command, launches no SketchUp/LayOut/browser, and calls no Trimble/Admin Console API.

False positives are controlled by excluding Viewer, Importer, SDK, language packs, LayOut/Style Builder as separate products, installers/updaters, V-Ray and Enscape, and by never returning `Licensed` from artifacts. False negatives remain possible for localized/unregistered installations and undocumented legacy layouts.
