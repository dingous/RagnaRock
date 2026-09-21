[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Repository = 'dingous/RagnaRock'
foreach ($tool in @('git','gh')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "Install '$tool' before continuing. Git: git-scm.com. GitHub CLI: cli.github.com."
    }
}
function Invoke-Checked {
    param([string]$Program, [string[]]$Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Program failed with exit code $LASTEXITCODE. No force-push was attempted." }
}
Push-Location $Root
try {
    if (-not (Test-Path 'Assets/RagnaRock/Scenes/RagnaRock.unity')) { throw 'Wrong project directory.' }
    # Authentication is handled by GitHub CLI/device login. Never paste a token into a script or chat.
    & gh auth status
    if ($LASTEXITCODE -ne 0) { Invoke-Checked -Program 'gh' -Arguments @('auth','login','--hostname','github.com','--web','--git-protocol','https') }
    $login = (& gh api user --jq '.login' | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $login -ne 'dingous') { throw "Expected dingous; authenticated as '$login'. Use gh auth switch and retry." }
    & gh repo view $Repository --json nameWithOwner
    if ($LASTEXITCODE -eq 0) { throw "$Repository already exists. Stopping to avoid modifying an existing repository." }
    # An unavailable view can also be a network/permission error. The create command below must succeed;
    # it never overwrites an existing remote repository.
    if (-not (Test-Path '.git')) {
        Invoke-Checked -Program 'git' -Arguments @('init','--initial-branch=master')
        Invoke-Checked -Program 'git' -Arguments @('add','--all')
        Invoke-Checked -Program 'git' -Arguments @('-c','user.name=RagnaRock Project Generator','-c','user.email=generated@local.invalid','commit','-m','feat: initial RagnaRock Unity survivor project')
    }
    $branch = (& git branch --show-current | Out-String).Trim()
    if ($branch -ne 'master') { throw "Expected master branch; found '$branch'. No branch was changed." }
    $dirty = (& git status --porcelain | Out-String).Trim()
    if ($dirty) { throw 'Working tree has uncommitted changes. Review and commit them before publishing.' }
    $remotes = (& git remote | Out-String).Trim()
    if ($remotes) { throw "A remote already exists: $remotes. Stopping without modifying it." }
    Invoke-Checked -Program 'gh' -Arguments @('repo','create',$Repository,'--private','--description','Unity 3D metal survivor: defend the stage with four musicians.','--source','.','--remote','origin','--push')
    Invoke-Checked -Program 'gh' -Arguments @('repo','view',$Repository,'--json','url,visibility,defaultBranchRef')
    Write-Host 'Repository created as PRIVATE and master pushed. This does not publish a game build.'
} finally { Pop-Location }
