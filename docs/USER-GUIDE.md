# AiEcoSys User Guide

**Version 1.0.0**

AiEcoSys is a local desktop catalogue of AI providers and models. It reads
Models.dev metadata, shows published token prices when present, and helps you
select models for a task. It does not call model APIs or generate text.

## 1. Starting the application

Launch **AiEcoSys** from the Start Menu, desktop shortcut, or by running
`AiEcoSys.exe`.

The status bar shows the catalogue state and API mode. The Dashboard shows the
provider/model counts, last synchronisation, data source, and recent events.

## 2. Navigation

Use the left navigation pane:

1. Dashboard
2. Model Explorer
3. Provider Manager
4. Pricing
5. Model Selection
6. Catalogue
7. Integrations
8. Security
9. Settings
10. About

## 3. Dashboard

The Dashboard summarises the loaded catalogue:

- provider count and model count;
- catalogue status (for example `Offline` or `Ready`);
- last sync time;
- data source and timestamp;
- recent sync history.

## 4. Model Explorer

Use Model Explorer to find models in the loaded catalogue.

- **Search** matches model names as you type.
- **Modality** filters by output modality (text, image, audio, and so on).
- **Context** filters by context-window size.
- The table shows model name, provider, context, tool calling, and prices.
- The summary line reports how many models match.

Absent metadata is shown as `—`, not as zero or an invented value.

## 5. Provider Manager

The provider table lists provider name, identifier, API endpoint, model count,
and credential status.

- **Test connection** sends an HTTPS `HEAD` request to the provider API
  endpoint when one is published in the catalogue.
- A provider may reject `HEAD` even when its API works; the result is a
  connectivity check, not an authentication test.
- Credential status is read from Windows Credential Manager.

## 6. Pricing

Pricing shows input and output prices in **USD per 1 million tokens**, using
the values in the Models.dev catalogue.

Filters:

- **Price mode**: all models, missing prices, zero-cost, or positive prices.
- **Minimum input price** and **minimum output price**.

Models without a published cost show `—`. AiEcoSys does not estimate or
convert prices.

## 7. Model Selection

Model Selection narrows the catalogue by requirements:

- task (for example general text or tool calling);
- provider;
- input modality;
- context window;
- minimum input/output price.

The result table and summary update immediately. Use it to shortlist models,
then open Provider Manager for endpoint details.

## 8. Catalogue

The Catalogue view manages local snapshots.

- **Save snapshot** writes `latest.json` to the configured snapshot directory.
- **Refresh from snapshot** reloads that file into the registry.
- **Refresh online** downloads `https://models.dev/api.json` when offline mode
  is disabled.
- **Search** filters the catalogue records shown in the table.

Snapshots are JSON files containing providers and models. They do not contain
credentials.

## 9. Integrations

AcademyOS, UBOS, and MavenMeeM integrations are **opt-in** and disabled by
default.

- Enable an integration only if you use it.
- Enter an endpoint. Remote endpoints must use HTTPS; HTTP is allowed only for
  localhost.
- **Test** performs a `HEAD` request.
- **Save integration settings** persists the opt-in state and endpoint.

AiEcoSys does not modify AcademyOS, UBOS, or MavenMeeM data.

## 10. Security

The Security view reports the credential backend and HTTPS policy.

To store a provider credential:

1. Enter the provider identifier, for example `openai`.
2. Enter the secret in the **Secret** field. The field is a password box and
   is not data-bound.
3. Select **Store credential**.
4. The secret is written to Windows Credential Manager as
   `AiEcoSys:<provider>`.

To remove it, select **Delete credential**. Secrets are never written to
settings, snapshots, or logs.

## 11. Settings

Settings controls:

- **Data Location** — absolute, non-C: data root.
- **Snapshot Location** — must be inside the data root.
- **Offline mode** — disables online catalogue refresh.
- **Automatically refresh catalogue on launch** — when enabled and offline mode
  is off, the local catalogue is loaded first and the online refresh follows.

Saving a new data location also moves the default snapshot path under it.

## 12. About

About shows the application version, build configuration, runtime, MIT
licence, project links, and a feature summary.

## 13. Data locations

Portable copy:

- program: the folder containing `AiEcoSys.exe`;
- data: `Data` beside the executable;
- catalogue: `catalogue\api.json` beside the executable.

Installed copy:

- program: `D:\AiEcoSys\Application` by default;
- data: `D:\AiEcoSys\Data` by default;
- catalogue: `catalogue\api.json` in the program directory.

Settings are stored in `<data>\settings\app-settings.json`; snapshots in
`<data>\snapshots`.

## 14. Offline use

The bundled catalogue makes the application useful without a network. Online
refresh and provider connection tests require network access and are optional.
No telemetry is sent.

## 15. Getting help

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md),
[RECOVERY-GUIDE.md](RECOVERY-GUIDE.md), and
[CONFIGURATION-GUIDE.md](CONFIGURATION-GUIDE.md).
