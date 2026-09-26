$pub = ".\bin\Release\net10.0-windows\win-x64\publish"
$ver = "1.1.0.0"
$zip = "havWndSwitcher-$ver-win-x64.zip"

# Copy README.txt
Copy-Item .\README.txt "$pub\README.txt" -Force

# Copy LICENSE.txt
Copy-Item .\LICENSE "$pub\LICENSE.txt" -Force

Compress-Archive -Path "$pub\*" -DestinationPath $zip -Force
Write-Output "Done! Created $zip."