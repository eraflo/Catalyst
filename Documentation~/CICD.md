# CI/CD

Continuous integration and deployment pipeline via GitHub Actions.

---

## Workflows

### 1. Main CI/CD (`ci.yml`)

| Job | Trigger | Action |
|-----|---------|--------|
| **test** | Push/PR | Runs Unity tests |
| **version** | Merge to `main` | Version bump + Release |
| **validate** | Push | Validates package structure |

### 2. PR Checks (`pr.yml`)

| Job | Action |
|-----|--------|
| **check-version** | Checks if version was manually changed |
| **lint-commits** | Validates commit message format |
| **breaking-changes** | Detects breaking changes |

---

## Automatic Versioning

The version is automatically updated on merge to `main` following **Conventional Commits**:

| Commit Type | Bump | Example |
|-------------|------|---------|
| `feat!:` or `BREAKING CHANGE` | **Major** (1.0.0 → 2.0.0) | `feat!: new API` |
| `feat:` | **Minor** (1.0.0 → 1.1.0) | `feat: added NetworkEventChannel` |
| `fix:` | **Patch** (1.0.0 → 1.0.1) | `fix: fixed EventBus bug` |
| `perf:` | **Patch** | `perf: optimized timer update` |
| `refactor:` | **Patch** | `refactor: cleaned up code` |
| `docs:` | **Patch** | `docs: updated README` |
| `style:` | **Patch** | `style: code formatting` |
| `test:` | **Patch** | `test: added unit tests` |
| `chore:` | **Patch** | `chore: updated dependencies` |
| `build:` | **Patch** | `build: updated CI config` |
| `ci:` | **Patch** | `ci: fixed workflow` |

> **Note**: `enhancement:` is no longer used. Use `feat:` for new features.

### Commit Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Examples

```bash
# Patch (1.0.0 → 1.0.1)
git commit -m "fix: fixed EventBus callback"
git commit -m "docs: updated Timer documentation"
git commit -m "refactor(timers): cleaned up backend code"

# Minor (1.0.0 → 1.1.0)
git commit -m "feat: added NetworkEventChannel"
git commit -m "feat(timers): added persistence support"

# Major (1.0.0 → 2.0.0)
git commit -m "feat!: complete API refactoring"
# or with body
git commit -m "feat: new API

BREAKING CHANGE: old API has been removed"
```

---

### GitHub Secrets

Add in **Settings > Secrets and variables > Actions**:

| Secret | Description |
|--------|-------------|
| `UNITY_LICENSE` | Content of the Unity `.ulf` license file |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `PAT_TOKEN` | Personal Access Token for version bumping |

### Getting the Unity License (Step by Step)

#### 1. Generate the activation request file (.alf)

Open PowerShell and run:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe" -batchmode -createManualActivationFile -logFile unity.log -quit
```

> Replace `2022.3.62f1` with your Unity version.

A `Unity_v2022.x.alf` file will be created in the current folder.

#### 2. Activate the License

1. Go to [license.unity3d.com/manual](https://license.unity3d.com/manual)
2. Upload the `.alf` file
3. Choose license type (Personal, Pro, etc.)
4. Download the `.ulf` file

#### 3. Add GitHub Secrets

1. Go to your repo → **Settings > Secrets and variables > Actions**
2. Click **New repository secret**
3. Create the secrets:
   - `UNITY_LICENSE`: paste **all content** of the `.ulf` file
   - `UNITY_EMAIL`: your Unity email
   - `UNITY_PASSWORD`: your Unity password

#### 4. Clean Up

Delete temporary files:
```powershell
Remove-Item *.alf
Remove-Item unity.log
```

> ⚠️ **Never commit** `.alf` or `.ulf` files!

---

## Tests

Tests are automatically run on every push and PR.

### Test Files

```
Tests/
├── Runtime/
│   ├── EventBusTests.cs
│   ├── EventChannelTests.cs
│   └── NetworkEventChannelTests.cs
└── Editor/
    └── (editor tests)
```

### Run Locally

In Unity: **Window > General > Test Runner**

---

## Releases

On every merge to `main`:
1. Tests are executed
2. Version is automatically bumped
3. Git tag is created (`v1.2.3`)
4. GitHub Release is created with auto-generated notes
