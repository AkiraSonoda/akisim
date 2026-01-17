#!/bin/bash

# Fix DLL references to have Private=True for proper deployment

echo "Fixing DLL references to set Private=True..."

# Function to fix a DLL reference in a file
fix_dll() {
    local file="$1"
    local dll_name="$2"

    if grep -q "Include=\"$dll_name\"" "$file"; then
        # Use sed to change Private>False to Private>True for this DLL
        sed -i "/<Reference Include=\"$dll_name\"/,/<\/Reference>/ s/<Private>False<\/Private>/<Private>True<\/Private>/" "$file"
        echo "  ✓ Fixed $dll_name in $(basename $file)"
    fi
}

# CSJ2K.dll
echo "Fixing CSJ2K.dll..."
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Region/CoreModules/OpenSim.Region.CoreModules.csproj" "CSJ2K"
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Region/PhysicsModules/Meshing/OpenSim.Region.PhysicsModule.Meshing.csproj" "CSJ2K"
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Region/PhysicsModules/ubOdeMeshing/OpenSim.Region.PhysicsModule.ubOdeMeshing.csproj" "CSJ2K"

# DotNetOpenId.dll
echo "Fixing DotNetOpenId.dll..."
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Capabilities/Handlers/OpenSim.Capabilities.Handlers.csproj" "DotNetOpenId"
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Server/Handlers/OpenSim.Server.Handlers.csproj" "DotNetOpenId"

# Ionic.Zip.dll
echo "Fixing Ionic.Zip.dll..."
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Region/CoreModules/OpenSim.Region.CoreModules.csproj" "Ionic.Zip"
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Region/OptionalModules/OpenSim.Region.OptionalModules.csproj" "Ionic.Zip"

# C5.dll (already fixed manually, but ensure it's done)
echo "Fixing C5.dll..."
fix_dll "/home/akira/develop/0.9.3.0_akisim/akisim/OpenSim/Region/ClientStack/Linden/UDP/OpenSim.Region.ClientStack.LindenUDP.csproj" "C5"

echo ""
echo "All DLL references fixed!"
echo "Run 'make deploy' to rebuild and deploy."
