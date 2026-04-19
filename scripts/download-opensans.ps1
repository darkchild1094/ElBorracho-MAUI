# Download OpenSans fonts into Resources/Fonts
# Usage: powershell -ExecutionPolicy Bypass -File .\scripts\download-opensans.ps1

$dest = Join-Path -Path $PSScriptRoot -ChildPath "..\Resources\Fonts"
$dest = [System.IO.Path]::GetFullPath($dest)
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }

$files = @(
    @{ Url = 'https://github.com/google/fonts/raw/main/apache/opensans/OpenSans-Regular.ttf'; Name = 'OpenSans-Regular.ttf' },
    @{ Url = 'https://github.com/google/fonts/raw/main/apache/opensans/OpenSans-Bold.ttf'; Name = 'OpenSans-Bold.ttf' },
    # Try common semibold name variants on the repo and save as OpenSans-Semibold.ttf
    @{ Url = 'https://github.com/google/fonts/raw/main/apache/opensans/OpenSans-SemiBold.ttf'; Name = 'OpenSans-Semibold.ttf' },
    @{ Url = 'https://github.com/google/fonts/raw/main/apache/opensans/OpenSans-Semibold.ttf'; Name = 'OpenSans-Semibold.ttf' }
)

foreach ($f in $files) {
    $out = Join-Path $dest $f.Name
    if (Test-Path $out) { Write-Host "Skipping existing: $($f.Name)"; continue }
    try {
        Write-Host "Downloading $($f.Url) -> $out"
        Invoke-WebRequest -Uri $f.Url -OutFile $out -UseBasicParsing -ErrorAction Stop
        Write-Host "Saved: $out"
        # If downloaded semibold variant succeeded, stop trying others that map to same name
        if ($f.Name -eq 'OpenSans-Semibold.ttf') { break }
    }
    catch {
        Write-Host "Failed to download $($f.Url): $($_.Exception.Message)"
    }
}

Write-Host "Done. Rebuild the project to include downloaded fonts."