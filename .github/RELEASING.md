# Production releases

Keep existing development branches (including master); this change does not rename branches or create beta/main. Where beta/main are used, promote beta → main → release through PRs. CI runs on master, beta, main and release PRs/pushes. The compatibility check and fail-closed Release readiness aggregate are required validation, not proof of in-game behavior.

## Reviews and protection

The temporary organization policy requires a PR and one independent approval from `Meitou-Modding-Ecosystem/super-maintainers` on beta/main/release. The team requirement is native in the ruleset, so it does not depend on path-specific CODEOWNERS. Authors cannot approve themselves; no team/Actions review bypass is allowed. Existing build/security checks and force-push/deletion protection remain. Strict up-to-date status requirements are off to avoid reverse merges for history alone. Ordinary branches retain their existing behavior.

This interim policy excludes Husbandry, where the custom author-as-owner policy remains under test. CODEOWNERS still documents ownership but does not add an extra approval requirement here. Reconciliation and migration details: https://github.com/TheIvoryDisaster/GutenStructure/blob/main/docs/interim-review-policy.md .

## Packaging and promotion

This PR enables release-branch CI only. No complete production packaging/publishing workflow or deployment environment is introduced. When packaging is implemented, use reviewed SHA-pinned central workflows and publish from the current protected release head; production tags must resolve to that exact commit. The current gate checks head/ownership/rules, with hidden bypass-list verification relying on scheduled administration reconciliation; a branch name or passing compilation alone is not a release authorization.

Do not use Update branch on a PR whose source is a permanent release branch to pull development changes into production. If a promotion/backport needs updating, use a temporary branch and let normal CI/reviews validate it. A history-only merge commit does not require reverse synchronization merely to make GitHub's ahead/behind counts match.

Setup and current enforcement details: https://github.com/TheIvoryDisaster/GutenStructure/blob/main/docs/releases.md . Policy changes require the scoped administration workflow and read-back verification, not just this file.
