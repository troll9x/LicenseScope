# Security policy

Only the current 1.0 release line receives security fixes. Use GitHub private vulnerability reporting when enabled (`NEEDS REPOSITORY SETTING`). Do not place product keys, license reports, account files, tokens, cookies, machine identifiers, or credentials in public issues. No private security email has been verified.

License Scope audit mode is read-only: activation, bypass, public KMS use, telemetry and report upload are outside scope. Explicit GUI remediation is limited to clearing a manually configured Windows KMS host and opening a Windows-registered vendor uninstaller for an installed product not confirmed as licensed. Both require typed confirmation and elevation. KMS removal invokes only `slmgr.vbs /ckms`; software removal blocks shell/script hosts and never offers Windows itself for uninstall. Neither path removes license keys, rearms Windows, or starts activation. Reports should be sanitized before sharing.

The Windows crack-trace analyzer is separate from those remediation actions and
is strictly read-only. It does not request elevation, mutate license state,
write Registry values, modify tasks or services, or delete files. Inaccessible
sources set the binary `ScanCompleted` field to `false`; they never become a
claim that no trace exists and never trigger remediation.
Historical event, Defender and allowlisted Prefetch/Amcache inspection is a
separate Deep forensic scan that is disabled by default and requires explicit
consent. It does not scan unrelated user files, upload data, or modify/delete
evidence. Current activation state is never treated as verified license
provenance.
