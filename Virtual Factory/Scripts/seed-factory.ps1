# seed-factory.ps1
# Triggers a factory reseed by calling the Virtual Factory API.
#
# Usage:
#   .\Scripts\seed-factory.ps1 [-BaseUrl <url>]
#
# Parameters:
#   -BaseUrl   Base URL of the running Virtual Factory API.
#              Defaults to https://localhost:5001
#
# Prerequisites:
#   - The Virtual Factory API must be running.
#   - The API must expose a seed or reset endpoint (to be implemented in v0.2+).
#
# TODO: Replace placeholder with actual API call once the seed endpoint is available.
#       Example:
#         Invoke-RestMethod -Uri "$BaseUrl/api/admin/seed" -Method Post

param(
    [string]$BaseUrl = "https://localhost:5001"
)

Write-Host "Virtual Factory – Factory seed script" -ForegroundColor Cyan
Write-Host "Target: $BaseUrl" -ForegroundColor Gray
Write-Host "This script is a placeholder. Seed endpoint not yet implemented." -ForegroundColor Yellow
