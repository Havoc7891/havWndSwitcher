# Make sure ImageMagick's 'magick' is in PATH
# Create multi-depth ICO (8-bit for small sizes, full color for large)
magick `
  -depth 8 16.png -colors 256 `
  -depth 8 32.png -colors 256 `
  48.png `
  256.png `
  havWndSwitcher.ico

Write-Output "Done! Created havWndSwitcher.ico with multi-resolution, multi-depth."