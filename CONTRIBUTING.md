# Contributing Guide

## Git Workflow

This repository uses a **dev → master** branching strategy:

1. Create a feature branch off `dev`
2. Open a pull request targeting `dev`
3. Once `dev` is ready to release, open a pull request from `dev` to `master`

```
feature-branch  →  dev  →  master
```

> **Rule:** Only the `dev` branch may be merged directly into `master`.  
> The `protect-master` CI check enforces this automatically on every pull request.

---

## Fixing a Diverged dev Branch (One-Time Step)

If `dev` has fallen behind `master` (e.g. commits were merged directly to `master`), run the following commands to bring `dev` up to date:

```bash
git fetch origin
git checkout dev
git merge origin/master --ff-only
git push origin dev
```

If the fast-forward fails (because branches have diverged), use a regular merge instead:

```bash
git fetch origin
git checkout dev
git merge origin/master -m "chore: sync dev with master"
git push origin dev
```

After this one-time fix, the `sync-dev-with-master` workflow will keep `dev` automatically in sync going forward.

---

## Enabling Branch Protection on master (Recommended)

To prevent direct pushes to `master` and require a passing status check before any merge, configure branch protection in the GitHub repository settings:

1. Go to **Settings → Branches** in this repository.
2. Click **Add branch protection rule**.
3. Set **Branch name pattern** to `master`.
4. Enable **Require a pull request before merging**.
5. Enable **Require status checks to pass before merging**, then search for and select **"Ensure PR to master comes from dev"**.
6. Enable **Do not allow bypassing the above settings** (to enforce for admins too).
7. Click **Save changes**.

With these settings in place:
- No one can push directly to `master`.
- PRs to `master` from any branch other than `dev` will be blocked by the required status check.

---

## Automated Workflows

| Workflow file | Trigger | Purpose |
|---|---|---|
| `.github/workflows/protect-master.yml` | PR opened targeting `master` | Fails the check if the source branch is not `dev` |
| `.github/workflows/sync-dev-with-master.yml` | Push to `master` | Fast-forwards `dev` to match `master` automatically |
