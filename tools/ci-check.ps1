# CI smoke check — runs the checks this repo relies on by hand, in one command.
# Usage: pwsh tools/ci-check.ps1   (or: powershell tools/ci-check.ps1)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Fail($msg) {
    Write-Host "FAIL: $msg" -ForegroundColor Red
    exit 1
}

# 1. dotnet build -c Release — fail on any error.
Write-Host "== dotnet build -c Release ==" -ForegroundColor Cyan
dotnet build NOCCC.csproj -c Release
if ($LASTEXITCODE -ne 0) { Fail "dotnet build failed (exit $LASTEXITCODE)" }

# 2. dotnet test — the xUnit project covering the pure toggle/restore state machine.
Write-Host "== dotnet test (tools/tests) ==" -ForegroundColor Cyan
dotnet test tools/tests/NOCCC.Tests.csproj
if ($LASTEXITCODE -ne 0) { Fail "dotnet test failed (exit $LASTEXITCODE)" }

Write-Host "All checks passed." -ForegroundColor Green
