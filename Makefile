# Check for configuration parameter
ifeq ($(config),)
    CONFIGURATION := Release
else
    CONFIGURATION := $(config)
endif

# Default to Linux paths
PACKAGING_DIR := $(HOME)/opensim/packaging
SOURCE_DIR := $(HOME)/src/0.9.3.0_akisim/akisim
SOURCE_BIN := $(HOME)/src/0.9.3.0_akisim/akisim/bin
DELTA_BIN := $(HOME)/src/akisim/doc/bin_delta/akisim_phpgrid_lin
SRC_BIN_OPENSIM := $(HOME)/src/0.9.3.0_akisim/akisim/OpenSim/Region/Application/bin/$(CONFIGURATION)/publish
DEST_DIR := $(HOME)/opensim/grid/working

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
all: build

# Build the solution
.PHONY: build
build:
	@echo "Building solution in $(CONFIGURATION) configuration..."
	dotnet build $(SOLUTION) -c $(CONFIGURATION)
	@echo "Publishing OpenSim in $(CONFIGURATION) configuration..."
	dotnet publish OpenSim/Region/Application/OpenSim.csproj -c $(CONFIGURATION) --no-self-contained --no-build

# Clean all build artifacts (but keep project files)
.PHONY: clean
clean:
	@echo "Cleaning dotnet..."
	@dotnet clean -c $(CONFIGURATION) $(SOLUTION)
	@echo "Cleaning obj directories..."
	@find . -type d -name "obj" -exec sh -c '\
		for dir in "$$@"; do \
			echo "Cleaning directory: $$dir"; \
			rm -rf "$$dir"/*; \
		done' sh {} +
	@echo "Removing .csproj.user files..."
	@find . -type f -name "*.csproj.user" -exec sh -c '\
		for file in "$$@"; do \
			echo "Removing file: $$file"; \
			rm -f "$$file"; \
		done' sh {} +
	@echo "Clean completed."
	
# Rebuild
.PHONY: rebuild
rebuild: clean build

# Deploy solution
.PHONY: deploy
deploy: build
	@echo "Deploying $(CONFIGURATION) build..."
	rm -rf "$(DEST_DIR)/bin"
	mkdir -p "$(DEST_DIR)/bin"
	@echo "Copying OpenSim binaries..."
	cp -r "$(SRC_BIN_OPENSIM)"/* "$(DEST_DIR)/bin/"
	@echo "Copying additional files from bin directory..."
	rsync -av --ignore-existing "$(SOURCE_BIN)/" "$(DEST_DIR)/bin/"
	@echo "Applying delta overrides..."
	cd "$(DELTA_BIN)" && \
	for item in *; do \
		if [ -e "$$item" ]; then \
			cp -rf "$$item" "$(DEST_DIR)/bin/" && \
			echo "Copied: $$item"; \
		fi \
	done
	@echo "Deployment completed successfully!"

# Package the solution (source distribution)
.PHONY: package
package: build
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
	rsync -av --exclude='.*' --exclude='obj/' --exclude='bin/Release/' --exclude='bin/Debug/' \
		--exclude='*.user' --exclude='*.suo' --exclude='publish/' \
		"$(SOURCE_DIR)/" "$$new_dir/"; \
	cd "$(PACKAGING_DIR)" && zip -r "akisim-$$new_version.zip" "akisim-$$new_version"; \
	echo "Package created: akisim-$$new_version.zip";

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
