param(
    [string]$UnityVersion = "2022.3.9f1"
)

$editorPath = Join-Path "C:\Program Files\Unity\Hub\Editor" $UnityVersion
$editorPath = Join-Path $editorPath "Editor\Unity.exe"

if (-not (Test-Path $editorPath)) {
    throw "Unity editor not found: $editorPath"
}

$editorPath
