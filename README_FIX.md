# Architecture Fix for SkiaSharp and Native Libraries

## Quick Start

### Deploy with Fix Applied

```bash
make deploy
```

This will automatically apply the architecture fix during deployment.

### Verify the Fix

```bash
bash verify_architecture.sh
```

## Problem

The application was crashing with:
```
System.DllNotFoundException: Unable to load shared library 'libSkiaSharp' or one of its dependencies.
... wrong ELF class: ELFCLASS32
```

**Cause:** 32-bit libraries were being deployed on a 64-bit system.

## Solution

Modified the Makefile to automatically copy 64-bit libraries from `bin/lib64/` to the deployment directories during deployment.

## What Was Changed

### 1. Makefile
- Added architecture fix to `deploy` target
- Created `fix-arch` target for manual fixes
- Automatically detects Linux systems
- Copies 4 critical libraries:
  - `libSkiaSharp.so` (image processing)
  - `libBulletSim.so` (physics engine)
  - `libopenjpeg-dotnet.so` (image compression)
  - `libubode.so` (dependency)

### 2. New Documentation
- `ARCHITECTURE_FIX.md` - Detailed explanation
- `FIX_SUMMARY.md` - Quick reference
- `CHANGES.md` - Detailed change log
- `verify_architecture.sh` - Verification script

## Verification

Run the verification script to confirm all libraries are 64-bit:

```bash
bash verify_architecture.sh
```

Expected output:
```
==========================================
Native Library Architecture Verification
==========================================

Checking libraries in /home/akira/opensim/grid/akisim/bin:
--------------------------------
✓ libSkiaSharp.so: ELF 64-bit ...
✓ libBulletSim.so: ELF 64-bit ...
✓ libopenjpeg-dotnet.so: ELF 64-bit ...
✓ libubode.so: ELF 64-bit ...

Checking libraries in /home/akira/opensim/grid/akisim/bin/runtimes/linux-x64/native:
--------------------------------------
✓ libSkiaSharp.so: ELF 64-bit ...
✓ libBulletSim.so: ELF 64-bit ...
✓ libopenjpeg-dotnet.so: ELF 64-bit ...
✓ libubode.so: ELF 64-bit ...

==========================================
✓ All libraries have correct 64-bit architecture!
==========================================
```

## Technical Details

### Root Cause
The build process created 32-bit libraries in the root `bin/` directory, which were then copied to the runtime directory during deployment. The system is 64-bit (x86_64), so it couldn't load the 32-bit libraries.

### Solution
The fix ensures that:
1. 64-bit libraries from `bin/lib64/` are copied to the runtime directory
2. The libraries in the root `bin/` directory are also updated to 64-bit
3. This happens automatically during deployment
4. Only applies to Linux systems (Windows not affected)

### Files Modified
- `Makefile` - Added architecture fix logic
- `verify_architecture.sh` - Created verification script (NEW)
- `ARCHITECTURE_FIX.md` - Created detailed documentation (NEW)
- `FIX_SUMMARY.md` - Created summary documentation (NEW)
- `CHANGES.md` - Created change log (NEW)

## Usage Examples

### Deploy New Build
```bash
# Build and deploy (automatically applies architecture fix)
make deploy

# Verify the fix worked
bash verify_architecture.sh
```

### Manual Verification
```bash
# Check individual library
file /home/akira/opensim/grid/akisim/bin/libSkiaSharp.so

# Should show: ELF 64-bit LSB shared object, x86-64, version 1 (GNU/Linux)
```

## Notes

- **Linux only:** This fix only applies to Linux systems
- **Windows safe:** Windows deployments are not affected
- **Backward compatible:** No breaking changes
- **Automatic:** Future deployments will automatically apply the fix
- **Verified:** All changes have been tested and verified

## Support

For issues or questions, refer to:
- `ARCHITECTURE_FIX.md` for detailed technical information
- `FIX_SUMMARY.md` for quick reference
- `CHANGES.md` for detailed change log

## Status

✅ Fix implemented and tested
✅ Documentation complete
✅ Verification script working
✅ All libraries confirmed as 64-bit
✅ Ready for production use
