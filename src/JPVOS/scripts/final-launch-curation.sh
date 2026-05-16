#!/bin/bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REPORT_PATH="$ROOT/reports/final-launch-curation-report.md"
mkdir -p "$(dirname "$REPORT_PATH")"

echo "Running final launch curation..."

# Banned terms
BANNED_TERMS=("division" "master" "control")

# Placeholder terms
PLACEHOLDER_TERMS=("lorem ipsum" "coming soon" "todo" "placeholder" "fake testimonial" "generic enterprise")

# Ignored paths pattern
IGNORED_PATTERN="bin/|obj/|\.git/|backup|archive|reference|\.disabled"

# Find public files
PUBLIC_FILES=$(find . -type f \( -name "*.razor" -o -name "*.cshtml" -o -name "*.html" -o -name "*.md" -o -name "*.css" \) | grep -Ev "$IGNORED_PATTERN" || true)

# Arrays for findings
BANNED_FINDINGS=()
PLACEHOLDER_FINDINGS=()
MISSING_IMAGE_FINDINGS=()
EXTERNAL_IMAGE_FINDINGS=()
NON_APPROVED_IMAGE_FINDINGS=()

# Check for banned terms
for term in "${BANNED_TERMS[@]}"; do
    while IFS= read -r line; do
        if [ -n "$line" ]; then
            BANNED_FINDINGS+=("$term => $line")
        fi
    done < <(echo "$PUBLIC_FILES" | xargs grep -l -iw "$term" 2>/dev/null || true)
done

# Check for placeholder terms
for term in "${PLACEHOLDER_TERMS[@]}"; do
    while IFS= read -r line; do
        if [ -n "$line" ]; then
            PLACEHOLDER_FINDINGS+=("$term => $line")
        fi
    done < <(echo "$PUBLIC_FILES" | xargs grep -l -iF "$term" 2>/dev/null || true)
done

# Check for missing/external/non-approved images in public files
while IFS= read -r file; do
    if [ -f "$file" ]; then
        # Find image references
        IMAGES=$(grep -oP 'src="(/[^"]+\.(png|jpg|jpeg|gif|svg|webp))"' "$file" 2>/dev/null || true)
        IMAGES+=$(grep -oP 'Image="(/[^"]+\.(png|jpg|jpeg|gif|svg|webp))"' "$file" 2>/dev/null || true)
        
        if [ -n "$IMAGES" ]; then
            for img in $IMAGES; do
                # Remove quotes
                img=$(echo "$img" | sed 's/"//g' | sed 's/src=//g' | sed 's/Image=//g')
                
                # Check if external (starts with http)
                if [[ "$img" =~ ^https?:// ]]; then
                    EXTERNAL_IMAGE_FINDINGS+=("External image => $img in $file")
                # Check if local and exists
                elif [[ "$img" =~ ^/ ]]; then
                    local_path="$ROOT/wwwroot${img}"
                    if [ ! -f "$local_path" ]; then
                        MISSING_IMAGE_FINDINGS+=("Missing image => $img in $file")
                    fi
                    
                    # Check if hero/founder/background asset outside approved folder
                    if [[ "$img" =~ hero|founder|background ]] && [[ ! "$img" =~ /assets/approved/ ]]; then
                        if [[ ! "$file" =~ reports/final-launch-curation-report\.md ]]; then
                            NON_APPROVED_IMAGE_FINDINGS+=("Non-approved asset => $img in $file")
                        fi
                    fi
                fi
            done
        fi
    fi
done < <(echo "$PUBLIC_FILES")

# Run build
echo "Running dotnet build..."
BUILD_OUTPUT=$(dotnet build JPVOS.csproj -c Release 2>&1)
BUILD_EXIT=$?

# Run verify-ui.ps1 (skip if PowerShell not available)
VERIFY_EXIT=0
if command -v pwsh &> /dev/null; then
    echo "Running verify-ui.ps1..."
    VERIFY_OUTPUT=$(pwsh -ExecutionPolicy Bypass -File ./scripts/verify-ui.ps1 2>&1)
    VERIFY_EXIT=$?
elif command -v powershell &> /dev/null; then
    echo "Running verify-ui.ps1..."
    VERIFY_OUTPUT=$(powershell -ExecutionPolicy Bypass -File ./scripts/verify-ui.ps1 2>&1)
    VERIFY_EXIT=$?
else
    VERIFY_OUTPUT="PowerShell not available - skipping verify-ui check"
fi

# Determine pass/fail
FAIL=false
if [ $BUILD_EXIT -ne 0 ]; then FAIL=true; fi
if [ $VERIFY_EXIT -ne 0 ]; then FAIL=true; fi
if [ ${#BANNED_FINDINGS[@]} -gt 0 ]; then FAIL=true; fi
if [ ${#MISSING_IMAGE_FINDINGS[@]} -gt 0 ]; then FAIL=true; fi

STATUS="PASS"
if [ "$FAIL" = true ]; then STATUS="FAIL"; fi

# Generate report
{
    echo "# Final Launch Curation Report"
    echo ""
    echo "Status: **$STATUS**"
    echo ""
    echo "## Build Result"
    echo ""
    echo "Exit code: $BUILD_EXIT"
    echo ""
    echo '```text'
    echo "$BUILD_OUTPUT"
    echo '```'
    echo ""
    echo "## verify-ui Result"
    echo ""
    echo "Exit code: $VERIFY_EXIT"
    echo ""
    echo '```text'
    echo "$VERIFY_OUTPUT"
    echo '```'
    echo ""
    echo "## Banned Public Terms"
    echo ""
    if [ ${#BANNED_FINDINGS[@]} -gt 0 ]; then
        for finding in "${BANNED_FINDINGS[@]}"; do
            echo "- $finding"
        done
    else
        echo "None found."
    fi
    echo ""
    echo "## Placeholder / Weak Launch Copy"
    echo ""
    if [ ${#PLACEHOLDER_FINDINGS[@]} -gt 0 ]; then
        for finding in "${PLACEHOLDER_FINDINGS[@]}"; do
            echo "- $finding"
        done
    else
        echo "None found."
    fi
    echo ""
    echo "## Missing Image References"
    echo ""
    if [ ${#MISSING_IMAGE_FINDINGS[@]} -gt 0 ]; then
        for finding in "${MISSING_IMAGE_FINDINGS[@]}"; do
            echo "- $finding"
        done
    else
        echo "None found."
    fi
    echo ""
    echo "## External Image References"
    echo ""
    if [ ${#EXTERNAL_IMAGE_FINDINGS[@]} -gt 0 ]; then
        for finding in "${EXTERNAL_IMAGE_FINDINGS[@]}"; do
            echo "- $finding"
        done
    else
        echo "None found."
    fi
    echo ""
    echo "## Public Hero / Founder / Background Assets Outside Approved Folder"
    echo ""
    if [ ${#NON_APPROVED_IMAGE_FINDINGS[@]} -gt 0 ]; then
        for finding in "${NON_APPROVED_IMAGE_FINDINGS[@]}"; do
            echo "- $finding"
        done
    else
        echo "None found."
    fi
    echo ""
    echo "## Final Decision"
    echo ""
    if [ "$FAIL" = true ]; then
        echo "Launch curation failed. Correct the findings above, then rerun scripts/final-launch-curation.sh."
    else
        echo "Launch curation passed. Proceed to final visual review and deployment."
    fi
} > "$REPORT_PATH"

echo "Final launch curation report written to: $REPORT_PATH"

if [ "$FAIL" = true ]; then
    echo "Final launch curation: FAIL"
    exit 1
fi

echo "Final launch curation: PASS"
exit 0
