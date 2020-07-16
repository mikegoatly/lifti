# Configures a local machine to work with the documentation and run hugo successfully
# Pre-reqs:
# 1) `hugo-extended`: choco install hugo-extended -y
# 2) NPM

$scriptPath = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

Push-Location (Join-Path $scriptPath "../docs" -Resolve)

npm install

git submodule update --init --recursive

Pop-Location

Write-Host "Now you can run the documentation locally using:" -ForegroundColor Green
Write-Host "cd docs" -ForegroundColor Cyan
Write-Host "hugo serve" -ForegroundColor Cyan