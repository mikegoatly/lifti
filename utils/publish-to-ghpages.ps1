# Derived from https://github.com/charlesjlee/hugo-publish-to-ghpages-powershell

param (
    [Parameter(Mandatory=$true)][string]$commitMessage
)

# abort if no changes to commit
If (-Not (git status --porcelain)) {
	"No changes to commit. Aborted!"
	exit
}

$scriptPath = split-path $SCRIPT:MyInvocation.MyCommand.Path -parent

Push-Location (Join-Path $scriptPath ".." -Resolve)

"Deleting old publication"
Remove-Item .\docs\public -Force -Recurse -ErrorAction Ignore
mkdir public | out-null
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

"Committing master branch"
git add --all
git commit -m $commitMessage

"Committing gh-pages branch"
Push-Location -path public
git add --all
git commit -m $commitMessage

"Pushing master to Github"
git push origin master

"Pushing gh-pages branch to Github"
git push origin gh-pages

# pop back to Hugo folder
Pop-Location

# back to the repo root
Pop-Location

# back to wherever we were before
Pop-Location