# Production releases

Keep existing development branches (including master); this change does not rename branches or create beta/main. Where beta/main are used, promote beta → main → release through PRs. CI runs on master, beta, main and release PRs/pushes. The compatibility check and fail-closed Release readiness aggregate are required validation, not proof of in-game behavior.

## Reviews and protection

Ordinary branches remain unprotected. Beta/main require PRs and applicable code-owner review, with only the configured review-rule exceptions; mandatory CI/safety remains separate and cannot be bypassed through that exception. GitHub cannot count an author's self-approval. The selected super-maintainers team has PR-only review bypass; beta may additionally have the configured GitHub Actions review exception. Do not infer ownership merely from team membership.

The agreed native target for release is one independent approving reviewer who is an applicable CODEOWNER, with passing up-to-date CI, fresh review after changes, resolved conversations and no release bypass. Authors cannot approve their own PRs. This documentation does not apply that policy: the central reconciliation and release gate still expect two approving reviewers including an applicable CODEOWNER until a coordinated policy/gate rollout changes them. Follow the actual stronger requirement in GitHub; do not bypass it or relax the gate in isolation.

CODEOWNERS uses the last matching pattern. List the personal maintainer alongside path-specific owners when they must both be eligible to review; a fallback owner is overridden by more specific entries.

## Packaging and promotion

This PR enables release-branch CI only. No complete production packaging/publishing workflow or deployment environment is introduced. When packaging is implemented, use reviewed SHA-pinned central workflows and publish from the current protected release head; production tags must resolve to that exact commit. The current gate checks head/ownership/rules, with hidden bypass-list verification relying on scheduled administration reconciliation; a branch name or passing compilation alone is not a release authorization.

Do not use Update branch on a PR whose source is a permanent release branch to pull development changes into production. If a promotion/backport needs updating, use a temporary branch and let normal CI/reviews validate it. A history-only merge commit does not require reverse synchronization merely to make GitHub's ahead/behind counts match.

Setup and current enforcement details: https://github.com/TheIvoryDisaster/GutenStructure/blob/main/docs/releases.md . Policy changes require the scoped administration workflow and read-back verification, not just this file.
