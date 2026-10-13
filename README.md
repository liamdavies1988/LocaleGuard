# LocaleGuard 🛡️

A lightweight, automated C# .NET CLI diagnostic tool built to audit localization files (JSON) for parity, missing keys, and broken template placeholders before deployment.

![build](https://github.com/liamdavies1988/LocaleGuard/actions/workflows/ci.yml/badge.svg)
![license](https://img.shields.io/badge/license-MIT-blue.svg)
![dotnet](https://img.shields.io/badge/.NET-8.0-purple.svg)

---

## 📌 Problem Statement
In bilingual and multilingual applications (such as English/Welsh public sector platforms or EdTech services), localization bugs frequently bypass manual QA:
- **Missing Keys:** Translators overlook newly added UI elements, causing unhandled runtime exceptions.
- **Variable Mismatches:** Developers update variable templates (e.g., `{username}`), but translators leave `{user_name}`, causing frontend rendering failures.
- **Empty / Stub Entries:** Untranslated keys ship to production, leaving blank buttons or broken UI flows.

`LocaleGuard` acts as a quality gate in CI/CD pipelines to catch these discrepancies automatically before code reaches staging or production.

---

## 🚀 Key Features
- **Hierarchical Key Flattening:** Recursively parses nested JSON trees (`footer.privacy.terms`).
- **Placeholder Parity Auditing:** Regex-powered variable extraction to ensure interpolation consistency across languages.
- **Spectre.Console Diagnostic UI:** Clear, color-coded diagnostic tables with distinct error classifications (`MISSING_KEY`, `EMPTY_VALUE`, `VARIABLE_MISMATCH`).
- **CI/CD Integration Ready:** Returns exit code `1` on failure to natively block faulty build pipelines.
- **Automated Test Coverage:** Tested using xUnit to verify string parsing and boundary edge cases.

---

## 🛠️ Tech Stack
- **Language:** C# / .NET 8
- **Console Rendering:** Spectre.Console
- **Unit Testing:** xUnit
- **CI/CD:** GitHub Actions

---

## 🚦 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Installation & Run
```bash
# Clone the repository
git clone https://github.com/liamdavies1988/LocaleGuard.git
cd LocaleGuard

# Run the validator against your locale files
dotnet run -- "./locales/en.json" "./locales/cy.json"
