# A heavily modified version of https://github.com/charlesjlee/hugo-publish-to-ghpages-powershell

param (
	[Parameter(Mandatory = $false)][string]$commitMessage = "",
	[Parameter(Mandatory = $false)][bool]$dryRun = $false,
	[Parameter(Mandatory = $false)][bool]$nopublish = $false,
	[Parameter(Mandatory = $false)][bool]$force = $false
)

# abort if no changes to commit
if ($commitMessage.Length -gt 0) {
	If ($force -eq $false -and -Not (git status --porcelain)) {
		Write-Host "No changes to commit. Aborting." -ForegroundColor Red
		exit
	}
}
elseif ($dryRun -eq $false) {
	Write-Host "No commit message provided. If you want to do dry-run without committing, specify dryRun=`$true." -ForegroundColor Red
	exit
}

$scriptPath = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

Push-Location (Join-Path $scriptPath ".." -Resolve)

"Deleting old publication"
Remove-Item .\docs\public -Force -Recurse -ErrorAction Ignore
mkdir .\docs\public | out-null
git worktree prune
Remove-Item .git\worktrees\public\ -Force -Recurse -ErrorAction Ignore

# Navigate to the Hugo folder
Push-Location docs

"Checking out gh-pages branch into public"
git worktree add -B gh-pages public origin/gh-pages

"Removing existing files"
Remove-Item .\public\* -Force -Recurse -Exclude '.git'

"Generating site"
hugo --quiet

"Building sample blazor site"
Remove-Item .\temp -Recurse -Force -ErrorAction Ignore
dotnet publish ..\samples\Blazor\Blazor.csproj -o .\temp\ -c release
Move-Item .\temp\wwwroot .\public\blazor-sample
Remove-Item .\temp -Recurse -Force -ErrorAction Ignore

"Fixing up relative path for blazor sample"
(Get-Content .\public\blazor-sample\index.html) `
	-replace '<base href="/" />', '<base href="/lifti/blazor-sample/" />' `
	-replace '_framework', 'framework' |
	Out-File .\public\blazor-sample\index.html

"Fixing underscored paths"
Rename-Item .\public\blazor-sample\_framework "framework"
Rename-Item .\public\blazor-sample\framework\_bin "bin"
(Get-Content .\public\blazor-sample\framework\blazor.webassembly.js) `
	-replace '_framework', 'framework' `
	-replace '_bin', 'bin' |
	Out-File .\public\blazor-sample\framework\blazor.webassembly.js

if ($commitMessage.Length -gt 0) {
	"Committing master branch"
	git add --all
	git commit -m $commitMessage

	"Committing gh-pages branch"
	Push-Location -path public
	git add --all
	git commit -m $commitMessage

	if ($nopublish -eq $false) {
		"Pushing master to Github"
		git push origin master

		"Pushing gh-pages branch to Github"
		git push origin gh-pages
	}

	# pop back to Hugo folder
	Pop-Location
}

# back to the repo root
Pop-Location

# back to wherever we were before
Pop-Location