$files = Get-ChildItem 'C:\Users\mrcook1e\Desktop\Auga\AugaUnity\Assets\Prefabs' -Recurse -Filter '*.prefab' | Where-Object { $_.Extension -eq '.prefab' }

$total = 0
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName)
    # Only replace "propertyPath: m_Text" -> "propertyPath: m_text"
    # NOT "  m_Text:" (inline component field) which stays uppercase
    $newContent = $content -replace '(propertyPath: )m_Text\b', '${1}m_text'
    $count = ([regex]::Matches($content, 'propertyPath: m_Text\b')).Count
    if ($count -gt 0) {
        [System.IO.File]::WriteAllText($f.FullName, $newContent, [System.Text.Encoding]::UTF8)
        Write-Host "Fixed $count in $($f.Name)"
        $total += $count
    }
}
Write-Host "Total fixed: $total"
