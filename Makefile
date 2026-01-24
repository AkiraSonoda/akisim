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
	dotnet build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore

# Clean all build artifacts and project files
.PHONY: clean
clean:
	@echo "Cleaning solution..."
	dotnet clean $(SOLUTION) --configuration $(CONFIGURATION)
	rm -rf bin/

	
# Restore NuGet packages
.PHONY: restore
restore:
	@echo "Restoring NuGet packages..."
	dotnet restore $(SOLUTION)

# Rebuild
.PHONY: rebuild
rebuild: clean build

# Deploy solution
.PHONY: deploy
deploy:
	@echo "Deploying $(CONFIGURATION) build..."
	rm -rf "$(DEST_DIR)/bin"
	cp -rL "$(SRC_BIN)" "$(DEST_DIR)/"
	cd "$(DELTA_BIN)" && \
	for item in *; do \
		if [ -e "$$item" ]; then \
			cp -rfL "$$item" "$(DEST_DIR)/bin/" && \
			echo "Copied: $$item"; \
		fi \
	done
	@echo "Deployment completed successfully!"

# Package the solution
.PHONY: package
package: 
	@latest_dir=$$(ls -d $(PACKAGING_DIR)/akisim-* 2>/dev/null | sort -V | tail -n 1); \
	if [ -z "$$latest_dir" ]; then \
		new_version="0.1.0"; \
	else \
		current_version=$$(basename "$$latest_dir" | sed 's/akisim-//'); \
		major=$$(echo $$current_version | cut -d. -f1); \
		minor=$$(echo $$current_version | cut -d. -f2); \
		patch=$$(echo $$current_version | cut -d. -f3); \
		new_patch=$$((patch + 1)); \
		new_version="$$major.$$minor.$$new_patch"; \
	fi; \
	new_dir="$(PACKAGING_DIR)/akisim-$$new_version"; \
	mkdir -p "$$new_dir"; \
	rsync -avL --exclude='.*' "$(SOURCE_DIR)/" "$$new_dir/"; \
	cd "$(DELTA_BIN)" && \
	for item in *; do \
		if [ -e "$$item" ]; then \
			cp -rfL "$$item" "$$new_dir/bin/" && \
			echo "Copied delta: $$item"; \
		fi \
	done; \
	cd "$(PACKAGING_DIR)" && zip -r "akisim-$$new_version.zip" "akisim-$$new_version"; \
	echo "Package created: akisim-$$new_version.zip";
