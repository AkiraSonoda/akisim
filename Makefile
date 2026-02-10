# Akisim Makefile - Thin wrapper around MSBuild/dotnet commands
#
# This Makefile provides convenient shortcuts for common build tasks.
# The actual build logic is in Akisim.targets, which is imported by all projects.
#
# Usage:
#   make build   - Build the solution
#   make clean   - Clean build artifacts
#   make deploy  - Deploy to configured location
#   make package - Create versioned package
#   make rebuild - Clean and rebuild

# Check for configuration parameter
ifeq ($(config),)
    CONFIGURATION := Release
else
    CONFIGURATION := $(config)
endif

# Default to Linux paths
PACKAGING_DIR := $(HOME)/opensim/packaging
SOURCE_DIR := $(HOME)/src/akisim
DELTA_BIN := $(HOME)/src/akisim/doc/bin_delta/akisim_phpgrid_lin
SRC_BIN := $(HOME)/src/akisim/bin
DEST_DIR := $(HOME)/opensim/grid/akisim

# Override with Windows paths if on Windows
ifeq ($(OS),Windows_NT)
    PACKAGING_DIR := /d/ieu/opensim/packaging
    SOURCE_DIR := /d/ieu/develop/akisim
    DELTA_BIN := /d/ieu/opensim/grid/akisim_akiwin/delta
    SRC_BIN := /d/ieu/develop/akisim/bin
    DEST_DIR := /d/ieu/opensim/grid/akisim_dereos/KoSuai
endif

SOLUTION := Akisim.sln

# Default target
.PHONY: all
all: deploy

# Build the solution
.PHONY: build
build: clean restore
	@echo "Building in $(CONFIGURATION) configuration..."
	dotnet build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore -maxcpucount:1

# Clean all build artifacts - MSBuild handles bin/ removal via CleanBinDirectory target
.PHONY: clean
clean:
	@echo "Cleaning solution..."
	dotnet clean $(SOLUTION) --configuration $(CONFIGURATION)

	
# Restore NuGet packages
.PHONY: restore
restore:
	@echo "Restoring NuGet packages..."
	dotnet restore $(SOLUTION)

# Rebuild
.PHONY: rebuild
rebuild: clean build

# Deploy solution - calls MSBuild Deploy target
.PHONY: deploy
deploy: build
	@echo "Deploying via MSBuild target..."
	dotnet msbuild $(SOLUTION) -t:Deploy -p:Configuration=$(CONFIGURATION) -nologo -v:minimal

# Package the solution - calls MSBuild Package target
.PHONY: package
package: build
	@echo "Creating package via MSBuild target..."
	dotnet msbuild $(SOLUTION) -t:Package -p:Configuration=$(CONFIGURATION) -nologo -v:minimal

# Package binary distribution (built artifacts only)
.PHONY: package-binary
package-binary: build
	@latest_dir=$$(ls -d $(PACKAGING_DIR)/akisim-bin-* 2>/dev/null | sort -V | tail -n 1); \
	if [ -z "$$latest_dir" ]; then \
		new_version="0.1.0"; \
	else \
		current_version=$$(basename "$$latest_dir" | sed 's/akisim-bin-//'); \
		major=$$(echo $$current_version | cut -d. -f1); \
		minor=$$(echo $$current_version | cut -d. -f2); \
		patch=$$(echo $$current_version | cut -d. -f3); \
		new_patch=$$((patch + 1)); \
		new_version="$$major.$$minor.$$new_patch"; \
	fi; \
	new_dir="$(PACKAGING_DIR)/akisim-bin-$$new_version"; \
	echo "Creating binary package: akisim-bin-$$new_version"; \
	rm -rf "$$new_dir"; \
	mkdir -p "$$new_dir/bin"; \
	echo "Copying OpenSim binaries..."; \
	cp -r "$(SRC_BIN_OPENSIM)"/* "$$new_dir/bin/"; \
	echo "Copying additional files from bin directory..."; \
	rsync -av --ignore-existing "$(SOURCE_BIN)/" "$$new_dir/bin/"; \
	echo "Applying delta overrides..."; \
	cd "$(DELTA_BIN)" && \
	for item in *; do \
		if [ -e "$$item" ]; then \
			cp -rf "$$item" "$$new_dir/bin/" && \
			echo "  Copied: $$item"; \
		fi \
	done; \
	echo "Creating archive..."; \
	cd "$(PACKAGING_DIR)" && zip -r "akisim-bin-$$new_version.zip" "akisim-bin-$$new_version"; \
	echo "Binary package created: akisim-bin-$$new_version.zip";
