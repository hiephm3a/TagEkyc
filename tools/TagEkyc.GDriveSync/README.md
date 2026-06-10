# TagEkyc.GDriveSync

Local CLI for syncing repository documentation to a configured Google Drive folder.

This tool is intentionally separate from the TagEkyc runtime projects. It uses Google OAuth2 and Google Drive API only inside `tools/TagEkyc.GDriveSync`.

## What It Does

- Syncs a local folder, usually `docs/`, to a target Google Drive folder ID.
- Preserves local relative folder structure on Drive.
- Creates missing Drive folders and files.
- Updates existing Drive files when content changes.
- Skips unchanged files by comparing local MD5 with Drive `md5Checksum`.
- Supports multiple Google accounts through named profiles.
- Does not delete remote files by default.

## What It Does Not Do

- It does not sync source code by default.
- It does not require Google Drive for desktop.
- It does not use Google API keys for private Drive access.
- It does not commit OAuth tokens or secrets.
- It does not install a Git hook automatically.
- It requests Google Drive OAuth access so it can find and update existing files in the configured folder.

## Google Setup

Create an OAuth client in Google Cloud Console:

1. Create or select a Google Cloud project.
2. Enable the Google Drive API.
3. Configure OAuth consent screen.
4. Create an OAuth client for a desktop app.
5. Copy the client id and client secret.

Use the destination Google Drive folder ID, not the folder name. The ID is in the folder URL:

```text
https://drive.google.com/drive/folders/<FOLDER_ID>
```

## Local Config

Copy the example config:

```powershell
Copy-Item .gdrive\config.example.json .gdrive\config.local.json
```

Edit `.gdrive/config.local.json`:

```json
{
  "applicationName": "TagEkyc.GDriveSync",
  "profiles": {
    "main": {
      "clientId": "YOUR_GOOGLE_OAUTH_CLIENT_ID",
      "clientSecret": "YOUR_GOOGLE_OAUTH_CLIENT_SECRET",
      "tokenPath": ".gdrive/tokens/main",
      "driveFolderId": "YOUR_TARGET_GOOGLE_DRIVE_FOLDER_ID",
      "localRoot": "docs"
    }
  }
}
```

`.gdrive/config.local.json` and `.gdrive/tokens/` are ignored by Git.

## Commands

Authenticate a profile:

```powershell
dotnet run --project tools\TagEkyc.GDriveSync -- auth --profile main
```

Dry-run a full docs sync:

```powershell
dotnet run --project tools\TagEkyc.GDriveSync -- sync --profile main --dry-run
```

Run a full docs sync:

```powershell
dotnet run --project tools\TagEkyc.GDriveSync -- sync --profile main
```

Sync files changed in the latest commit:

```powershell
dotnet run --project tools\TagEkyc.GDriveSync -- sync-changed --profile main --commit HEAD
```

Optionally trash remote files missing locally during full sync:

```powershell
dotnet run --project tools\TagEkyc.GDriveSync -- sync --profile main --delete-missing
```

Use `--delete-missing` carefully. It is ignored by `sync-changed`.

## Multi-Account Profiles

Add another profile:

```json
{
  "profiles": {
    "main": {
      "clientId": "...",
      "clientSecret": "...",
      "tokenPath": ".gdrive/tokens/main",
      "driveFolderId": "...",
      "localRoot": "docs"
    },
    "clientA": {
      "clientId": "...",
      "clientSecret": "...",
      "tokenPath": ".gdrive/tokens/clientA",
      "driveFolderId": "...",
      "localRoot": "docs"
    }
  }
}
```

Run with:

```powershell
dotnet run --project tools\TagEkyc.GDriveSync -- sync --profile clientA
```

## Post-Commit Hook

The repository includes an opt-in hook script:

```text
tools/git-hooks/post-commit-gdrive-sync.ps1
```

Install it for this local checkout:

```powershell
.\tools\git-hooks\install-post-commit-gdrive-sync.ps1 -Profile main
```

The hook syncs changed docs from `HEAD`. It does not run during normal build/test.
