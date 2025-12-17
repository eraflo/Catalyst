# CI/CD

Pipeline d'intégration et déploiement continu via GitHub Actions.

---

## Workflows

### 1. CI/CD Principal (`ci.yml`)

| Job | Déclencheur | Action |
|-----|-------------|--------|
| **test** | Push/PR | Exécute les tests Unity |
| **version** | Merge sur `main` | Bump version + Release |
| **validate** | Push | Valide la structure du package |

### 2. PR Checks (`pr.yml`)

| Job | Action |
|-----|--------|
| **check-version** | Vérifie si la version a été modifiée manuellement |
| **lint-commits** | Vérifie le format des commits |
| **breaking-changes** | Détecte les breaking changes |

---

## Versioning Automatique

La version est automatiquement mise à jour sur merge vers `main` selon les **Conventional Commits** :

| Type de commit | Bump | Exemple |
|----------------|------|---------|
| `fix:` | **Patch** (1.0.0 → 1.0.1) | `fix: correction du bug X` |
| `feat:` | **Minor** (1.0.0 → 1.1.0) | `feat: ajout de la feature Y` |
| `feat!:` ou `BREAKING CHANGE` | **Major** (1.0.0 → 2.0.0) | `feat!: nouvelle API` |

### Exemples de commits

```bash
# Patch version (1.0.0 → 1.0.1)
git commit -m "fix: correction du EventBus"

# Minor version (1.0.0 → 1.1.0)
git commit -m "feat: ajout du NetworkEventChannel"

# Major version (1.0.0 → 2.0.0)
git commit -m "feat!: refactoring complet de l'API"
# ou
git commit -m "feat: nouvelle API

BREAKING CHANGE: l'ancienne API est supprimée"
```

---

### Secrets GitHub

Ajouter dans **Settings > Secrets and variables > Actions** :

| Secret | Description |
|--------|-------------|
| `UNITY_LICENSE` | Contenu du fichier `.ulf` de licence Unity |
| `UNITY_EMAIL` | Email du compte Unity |
| `UNITY_PASSWORD` | Mot de passe du compte Unity |

### Obtenir la licence Unity (étape par étape)

#### 1. Générer le fichier de demande (.alf)

Ouvre PowerShell et exécute :
```powershell
& "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -createManualActivationFile -logFile unity.log -quit
```

> Remplace `2022.3.62f3` par ta version Unity.

Un fichier `Unity_v2022.x.alf` sera créé dans le dossier courant.

#### 2. Activer la licence

1. Va sur [license.unity3d.com/manual](https://license.unity3d.com/manual)
2. Upload le fichier `.alf`
3. Choisis le type de licence (Personal, Pro, etc.)
4. Télécharge le fichier `.ulf`

#### 3. Ajouter les secrets GitHub

1. Va dans ton repo → **Settings > Secrets and variables > Actions**
2. Clique sur **New repository secret**
3. Crée les 3 secrets :
   - `UNITY_LICENSE` : colle **tout le contenu** du fichier `.ulf`
   - `UNITY_EMAIL` : ton email Unity
   - `UNITY_PASSWORD` : ton mot de passe Unity

#### 4. Nettoyer

Supprime les fichiers temporaires :
```powershell
Remove-Item *.alf
Remove-Item unity.log
```

> ⚠️ **Ne jamais commit** les fichiers `.alf` ou `.ulf` !

---

## Tests

Les tests sont exécutés automatiquement sur chaque push et PR.

### Fichiers de tests

```
Tests/
├── Runtime/
│   ├── EventBusTests.cs
│   ├── EventChannelTests.cs
│   └── NetworkEventChannelTests.cs
└── Editor/
    └── (tests editor)
```

### Exécuter localement

Dans Unity : **Window > General > Test Runner**

---

## Releases

À chaque merge sur `main` :
1. Tests exécutés
2. Version bumpée automatiquement
3. Tag Git créé (`v1.2.3`)
4. Release GitHub créée avec notes générées
