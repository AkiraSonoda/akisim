# Configuration
CONFIGURATION := Release
SOLUTION := Akisim.sln
PACKAGING_DIR := $(HOME)/opensim/packaging
SOURCE_DIR := $(HOME)/src/akisim
DELTA_BIN := $(HOME)/src/akisim/doc/bin_delta/akisim_phpgrid_lin/bin_delta
SRC_BIN := $(HOME)/src/akisim/bin
DEST_DIR := $(HOME)/opensim/bin

# Default target
.PHONY: all
all: build

# Build the solution
.PHONY: build
build:
	dotnet build -c $(CONFIGURATION) $(SOLUTION)

# Clean build outputs
.PHONY: clean
clean:
	dotnet clean -c $(CONFIGURATION) $(SOLUTION)

# Rebuild (clean + build)
.PHONY: rebuild
rebuild: clean build

# Deploy solution
.PHONY: deploy
deploy: build
	@echo "Removing existing bin directory..."
	rm -rf "$(DEST_DIR)/bin"
	@echo "Copying main bin directory..."
	cp -r "$(SRC_BIN)" "$(DEST_DIR)/"
	@echo "Applying delta files..."
	cd "$(DELTA_BIN)" && \
	for item in *; do \
		if [ -e "$$item" ]; then \
			cp -rf "$$item" "$(DEST_DIR)/bin/" && \
			echo "Copied: $$item"; \
		fi \
	done
	@echo "Deployment completed successfully!"

# Package the solution
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
	rsync -av --exclude='.*' "$(SOURCE_DIR)/" "$$new_dir/"; \
	cd "$(PACKAGING_DIR)" && zip -r "akisim-$$new_version.zip" "akisim-$$new_version"; \
	echo "Package created: akisim-$$new_version.zip"; \
	echo "Location: $(PACKAGING_DIR)/akisim-$$new_version.zip"
