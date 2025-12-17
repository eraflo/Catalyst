# Package Settings

Configuration centrale du package UnityImportPackage.

---

## Emplacement

Le fichier de configuration est **créé automatiquement** lors de l'import du package :

```
Assets/Resources/UnityImportPackageSettings.asset
```

---

## Accès

### Via Menu
**Tools > Unity Import Package > Settings**

### Via Code
```csharp
using Eraflo.UnityImportPackage;

var settings = PackageSettings.Instance;
bool networkEnabled = settings.EnableNetworking;
bool debugMode = settings.NetworkDebugMode;
```

---

## Paramètres

| Paramètre | Type | Description |
|-----------|------|-------------|
| **Enable Networking** | `bool` | Active l'instanciation automatique du `NetworkEventManager` |
| **Network Debug Mode** | `bool` | Affiche les logs de debug réseau dans la console |

---

## Comportement au Runtime

### Si Enable Networking = true

Au lancement du jeu (`RuntimeInitializeOnLoadMethod`) :
1. Un GameObject `[NetworkEventManager]` est créé
2. `DontDestroyOnLoad` est appliqué → persiste entre les scènes
3. Prêt à recevoir un `INetworkEventHandler`

```
[PackageInitializer] NetworkEventManager initialized
```

### Si Enable Networking = false

Aucune action automatique. Tu dois gérer manuellement :
```csharp
var go = new GameObject("NetworkEventManager");
go.AddComponent<NetworkEventManagerBehaviour>();
DontDestroyOnLoad(go);
```

---

## Forcer le Rechargement

Si tu modifies les settings pendant l'exécution :
```csharp
PackageSettings.Reload();
```

---

## Dépannage

| Problème | Solution |
|----------|----------|
| "No settings found" warning | Créer via **Tools > Unity Import Package > Create Settings** |
| Settings non appliqués | Vérifier que le fichier est dans `Assets/Resources/` |
| NetworkEventManager absent | Activer **Enable Networking** dans les settings |
