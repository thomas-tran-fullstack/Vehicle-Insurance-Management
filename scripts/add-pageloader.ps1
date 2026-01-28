# Script to add pageLoader.js to all HTML files
param(
    [string]$FrontendPath = "C:\Code\Vehicle-Insurance-Management\frontend"
)

$scriptTag = '    <!-- Page Loader Script -->'
$scriptLine = '    <script src="../js/pageLoader.js"><\/script>'

# Get all HTML files except templates
$htmlFiles = Get-ChildItem -Path $FrontendPath -Recurse -Include '*.html' | Where-Object {
    $_.Name -notmatch '^(header|footer)\.html$'
}

$updatedCount = 0
$skippedCount = 0

foreach ($file in $htmlFiles) {
    try {
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        
        # Check if script is already present
        if ($content -match 'pageLoader\.js') {
            $skippedCount++
            Write-Host "Skipped (already has): $($file.Name)" -ForegroundColor Yellow
            continue
        }
        
        # Add script tag before closing body tag
        if ($content -match '</body>') {
            # Calculate relative path based on file location
            $relativeDepth = ($file.FullName -replace [regex]::Escape($FrontendPath), '' | Select-String -Pattern '/' -AllMatches | Measure-Object -Line | Select-Object -ExpandProperty Lines) + 1
            $upPath = (1..$relativeDepth | ForEach-Object { '..' }) -join '/'
            
            # Create relative path to js/pageLoader.js
            if ($file.FullName -like "$FrontendPath\user\*") {
                $pathToScript = '../js/pageLoader.js'
            } elseif ($file.FullName -like "$FrontendPath\admin\*") {
                $pathToScript = '../js/pageLoader.js'
            } elseif ($file.FullName -like "$FrontendPath\staff\*") {
                $pathToScript = '../js/pageLoader.js'
            } else {
                $pathToScript = './js/pageLoader.js'
            }
            
            $newScript = "    <!-- Page Loader Script -->`n    <script src=`"$pathToScript`"></script>"
            $newContent = $content -replace '(</body>)', "$newScript`n`$1"
            
            Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
            $updatedCount++
            Write-Host "Updated: $($file.Name)" -ForegroundColor Green
        } else {
            Write-Host "Skipped (no </body> tag): $($file.Name)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "Error processing $($file.Name): $_" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Summary:"
Write-Host "Updated files: $updatedCount" -ForegroundColor Green
Write-Host "Skipped files: $skippedCount" -ForegroundColor Yellow
Write-Host "Total processed: $($updatedCount + $skippedCount)"
