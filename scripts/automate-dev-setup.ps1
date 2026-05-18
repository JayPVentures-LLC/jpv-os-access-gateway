# automate-dev-setup.ps1
# Automates Docker installation (if missing), server startup, and homepage build/test for JPV-OS Access Gateway

function Install-DockerIfMissing {
  Write-Host "Checking for Docker..."
  if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker not found. Installing Docker Desktop..."
    $dockerInstaller = "$env:TEMP\DockerDesktopInstaller.exe"
    Invoke-WebRequest -Uri "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe" -OutFile $dockerInstaller
    Start-Process -FilePath $dockerInstaller -Wait
    Write-Host "Docker Desktop installation launched. Please complete the setup manually if prompted."
  }
  else {
    Write-Host "Docker is already installed."
  }
}


function Start-Server {
  Write-Host "Starting server (dotnet run)..."
  Push-Location "src\JPVOS"
  dotnet run --no-launch-profile
  Pop-Location
}

function Build-And-Test-Homepage {
  Write-Host "Building project..."
  Push-Location "src\JPVOS"
  dotnet build
  Write-Host "Running homepage tests (if any)..."
  # Add homepage-specific tests here if available
  Pop-Location
}

# Main automation flow
Install-DockerIfMissing
Start-Server
Build-And-Test-Homepage
