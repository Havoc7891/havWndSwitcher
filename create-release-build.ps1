$pub = ".\bin\x64\Release\net8.0-windows\win-x64\publish"
$ver = "1.0.0.0"
$zip = "havWndSwitcher-$ver-win-x64.zip"

# Remove .pdb and .xml files in the publish folder
Get-ChildItem "$pub\*.pdb","$pub\*.xml" -File | Remove-Item -Force -ErrorAction SilentlyContinue

# Copy README.txt
Copy-Item .\README.txt "$pub\README.txt" -Force

# Copy LICENSE.txt
Copy-Item .\LICENSE "$pub\LICENSE.txt" -Force

Compress-Archive -Path "$pub\*" -DestinationPath $zip -Force
Write-Output "Done! Created $zip."