#!/bin/bash

# Directory paths
PACKAGING_DIR="$HOME/opensim/packaging"
SOURCE_DIR="$HOME/src/akisim"

# Find the latest akisim directory and extract its version
latest_dir=$(ls -d "$PACKAGING_DIR"/akisim-* 2>/dev/null | sort -V | tail -n 1)

if [ -z "$latest_dir" ]; then
    # If no existing version found, start with 0.1.0
    new_version="0.1.0"
else
    # Extract version from directory name
    current_version=$(basename "$latest_dir" | sed 's/akisim-//')
    
    # Split version into major.minor.patch
    IFS='.' read -r major minor patch <<< "$current_version"
    
    # Increment patch version
    new_patch=$((patch + 1))
    new_version="${major}.${minor}.${new_patch}"
fi

# Create new directory name
new_dir="$PACKAGING_DIR/akisim-$new_version"

# Create new directory
mkdir -p "$new_dir"

# Copy files from source directory, excluding hidden files/directories
rsync -av --exclude='.*' "$SOURCE_DIR/" "$new_dir/"

# Create zip file
cd "$PACKAGING_DIR"
zip -r "akisim-${new_version}.zip" "akisim-${new_version}"

echo "Package created: akisim-${new_version}.zip"
echo "Location: $PACKAGING_DIR/akisim-${new_version}.zip"