#!/bin/bash

# Define paths
SRC_BIN="$HOME/src/akisim/bin"
DEST_DIR="$HOME/opensim/grid/akisim"
DELTA_BIN="$HOME/src/akisim/doc/bin_delta/akisim_phpgrid_lin/bin_delta"

# Remove existing bin directory
echo "Removing existing bin directory..."
rm -rf "${DEST_DIR}/bin"

# Copy main bin directory
echo "Copying main bin directory..."
cp -r "${SRC_BIN}" "${DEST_DIR}/"

# Copy and overwrite with delta files
echo "Applying delta files..."
cd "${DELTA_BIN}"
for item in *; do
    if [ -e "$item" ]; then
        cp -rf "$item" "${DEST_DIR}/bin/"
        echo "Copied: $item"
    fi
done

echo "Deployment completed successfully!"
