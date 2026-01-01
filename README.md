# Catalyst

Eraflo Catalyst is a comprehensive Unity toollset designed to accelerate development. It includes a robust Behaviour Tree system, Networking, Event Bus, Pooling, and more.

## Package Structure

```
Catalyst/
├── package.json              # Package manifest
├── CHANGELOG.md              # Version history
├── README.md                 # This file
├── LICENSE                   # License file
├── Runtime/                  # Runtime scripts
│   └── *.asmdef             # Runtime assembly definition
├── Editor/                   # Editor-only scripts
│   └── *.asmdef             # Editor assembly definition
└── Tests/                    # Test scripts
    ├── Runtime/             # Runtime tests
    └── Editor/              # Editor tests
```

## Installation

### Via Git URL (Package Manager)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter: `https://github.com/eraflo/Catalyst.git`

### Via manifest.json

Add the following line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eraflo.catalyst": "https://github.com/eraflo/Catalyst.git"
  }
}
```

## Requirements

- Unity 2022.3 or higher

## License

See [LICENSE](LICENSE) for details.
