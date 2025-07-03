#!/bin/bash

# fix-xml-documentation.sh
# Comprehensive script to fix XML documentation errors in C# files

# Default values
SEARCH_PATH="."
WHATIF=false
VERBOSE=false

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Function to print colored output
print_color() {
    local color=$1
    shift
    echo -e "${color}$*${NC}"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -p, --path PATH     Search path for C# files (default: current directory)"
    echo "  -w, --whatif        Show what would be changed without modifying files"
    echo "  -v, --verbose       Show detailed output"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                          # Fix all C# files in current directory"
    echo "  $0 --whatif                 # Preview changes without applying them"
    echo "  $0 --path ./src --verbose   # Fix files in ./src with detailed output"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--path)
            SEARCH_PATH="$2"
            shift 2
            ;;
        -w|--whatif)
            WHATIF=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Function to create a backup of a file
backup_file() {
    local file="$1"
    cp "$file" "$file.bak.$(date +%Y%m%d_%H%M%S)"
}

# Function to fix XML documentation in a single file
fix_xml_documentation() {
    local file="$1"
    local temp_file=$(mktemp)
    local changes_made=false
    local change_count=0
    
    print_color $YELLOW "Processing: $file"
    
    # Copy original file to temp
    cp "$file" "$temp_file"
    
    # Apply fixes using sed (each fix is applied sequentially)
    
    # Fix 1: Escape ampersands that aren't already part of entities
    if sed -i 's|///\([^&]*\)&\([^a-zA-Z]\)|///\1\&amp;\2|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Escape ampersands"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 2: Fix generic type references (e.g., <T> becomes &lt;T&gt;)
    if sed -i 's|///\(.*\)<\([A-Z][a-zA-Z0-9]*\)>|\1\&lt;\2\&gt;|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Fix generic type references"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 3: Fix equals signs in text content
    if sed -i 's|///\([^<]*\)=\([^">]\)|///\1\&#61;\2|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Escape equals signs"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 4: Fix commas in problematic contexts
    if sed -i 's|///\([^<]*\),\([^>]*\)|///\1\&#44;\2|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Escape commas"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 5: Fix parentheses in function signatures
    if sed -i 's|///\([^<]*\)(\([^)]*\))|///\1\&#40;\2\&#41;|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Escape parentheses"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 6: Fix backticks
    if sed -i 's|///\([^`]*\)`|///\1\&#96;|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Escape backticks"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 7: Close unclosed <para> tags
    if sed -i '/\/\/\/ <para>$/s|$|</para>|' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Close unclosed para tags"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 8: Close unclosed <summary> tags at end of comment blocks
    if sed -i '/\/\/\/ <summary>$/{
        :a
        n
        /^[[:space:]]*\/\/\/[[:space:]]*$/ba
        /^[[:space:]]*\/\/\/ </summary>/!{
            /^[[:space:]]*\(public\|private\|internal\|protected\|namespace\|class\|struct\|interface\|enum\|\[\|}\)/i\
/// </summary>
        }
    }' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Close unclosed summary tags"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 9: Remove orphaned end tags
    if sed -i '/\/\/\/ <\/\(summary\|para\|remarks\|code\|example\|list\|item\|description\|term\|returns\|param\)>[[:space:]]*$/d' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Remove orphaned end tags"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 10: Handle malformed attribute-like text (specific to the build errors)
    if sed -i 's|\/\/\/\([^<]*\)\([a-zA-Z]\+\)[[:space:]]\+\([a-zA-Z]\+\)[[:space:]]\+\([a-zA-Z]\+\)|///\1\2="\3" \4|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Fix malformed attributes"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 11: Remove duplicate consecutive end tags
    if sed -i ':a;N;$!ba;s|\/\/\/ </\([^>]*\)>\n[[:space:]]*\/\/\/ </\1>|/// </\1>|g' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Remove duplicate end tags"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Fix 12: Clean up empty XML comment lines
    if sed -i '/^[[:space:]]*\/\/\/[[:space:]]*$/d' "$temp_file" 2>/dev/null; then
        if ! cmp -s "$file" "$temp_file"; then
            changes_made=true
            ((change_count++))
            $VERBOSE && print_color $GREEN "  Applied: Clean up empty comment lines"
            cp "$temp_file" "$file.tmp" && mv "$file.tmp" "$temp_file"
        fi
    fi
    
    # Check if any changes were made
    if $changes_made; then
        if $WHATIF; then
            print_color $RED "WHAT-IF: Would modify $file ($change_count changes)"
        else
            # Create backup before modifying
            backup_file "$file"
            mv "$temp_file" "$file"
            print_color $GREEN "Modified $file ($change_count changes)"
        fi
        rm -f "$temp_file"
        return 0
    else
        print_color $GRAY "  No changes needed"
        rm -f "$temp_file"
        return 1
    fi
}

# Main execution
print_color $CYAN "=== XML Documentation Fixer ==="
print_color $NC "Scanning for C# files in: $SEARCH_PATH"

if $WHATIF; then
    print_color $YELLOW "RUNNING IN WHAT-IF MODE - No files will be modified"
fi

# Find all C# files
if [[ ! -d "$SEARCH_PATH" ]]; then
    print_color $RED "Error: Directory '$SEARCH_PATH' does not exist"
    exit 1
fi

mapfile -t files < <(find "$SEARCH_PATH" -name "*.cs" -type f)
modified_count=0
total_files=${#files[@]}

print_color $NC "Found $total_files C# files"
echo ""

# Process each file
for file in "${files[@]}"; do
    if fix_xml_documentation "$file"; then
        ((modified_count++))
    fi
done

echo ""
print_color $CYAN "=== Summary ==="
if $WHATIF; then
    print_color $YELLOW "WHAT-IF: Would modify $modified_count out of $total_files files"
else
    print_color $GREEN "Modified $modified_count out of $total_files files"
fi

if [[ $modified_count -gt 0 ]] && ! $WHATIF; then
    echo ""
    print_color $CYAN "Recommendation: Run 'dotnet build' to check if all XML documentation errors are resolved."
    print_color $CYAN "If errors remain, you may need to manually review the remaining files."
    echo ""
    print_color $NC "Backup files were created with timestamp extensions (.bak.YYYYMMDD_HHMMSS)"
    print_color $NC "You can restore them if needed: cp file.cs.bak.* file.cs"
fi